using System.Text;
using System.Collections.Generic;
using Linux.IO;

namespace Linux.PseudoTerminal
{
    public abstract class VirtualTerminal
    {
        event System.Action OnFirstDraw;
        bool _moveCursorToEnd = false;
        int _moveYAxis = -1;
        int _moveXAxis = -1;
        int _typedKeysCount;

        protected AbstractTextIO Buffer;

        protected bool IsFirstDraw = true;

        protected string KeyboardEvent;

        protected CursorDisplay CursorManager;

        public bool IsClosed { get; protected set; }

        public VirtualTerminal(int bufferSize) {
            Buffer = new LimitedStream(bufferSize);

            CursorManager = new CursorDisplay();

            IsClosed = false;
        }

        public void Close() {
            Buffer.Close();
            IsClosed = true;
        }

        public void ClearBuffer() {
            Buffer.Truncate();
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
            if (message.StartsWith(CharacterControl.C_WRITE_KEY)) {
                string key = message.Remove(0, CharacterControl.C_WRITE_KEY.Length);
                HandleInputKey(key);
                return 1;
            }

            RequestHighYAxis();
            return Buffer.Write(message);
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
            foreach (string message in Buffer.ReadLines()) {
                switch (message) {
                    default: {
                        DrawLine(message);
                        break;
                    }
                }
            }
        }

        protected void HandleInputKey(string key) {
            switch(key) {
                case CharacterControl.C_DBACKSPACE: {
                    if (CursorManager.CursorPosition > 0) {
                        CursorManager.RemoveAtCursorPosition(-1);
                    }
                    break;
                }

                case CharacterControl.C_DDELETE: {
                    if (!CursorManager.IsAtEnd()) {
                        // Delete token at cursor position
                        CursorManager.RemoveAtCursorPosition(0);
                    }
                    break;
                }

                case CharacterControl.C_DLEFT_ARROW: {
                    CursorManager.Move(-1);
                    break;
                }

                case CharacterControl.C_DRIGHT_ARROW: {
                    CursorManager.Move(1);
                    break;
                }

                default: {
                    CursorManager.AddToCollection(key);
                    break;
                }
            }
        }
    }
}
