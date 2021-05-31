using System.Collections.Generic;
using System.Text;

namespace Linux.IO
{
    public static class AccessMode {
        public const int O_RDONLY = 0b_0100;
        public const int O_WRONLY = 0b_0010;
        public const int O_RDWR = 0b_0001;
        public const int O_APONLY = 0b_1000;

        public static bool CanWrite(int mode) {
            return (mode & (O_WRONLY | O_RDWR | O_APONLY)) != 0;
        }

        public static bool CanRead(int mode) {
            return (mode & (O_RDONLY | O_RDWR)) != 0;
        }
    }

    public abstract class AbstractTextIO : ITextIO {

        const char LINE_FEED = '\n';
        protected readonly object StreamLock = new object();

        public int Mode { get; protected set; }
        public bool IsClosed { get; protected set; }

        public AbstractTextIO(int mode) {
            Mode = mode;
            IsClosed = false;
        }

        protected abstract int InternalWrite(string data);
        protected abstract int InternalAppend(string data);
        protected abstract string InternalRead();
        protected abstract void InternalClose();

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
            var content = new StringBuilder();

            bool missingLineFeed = true;

            string lineFeed = $"{LINE_FEED}";

            while (missingLineFeed) {
                foreach(string input in Read().Split()) {
                    if (input == lineFeed) {
                        missingLineFeed = false;
                        break;
                    } else {
                        content.Append(input);
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

                if ((Mode & AccessMode.O_APONLY) > 0) {
                    written = InternalAppend(data);
                } else {
                    written = InternalWrite(data);
                }
            }

            return written;
        }

        public string Read() {
            if (!AccessMode.CanRead(Mode)) {
                ThrowIncorretMode("read");
            }

            EnsureNotClosed();

            return InternalRead();
        }

        public void Close() {
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