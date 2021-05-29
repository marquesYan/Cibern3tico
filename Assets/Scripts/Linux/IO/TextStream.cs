using System.Collections.Generic;

namespace Linux.IO
{
    public static class AccessMode {
        public const int O_RDONLY = 0b_0100;
        public const int O_WRONLY = 0b_0010;
        public const int O_RDWR = 0b_0001;
        public const int O_APONLY = 0b_1000;

        public static bool CanWrite(int mode) {
            return (mode & (O_WRONLY | O_RDWR)) != 0;
        }

        public static bool CanAppend(int mode) {
            return (mode & (O_APONLY | O_RDWR)) != 0;
        }

        public static bool CanRead(int mode) {
            return (mode & (O_RDONLY | O_RDWR)) != 0;
        }
    }

    public class TextStream {
        readonly object _bufferLock = new object();

        const char LINE_FEED = '\n';

        protected string Buffer;
        protected bool IsClosed = false;

        public int Mode { get; protected set; }

        public TextStream(string initialValue, int mode) {
            Buffer = initialValue;
            Mode = mode;
        }

        public void Close() {
            lock(_bufferLock) {
                IsClosed = true;
            }
        }

        public TextStream(int mode) : this("", mode) { }

        public int WriteLine(string line) {
            return Write(line + LINE_FEED);
        }

        public int AppendLine(string line) {
            return Append(line + LINE_FEED);
        }

        public int WriteLines(string[] lines) {
            return Write(string.Join(""+LINE_FEED, lines));
        }

        public int AppendLines(string[] lines) {
            return Append(string.Join(""+LINE_FEED, lines));
        }

        public string[] ReadLines() {
            return Read().Split(LINE_FEED);
        }

        public int Write(string data) {
            if (!AccessMode.CanWrite(Mode)) {
                ThrowIncorretMode("write");
            }

            int written = data.Length;

            lock(_bufferLock) {
                EnsureNotClosed();
                Buffer = data;
            }

            return written;
        }

        public int Append(string data) {
            if (!AccessMode.CanAppend(Mode)) {
                ThrowIncorretMode("append");
            }

            int written = data.Length;

            lock(_bufferLock) {
                EnsureNotClosed();
                Buffer += data;
            }

            return written;
        }

        public string Read() {
            if (!AccessMode.CanRead(Mode)) {
                ThrowIncorretMode("read");
            }

            EnsureNotClosed();

            return Buffer;
        }

        protected void EnsureNotClosed() {
            if (IsClosed) {
                throw new System.IO.EndOfStreamException(
                    "Can not operate in closed stream"
                );
            }
        }

        public void ThrowIncorretMode(string mode) {
            throw new System.AccessViolationException(
                $"Not opened in {mode} mode"
            );
        }
    }

    public class TextStreamWrapper {
        protected TextStream StreamBackend;

        public int Mode { get; protected set; }

        public TextStreamWrapper(TextStream stream, int mode) {
            StreamBackend = stream;
            Mode = mode;
        }

        public void Close() {
            //
        }

        public int WriteLine(string line) {
            return StreamBackend.WriteLine(line);
        }

        public int AppendLine(string line) {
            return StreamBackend.AppendLine(line);
        }

        public int WriteLines(string[] lines) {
            return StreamBackend.WriteLines(lines);
        }

        public int AppendLines(string[] lines) {
            return StreamBackend.AppendLines(lines);
        }

        public string[] ReadLines() {
            return StreamBackend.ReadLines();
        }

        public int Write(string data) {
            if (!AccessMode.CanWrite(Mode)) {
                StreamBackend.ThrowIncorretMode("write");
            }

            return StreamBackend.Write(data);
        }

        public int Append(string data) {
            if (!AccessMode.CanAppend(Mode)) {
                StreamBackend.ThrowIncorretMode("append");
            }

            return StreamBackend.Append(data);
        }

        public string Read() {
            if (!AccessMode.CanRead(Mode)) {
                StreamBackend.ThrowIncorretMode("read");
            }

            return StreamBackend.Read();
        }
    }
}