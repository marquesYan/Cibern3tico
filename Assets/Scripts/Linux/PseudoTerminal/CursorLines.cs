using System.Collections.Generic;
using Linux.IO;
using UnityEngine;

namespace Linux.PseudoTerminal
{
    public class CursorLines {
        string[] _linesCache;

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

            MoveCursor(text.Length);
        }

        public void Remove(int index) {
            Buffer.Remove(index);
            UpdateLinesCache();

            MoveCursor(-index);
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
            if (Buffer.Length == 0) {
                Buffer.Seek(0);
                return;
            }

            int pointer = Buffer.Length + Pointer + step;
            int cursor = GetCursorIndex() + step;

            if (pointer <= 0) {
                pointer = cursor = 0;
            } else if (CheckAtEnd(pointer)) {
                pointer = Buffer.Length;
            }

            if (cursor < 0) {
                if (LineIndex > 0) {
                    UpdateCurrentLine(LineIndex - 1);
                    cursor = CurrentLine.Length;
                } else {
                    cursor = 0;
                }
            } else if (cursor >= CurrentLine.Length) {
                if (LineIndex >= _linesCache.Length) {
                    cursor = CurrentLine.Length;
                } else {
                    UpdateCurrentLine(LineIndex + 1);
                    cursor = CurrentLine.Length;
                }
            }

            Buffer.Seek(pointer);
            CursorIndexes[LineIndex] = cursor;
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

        void UpdateLinesCache() {
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