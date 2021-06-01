using System.Text;
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
}