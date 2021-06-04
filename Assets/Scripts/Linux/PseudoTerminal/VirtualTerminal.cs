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

        public void RemoveCharAtFront() {
            CursorLinesMngr.RemoveAtFront();
        }

        public void RemoveCharAtBack() {
            CursorLinesMngr.RemoveAtBack();
        }

        public void MoveCursorUp() {
            CursorLinesMngr.MoveLine(-1);
        }

        public void MoveCursorDown() {
            CursorLinesMngr.MoveLine(1);
        }

        public void MoveCursorRight() {
            CursorLinesMngr.MoveCursor(1);
        }

        public void MoveCursorLeft() {
            CursorLinesMngr.MoveCursor(-1);
        }

        public int WriteToScreen(string message) {
            CursorLinesMngr.Add(message);
            RequestHighYAxis();
            return message.Length;
        }

        public void ReceiveKey(string key) {
            CursorLinesMngr.AddKey(key);
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
                    string lineToCalc = line;
                    if (string.IsNullOrEmpty(line)) {
                        // Calculate the height of a real character
                        lineToCalc = "A";
                    }

                    CursorSize = CalcSize(lineToCalc);
                }

                i++;
            }
        }
    }
}
