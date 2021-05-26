using System.Text;
using System.Collections;
using UnityEngine;

namespace Linux.PseudoTerminal
{
    public static class InputChar {
        public const string ScrollAllDown = "^S";
    }

    public abstract class VirtualTerminal
    {
        protected BufferedStreamWriter Buffer;

        bool _moveCursorToEnd = false;
        int _moveYAxis = -1;
        int _moveXAxis = -1;

        public bool IsFirstDraw { get; protected set; }

        public Event LastEvent { get; protected set; }
        public string LastTextInput { get; protected set; }

        public VirtualTerminal(int bufferSize) {
            Buffer = new BufferedStreamWriter(bufferSize);
            IsFirstDraw = true;
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

        public string Read() {
            return "from vt";
        }

        protected abstract void MoveYAxis(int position);
        protected abstract void MoveXAxis(int position);
        protected abstract void DrawLine(string message);

        public abstract void MoveCursorToEnd();

        protected virtual void OnFirstDraw() { }

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
                    case InputChar.ScrollAllDown: {
                        RequestYAxis(0);
                        break;
                    }

                    default: {
                        DrawLine(message);
                        break;
                    }
                }
            }
        }
    }
}
