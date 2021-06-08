using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Linux;
using UnityEngine;

namespace Linux.IO
{
    public static class AccessMode {
        public const int O_RDONLY = 0b_0100;
        public const int O_WRONLY = 0b_0010;
        public const int O_RDWR =   0b_0001;
        public const int O_APONLY = 0b_1000;

        public static bool CanWrite(int mode) {
            return (mode & (O_WRONLY | O_RDWR | O_APONLY)) != 0;
        }

        public static bool CanCreate(int mode) {
            return (mode & (O_WRONLY | O_RDWR)) != 0;
        }

        public static bool CanRead(int mode) {
            return (mode & (O_RDONLY | O_RDWR)) != 0;
        }
    }

    public abstract class AbstractTextIO : ITextIO {

        public const char LINE_FEED = '\n';
        protected readonly object StreamLock = new object();

        public int Mode { get; protected set; }
        public int Pointer { get; protected set; }
        public bool IsClosed { get; protected set; }
        public int Length { get; protected set; }

        public AbstractTextIO(int mode) {
            Mode = mode;
            IsClosed = false;
            Pointer = Length = 0;

            if (AccessMode.CanCreate(mode)) {
                Truncate();
            }
        }

        public virtual int GetLength() {
            return Length;
        }

        public int GetMode() {
            return Mode;
        }

        public void Truncate() {
            if (!AccessMode.CanCreate(Mode)) {
                ThrowIncorretMode("truncate");
            }
            
            InternalTruncate();
        }

        protected abstract void InternalTruncate();
        protected abstract int InternalAppend(string data);
        protected abstract string InternalRead(int length);

        protected abstract bool CanMovePointer(int newPosition);
        protected abstract void InternalClose();

        public void Dispose() {
            Close();
            GC.SuppressFinalize(this);
        }

        public virtual int WriteLine(string line) {
            return Write(line + LINE_FEED);
        }

        public virtual int WriteLines(string[] lines) {
            return Write(string.Join(""+LINE_FEED, lines));
        }

        public virtual string[] ReadLines() {
            return Read().Split(LINE_FEED);
        }

        public virtual string ReadLine() {
            string lineFeed = $"{LINE_FEED}";

            return ReadUntil(lineFeed).Replace(lineFeed, "");
        }

        public virtual string ReadUntil(params string[] input) {
            var content = new StringBuilder();

            bool missing = true;

            string buffer, compareBuf;

            while (Kernel.IsRunning && missing) {
                if (Length > 0) {
                    buffer = Read(1);
                    content.Append(buffer);

                    compareBuf = content.ToString();
                    
                    foreach (string inputChar in input) {
                        if (compareBuf.Contains(inputChar)) {
                            missing = false;
                            break;
                        }
                    }
                }
            }

            return content.ToString();
        }

        public void ThrowIncorretMode(string mode) {
            throw new System.AccessViolationException(
                $"Not opened in {mode} mode"
            );
        }

        public int Write(string data) {
            if (!AccessMode.CanWrite(Mode)) {
                ThrowIncorretMode("write");
            }

            int written;

            lock(StreamLock) {
                EnsureNotClosed();

                written = InternalAppend(data);
            }

            Length += written;

            return written;
        }

        public string Read(int length) {
            if (!AccessMode.CanRead(Mode)) {
                ThrowIncorretMode("read");
            }

            EnsureNotClosed();

            string data = InternalRead(length);

            Length -= data.Length;

            return data;
        }

        public string Read() {
            return Read(-1);
        }

        public void Seek(int position) {
            if (position < 0 && !CanMovePointer(position)) {
                throw new System.IO.IOException(
                    "Can not seek to position: " + position
                );
            }

            Pointer = position;
        }

        public void Close() {
            if (IsClosed) {
                return;
            }

            lock(StreamLock) {
                IsClosed = true;
                InternalClose();
            }
        }

        protected void EnsureNotClosed() {
            if (IsClosed) {
                throw new System.IO.EndOfStreamException(
                    "Can not operate in closed stream"
                );
            }
        }
    }
}