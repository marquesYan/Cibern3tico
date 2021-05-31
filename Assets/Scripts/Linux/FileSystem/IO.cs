using Linux.IO;

namespace Linux.FileSystem {
    public class TextStreamWrapper : ITextIO {
        protected AbstractTextIO StreamBackend;

        public int Mode { get; protected set; }
        public bool IsClosed { get; protected set; }

        public TextStreamWrapper(
            AbstractTextIO stream,
            int mode
        ) {
            StreamBackend = stream;
            Mode = mode;
        }

        public void Close() {
            //
        }

        public int WriteLine(string line) {
            return StreamBackend.WriteLine(line);
        }

        public int WriteLines(string[] lines) {
            return StreamBackend.WriteLines(lines);
        }

        public string[] ReadLines() {
            return StreamBackend.ReadLines();
        }

        public string ReadLine() {
            return StreamBackend.ReadLine();
        }

        public int Write(string data) {
            if (!AccessMode.CanWrite(Mode)) {
                StreamBackend.ThrowIncorretMode("write");
            }

            return StreamBackend.Write(data);
        }

        public string Read() {
            if (!AccessMode.CanRead(Mode)) {
                StreamBackend.ThrowIncorretMode("read");
            }

            return StreamBackend.Read();
        }
    }
}