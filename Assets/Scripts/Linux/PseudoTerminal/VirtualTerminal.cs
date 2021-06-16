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
            CursorLinesMngr.ClearUntilLastLine();
        }

        public void RequestHighXAxis() {
            MoveXAxis(int.MaxValue);
        }

        public void RequestHighYAxis() {
            MoveYAxis(int.MaxValue);
        }

        public void RequestLowYAxis() {
            MoveYAxis(int.MinValue);
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

        public void DrawGUI() {
            if (! IsClosed) {
                HandleDraw();
            }
        }

        protected abstract void MoveYAxis(int position);
        protected abstract void MoveXAxis(int position);
        protected abstract void DrawLine(string message);
        protected abstract void HandleDraw();
        protected abstract float GetScreenWidth();

        protected abstract Vector CalcSize(string message);

        protected void DrawTerm() {
            FlushBuffer();
        }

        protected void FlushBuffer() {
            int i = 0;
            float screenWidth = GetScreenWidth() - 32;

            foreach (string line in CursorLinesMngr.GetLines()) {
                DrawLine(line);

                Vector vector = CalcSize(line);

                if (vector.X >= screenWidth) {
                    int position = line.Length;
                    Vector remove;

                    do {
                        remove = CalcSize(
                            line.Substring(0, position)
                        );
                        position--;
                    } while (remove.X >= screenWidth);

                    CursorLinesMngr.WrapAt(line, position);
                }

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
