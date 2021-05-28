using System.Text;
using System.Collections.Generic;
using Linux.Devices.Input;
using UnityEngine;

namespace Linux.PseudoTerminal
{
    public static class CharacterControl {
        public const string C_DBACKSPACE = "^BKS";
        public const string C_DDELETE = "^DEL";
        public const string C_DTAB = "^TAB";
        public const string C_DESCAPE = "^ESC";
        public const string C_DLEFT_SHIFT = "^LSH";
        public const string C_DRIGHT_SHIFT = "^RSH";
        public const string C_DCTRL = "^CTR";
        public const string C_DUP_ARROW = "^UPA";
        public const string C_DDOWN_ARROW = "^DOA";
        public const string C_DLEFT_ARROW = "^LEA";
        public const string C_DRIGHT_ARROW = "^RIA";

        public const string C_UBACKSPACE = "BKS^";
        public const string C_UDELETE = "DEL^";
        public const string C_UTAB = "TAB^";
        public const string C_UESCAPE = "ESC^";
        public const string C_ULEFT_SHIFT = "LSH^";
        public const string C_URIGHT_SHIFT = "RSH^";
        public const string C_UCTRL = "CTR^";
        public const string C_UUP_ARROW = "UPA^";
        public const string C_UDOWN_ARROW = "DOA^";
        public const string C_ULEFT_ARROW = "LEA^";
        public const string C_URIGHT_ARROW = "RIA^";
    }

    public abstract class VirtualTerminal
    {
        event System.Action OnFirstDraw;
        bool _moveCursorToEnd = false;
        int _moveYAxis = -1;
        int _moveXAxis = -1;
        int _typedKeysCount;

        protected BufferedStreamWriter Buffer;

        protected EventFile KeyboardEvent;
        protected bool IsFirstDraw = true;

        protected CursorDisplay CursorManager;

        public bool IsClosed { get; protected set; }

        public VirtualTerminal(int bufferSize, EventFile keyboardEvent) {
            Buffer = new BufferedStreamWriter(bufferSize);
            KeyboardEvent = keyboardEvent;

            CursorManager = new CursorDisplay();

            IsClosed = false;

            new EventController(this, keyboardEvent.Duplicate());
        }

        public void Close() {
            IsClosed = true;
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

        public void ClearBuffer() {
            Buffer.Clear();
        }

        public void RequestCursorToEnd() {
            _moveCursorToEnd = true;
        }

        public int Write(string message) {
            return Buffer.Write(message);
        }

        public string ReadLine() {
            var buffer = new StringBuilder();

            string lastChar = "";

            while (lastChar != "\n") {
                lastChar = KeyboardEvent.Read();
                buffer.Append(lastChar);
            }

            return buffer.ToString();
        }

        public void WriteKey(string key) {
            HandleInputKey(key);
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
            foreach (string message in Buffer.Messages) {
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
