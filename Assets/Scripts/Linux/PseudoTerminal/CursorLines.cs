using System.Collections.Generic;
using Linux.IO;

namespace Linux.PseudoTerminal
{
    public class CursorLines {
        readonly object _cursorLock = new object();

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
            lock(_cursorLock) {
                Buffer.Truncate();
                _linesCache = null;
                BlockedIndex = -1;
                Cursor = 0;
                CurrentLine = "";
            }
        }

        public void ClearUntilLastLine() {
            lock(_cursorLock) {
                string line = CurrentLine;
                int blockedIndex = BlockedIndex;

                Truncate();

                AddKey(line);
                BlockedIndex = blockedIndex;
            }
        }

        public void Close() {
            Buffer.Close();
        }

        public void Block() {
            lock(_cursorLock) {
                BlockedIndex = Pointer;
            } 
        }

        public void Add(string text) {
            lock(_cursorLock) {
                if (text.Contains("\r")) {
                    int pointer = Buffer.Length - Cursor;
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
        }

        public void AddKey(string text) {
            lock(_cursorLock) {
                if (text == "\n") {
                    text = " " + text;

                    int index = Buffer.Length;
                    Buffer.Seek(index);
                }

                Buffer.Write(text);
                UpdateLinesCache();

                // Always move Cursor after UpdateLinesCache()
                MoveCursor(text.Length);
            }
        }

        public void RemoveAtFront() {
            lock(_cursorLock) {
                if (IsAtEnd()) {
                    return;
                }

                Buffer.Remove();
                UpdateLinesCache();
            }
        }

        public void RemoveAtBack() {
            lock(_cursorLock) {
                if (IsAtBegin()) {
                    return;
                }

                if (MovePointer(-1)) {
                    Buffer.Remove();

                    UpdateLinesCache();

                    // Always move Cursor after UpdateLinesCache()
                    InternalMoveCursor(-1);
                }
            }
        }

        public void WrapAt(string message, int position) {
            lock(_cursorLock) {
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
        }

        public bool IsAtEnd() {
            return CheckAtEnd(Pointer);
        }

        public bool IsAtBegin() {
            return Pointer <= 0;
        }

        public void MoveCursor(int step) {
            lock(_cursorLock) {
                if (MovePointer(step)) {
                    InternalMoveCursor(step);
                }
            }
        }

        void InternalMoveCursor(int step) {
            if (Buffer.Length == 0) {
                Cursor = 0;
                return;
            }

            int cursor = Cursor + step;

            if (cursor < 0) {
                cursor = 0;
            } else if (cursor >= CurrentLine.Length) {
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

            if (pointer < 0) {
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
            lock(_cursorLock) {
                _linesCache = Buffer.ReadLines();
                CurrentLine = _linesCache[_linesCache.Length - 1];
            }
        }
    }
}