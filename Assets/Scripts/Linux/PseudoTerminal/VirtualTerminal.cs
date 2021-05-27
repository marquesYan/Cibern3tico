using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Concurrent;
using Linux.Devices.Input;

namespace Linux.PseudoTerminal
{
    public static class InputChar {
        public const string ScrollAllDown = "^S";
    }

    public abstract class VirtualTerminal
    {
        event System.Action OnFirstDraw;
        protected BufferedStreamWriter Buffer;

        protected event System.Action<string> OnEndOfLine;

        protected EventFile KeyboardEvent;

        bool _moveCursorToEnd = false;
        int _moveYAxis = -1;
        int _moveXAxis = -1;

        object _lastEvent;

        public bool IsFirstDraw { get; protected set; }
        public bool IsClosed { get; protected set; }

        public object LastEvent { 
            get { return _lastEvent; }
            protected set {
                _lastEvent = value;
                OnEventRecv(); 
            }
        }
        protected string LastTextInput { get; set; }

        public VirtualTerminal(int bufferSize, EventFile keyboardEvent) {
            Buffer = new BufferedStreamWriter(bufferSize);
            KeyboardEvent = keyboardEvent;
            IsFirstDraw = true;

            LastTextInput = "";

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
            LastTextInput += key;
        }

        public void SubscribeRead(System.Action<string> onEol) {
            OnEndOfLine = onEol;
        }

        public void SubscribeFirstDraw(System.Action onFirstDraw) {
            OnFirstDraw += onFirstDraw;
        }

        protected abstract void MoveYAxis(int position);
        protected abstract void MoveXAxis(int position);
        protected abstract void DrawLine(string message);

        protected abstract bool HasReturnEvent();

        public abstract void MoveCursorToEnd();

        protected virtual void OnEventRecv() {
            if (HasReturnEvent() && OnEndOfLine != null) {
                System.Action<string> localAction = OnEndOfLine;

                OnEndOfLine = null;

                localAction(LastTextInput);
            }
        }

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
