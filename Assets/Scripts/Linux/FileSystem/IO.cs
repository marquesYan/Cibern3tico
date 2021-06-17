using System.Text;
using SysFile = System.IO.File;
using Linux.IO;

namespace Linux.FileSystem {
    public class TempStreamWrapper : AbstractTextIO {
        protected ITextIO StreamBackend;

        protected StringBuilder InternalBuffer = new StringBuilder();

        public TempStreamWrapper(
            ITextIO stream,
            int mode
        ) : base(mode) {
            StreamBackend = stream;

            // Maybe truncate
            if (AccessMode.CanCreate(mode)) {
                Truncate();
            }

            FillInternalBuffer();
        }

        protected override void InternalClose() {
            InternalBuffer = null;
        }

        protected override string InternalRead(int length) {
            return BufferedStream.ExhaustBuffer(InternalBuffer, Pointer, length);
        }

        protected override int InternalAppend(string data) {
            InternalBuffer.Append(data);
            return StreamBackend.Write(data);
        }

        protected override bool CanMovePointer(int newPosition) {
            StreamBackend.Seek(newPosition);
            return newPosition < InternalBuffer.Length;
        }

        protected override void InternalTruncate() {
            if (StreamBackend != null) {
                InternalBuffer.Clear();
                StreamBackend.Truncate();
            }
        }

        protected void FillInternalBuffer() {
            InternalAppend(StreamBackend.Read());

            Length = InternalBuffer.Length;
        }
    }

    public class LocalFileStream : AbstractTextIO {
        protected string Path;

        public LocalFileStream(
            string path,
            int mode
        ) : base(mode) {
            Path = path;

            if (AccessMode.CanCreate(mode)) {
                Truncate();
            }
        }

        protected override void InternalTruncate() {
            if (Path != null) {
                SysFile.WriteAllText(Path, "");
            }
        }

        protected override int InternalAppend(string data) {
            SysFile.AppendAllText(Path, data);
            return data.Length;
        }

        protected override string InternalRead(int length) {
            return SysFile.ReadAllText(Path);
        }

        protected override bool CanMovePointer(int newPosition) {
            return false;
        }

        protected override void InternalClose() {
            //
        }
    }
}