using System.Collections.Generic;
using Linux.IO;
using UnityEngine;

namespace Linux.PseudoTerminal
{
    public class CursorLines {
        string[] _linesCache;

        public delegate int OnCursorUpward();

        protected LimitedStream Buffer;
        protected Dictionary<int, int> CursorIndexes;

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
        public int BlockedIndex { get; protected set; }


        public CursorLines(int bufferSize) {
            Buffer = new LimitedStream(bufferSize);
            CursorIndexes = new Dictionary<int, int>();

            LineIndex = 0;
            CurrentLine = "";
            BlockedIndex = -1;
        }

        public void Truncate() {
            Buffer.Truncate();
            _linesCache = null;
            BlockedIndex = -1;
            LineIndex = 0;
            CurrentLine = "";
            CursorIndexes.Clear();
        }

        public void ClearUntilLastLine() {
            string line = CurrentLine;
            int blockedIndex = BlockedIndex;

            Truncate();

            AddKey(line);
            BlockedIndex = blockedIndex;
        }

        public void Close() {
            Buffer.Close();
        }

        public void Block() {
            BlockedIndex = Pointer;
        }

        public void Add(string text) {
            Buffer.Write(text);
            UpdateLinesCache();

            // Always move Cursor after UpdateLinesCache()
            MoveCursor(text.Length);

            Block();
        }

        public void AddKey(string text) {
            if (text == "\n") {
                text = " " + text;
                Buffer.Seek(Buffer.Length);
            }

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

            if (MovePointer(-1)) {
                Buffer.Remove();

                UpdateLinesCache();

                // Always move Cursor after UpdateLinesCache()
                MoveCursor(-1, () => CurrentLine.Length);
            }
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
            if (MovePointer(step)) {
                MoveCursor(step, GetCursorIndex);
            }
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

        bool MovePointer(int step) {
            if (Buffer.Length == 0) {
                Buffer.Seek(0);
                return false;
            }

            int pointer = Pointer + step;

            if (pointer <= 0) {
                pointer = 0;
            } else if (CheckAtEnd(pointer)) {
                pointer = Buffer.Length;
            }

            if (IsMoveBlocked(pointer)) {
                return false;
            }

            Buffer.Seek(pointer);
            return true;
        }

        public string[] GetLines() {
            if (_linesCache == null) {
                UpdateLinesCache();
            }

            return _linesCache;
        }

        protected bool IsMoveBlocked(int newPointer) {
            return BlockedIndex != -1 &&
                    newPointer < BlockedIndex;
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
            if (CursorIndexes.ContainsKey(LineIndex)) {
                return CursorIndexes[LineIndex];
            }

            CursorIndexes.Add(LineIndex, 0);
            return 0;
        }
    }
}