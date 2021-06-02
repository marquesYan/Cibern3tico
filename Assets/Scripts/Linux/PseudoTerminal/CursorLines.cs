using System.Collections.Generic;
using Linux.IO;
using UnityEngine;

namespace Linux.PseudoTerminal
{
    public class CursorLines {
        string[] _linesCache;

        public delegate int OnCursorUpward();

        protected LimitedStream Buffer;
        protected List<int> CursorIndexes;

        public int Pointer {
            get {
                return Buffer.Pointer;
            }
        }

         public int Cursor {
            get {
                return GetCursorIndex();
            }
        }

        public int LineIndex { get; protected set; }
        public string CurrentLine { get; protected set; }


        public CursorLines(int bufferSize) {
            Buffer = new LimitedStream(bufferSize);
            CursorIndexes = new List<int>();
            CursorIndexes.Add(0);
            LineIndex = 0;
            CurrentLine = "";
        }

        public void Truncate() {
            Buffer.Truncate();
        }

        public void Close() {
            Buffer.Close();
        }

        public void Block() {
            // TODO
        }

        public void Add(string text) {
            Buffer.Write(text);
            UpdateLinesCache();

            // Always move Cursor after UpdateLinesCache()
            MoveCursor(text.Length);
        }

        public void RemoveAtFront() {
            if (IsAtEnd()) {
                return;   
            }

            Buffer.Remove();
            UpdateLinesCache();
        }

        public void RemoveAtBack() {
            if (IsAtBegin()) {
                return;
            }

            MovePointer(-1);

            Buffer.Remove();

            UpdateLinesCache();

            // Always move Cursor after UpdateLinesCache()
            MoveCursor(-1, () => CurrentLine.Length);
        }

        public void MoveLine(int step) {
            UpdateCurrentLine(LineIndex + step);
        }

        public bool IsAtEnd() {
            return CheckAtEnd(Pointer);
        }

        public bool IsAtBegin() {
            return Pointer <= 0;
        }

        public void MoveCursor(int step) {
            MovePointer(step);
            MoveCursor(step, GetCursorIndex);
        }

        void MoveCursor(int step, OnCursorUpward onUpward) {
            if (Buffer.Length == 0) {
                return;
            }

            int cursor = GetCursorIndex() + step;

            if (cursor < 0) {
                if (LineIndex > 0) {
                    UpdateCurrentLine(LineIndex - 1);
                }

                cursor = onUpward();
            } else if (cursor > CurrentLine.Length) {
                if (LineIndex > _linesCache.Length) {
                    cursor = CurrentLine.Length;
                } else {
                    UpdateCurrentLine(LineIndex + 1);
                    cursor = GetCursorIndex();
                }
            }

            CursorIndexes[LineIndex] = cursor;
        }

        void MovePointer(int step) {
            if (Buffer.Length == 0) {
                Buffer.Seek(0);
                return;
            }

            int pointer = Pointer + step;

            if (pointer <= 0) {
                pointer = 0;
            } else if (CheckAtEnd(pointer)) {
                pointer = Buffer.Length;
            }

            Buffer.Seek(pointer);
        }

        public string[] GetLines() {
            if (_linesCache == null) {
                UpdateLinesCache();
            }

            return _linesCache;
        }

        protected void UpdateCurrentLine(int index) {
            if (index <= 0) {
                index = 0;
            } else if (index >= _linesCache.Length) {
                index = _linesCache.Length - 1;
            }

            LineIndex = index;
            CurrentLine = _linesCache[LineIndex];
        }

        protected bool CheckAtEnd(int index) {
            return index >= Buffer.Length;
        }

        protected void UpdateLinesCache() {
            _linesCache = Buffer.ReadLines();
            UpdateCurrentLine(_linesCache.Length - 1);
        }

        protected int GetCursorIndex() {
            try {
                return CursorIndexes[LineIndex];
            } catch (System.ArgumentOutOfRangeException) {
                CursorIndexes.Insert(LineIndex, 0);
                return 0;
            }
        }
    }
}