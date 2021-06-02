using System.Text;
using System.Collections.Generic;
using Linux.IO;
using UnityEngine;

namespace Linux.PseudoTerminal
{
    public class Vector {
        public float X;
        public float Y;

        public Vector(float x, float y) {
            X = x;
            Y = y;
        }
    }

    public abstract class VirtualTerminal
    {
        event System.Action OnFirstDraw;
        bool _moveCursorToEnd = false;
        int _moveYAxis = -1;
        int _moveXAxis = -1;
        int _typedKeysCount;

        protected bool IsFirstDraw = true;

        protected CursorLines CursorLinesMngr;

        protected Vector CursorSize;

        public bool IsClosed { get; protected set; }

        public VirtualTerminal(int bufferSize) {
            CursorLinesMngr = new CursorLines(bufferSize);

            IsClosed = false;

            // CursorIndexes = new List<int>();
            // CursorIndexes.Add(0);
        }

        public void Close() {
            CursorLinesMngr.Close();
            IsClosed = true;
        }

        public void ClearBuffer() {
            CursorLinesMngr.Truncate();
        }

        public void RequestYAxis(int position) {
            _moveYAxis = position;
        }

        public void RequestXAxis(int position) {
            _moveXAxis = position;
        }

        public void RequestHighXAxis() {
            RequestXAxis(int.MaxValue);
        }

        public void RequestHighYAxis() {
            RequestYAxis(int.MaxValue);
        }

        public void RequestCursorToEnd() {
            _moveCursorToEnd = true;
        }

        public int SendToSreen(string message) {
            HandleInputKey(message);
            return message.Length;
        }

        protected int WriteToBuffer(string message) {
            RequestHighYAxis();
            CursorLinesMngr.Add(message);
            return message.Length;
        }

        public void SubscribeFirstDraw(System.Action onFirstDraw) {
            OnFirstDraw += onFirstDraw;
        }

        public void DrawGUI() {
            if (! IsClosed) {
                HandleDraw();
            }
        }

        protected abstract void MoveYAxis(int position);
        protected abstract void MoveXAxis(int position);
        protected abstract void DrawLine(string message);
        protected abstract void HandleDraw();

        protected abstract Vector CalcSize(string message);

        public abstract void MoveCursorToEnd();

        protected void DrawTerm() {
            FlushBuffer();

            if (_moveCursorToEnd) {
                _moveCursorToEnd = false;
                MoveCursorToEnd();
            }

            if (_moveYAxis != -1) {
                MoveYAxis(_moveYAxis);
                _moveYAxis = -1;
            }

            if (_moveXAxis != -1) {
                MoveXAxis(_moveXAxis);
                _moveXAxis = -1;
            }

            if (IsFirstDraw) {
                IsFirstDraw = false;
                OnFirstDraw();
            }
        }

        protected void FlushBuffer() {
            int i = 0;
            foreach (string line in CursorLinesMngr.GetLines()) {
                DrawLine(line);

                if (i == CursorLinesMngr.LineIndex) {
                    // int index = GetCursorIndex(i);
                    // string fmt = line.Substring(0, index);
                    // Debug.Log("calc cursor size: " + fmt);
                    // Debug.Log("calc cursor lenght: " + fmt.Length);
                    CursorSize = CalcSize(line);
                }

                i++;
            }
        }

        protected void HandleInputKey(string key) {
            switch(key) {
                case CharacterControl.C_DBACKSPACE: {
                    // Buffer.TryRemove(Buffer.CurrentLine.Length - 1);
                    // UpdateCursorIndex(-1, true);
                    // if (CursorLinesMngr.CursorPosition > 0) {
                    //     CursorLinesMngr.RemoveAtCursorPosition(-1);
                    // }
                    break;
                }

                case CharacterControl.C_DDELETE: {
                    if (!CursorLinesMngr.IsAtEnd()) {
                        // Delete token at cursor position
                        // CursorLinesMngr.RemoveAtCursorPosition(0);
                    }
                    break;
                }

                case CharacterControl.C_DLEFT_ARROW: {
                    CursorLinesMngr.MoveCursor(-1);
                    break;
                }

                case CharacterControl.C_DRIGHT_ARROW: {
                    CursorLinesMngr.MoveCursor(1);
                    break;
                }

                case CharacterControl.C_DUP_ARROW: {
                    CursorLinesMngr.MoveLine(-1);
                    break;
                }

                case CharacterControl.C_DDOWN_ARROW: {
                    CursorLinesMngr.MoveLine(1);
                    break;
                }

                case CharacterControl.C_BLOCK_REMOVE: {
                    CursorLinesMngr.Block();
                    break;
                }

                default: {
                    CursorLinesMngr.Add(key);
                    break;
                }
            }
        }

        // protected void UpdateCursorIndex(int step, bool seekBuffer) {
        //     int index = GetCurrentCursorIndex();

        //     index += step;

        //     if (index < 0) {
        //         index = 0;
        //     } else if (index > Buffer.CurrentLine.Length - 1) {
        //         index = Buffer.CurrentLine.Length;
        //     }

        //     // if (Buffer.BlockedIndex < index) {
        //     CursorIndexes[Buffer.CurrentLineIndex] = index;

        //     Debug.Log("new index: " + index);
        //     // Buffer.Seek(index);
        //     // }
        // }

        // protected int GetCurrentCursorIndex() {
        //     return GetCursorIndex(Buffer.CurrentLineIndex);
        // }

        // protected int GetCursorIndex(int line) {
        //     try {
        //         return CursorIndexes[line];
        //     } catch (System.ArgumentOutOfRangeException) {
        //         CursorIndexes.Insert(line, 0);
        //         return 0;
        //     }
        // }
    }
}
