using System.Collections.Generic;
using Linux.IO;
using UnityEngine;

namespace Linux.PseudoTerminal
{
    public class CursorLines {
        string[] _linesCache;

        protected LimitedStream Buffer;
        public int Pointer {
            get {
                return Buffer.Pointer;
            }
        }

        public int Cursor { get; protected set; }

        public string CurrentLine { get; protected set; }

        public int BlockedIndex { get; protected set; }

        public CursorLines(int bufferSize) {
            Buffer = new LimitedStream(bufferSize);

            Truncate();
        }

        public void Truncate() {
            Buffer.Truncate();
            _linesCache = null;
            BlockedIndex = -1;
            Cursor = 0;
            CurrentLine = "";
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
            if (text.Contains("\r")) {
                int cursor = Cursor;
                int pointer = Buffer.Length - cursor;
                int diff = Pointer - pointer;

                int oldBlockedIndex = BlockedIndex;
                BlockedIndex = -1;

                for (var i = 0; i < diff; i++) {
                    MoveCursor(-1);
                    Buffer.Remove();
                }

                BlockedIndex = oldBlockedIndex;

                UpdateLinesCache();
            } else {
                Buffer.Write(text);
                UpdateLinesCache();

                // Always move Cursor after UpdateLinesCache()
                MoveCursor(text.Length);

                Block();
            }
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
                MoveCursor(-1);
            }
        }

        public void WrapAt(string message, int position) {
            if (message == CurrentLine) {
                int oldBlockedIndex = BlockedIndex;
                BlockedIndex = -1;

                int toRemove = message.Length - position;

                for (var i = 0; i < toRemove; i++) {
                    MoveCursor(-1);
                    Buffer.Remove();
                }

                BlockedIndex = oldBlockedIndex;

                AddKey("\n");

                string nextMsg = message.Substring(position);

                AddKey(nextMsg);
            }
        }

        public bool IsAtEnd() {
            return CheckAtEnd(Pointer);
        }

        public bool IsAtBegin() {
            return Pointer <= 0;
        }

        public void MoveCursor(int step) {
            if (MovePointer(step)) {
                InternalMoveCursor(step);
            }
        }

        void InternalMoveCursor(int step) {
            if (Buffer.Length == 0) {
                return;
            }

            int cursor = Cursor + step;

            if (cursor < 0) {
                cursor = 0;
            } else if (cursor > CurrentLine.Length) {
                cursor = CurrentLine.Length;
            }

            Cursor = cursor;
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

        protected bool CheckAtEnd(int index) {
            return index >= Buffer.Length;
        }

        protected void UpdateLinesCache() {
            _linesCache = Buffer.ReadLines();
            CurrentLine = _linesCache[_linesCache.Length - 1];
        }
    }
}