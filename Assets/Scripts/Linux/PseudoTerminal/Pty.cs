using Linux.FileSystem;
using Linux.IO;

namespace Linux.PseudoTerminal
{
    public class Pty : AbstractTextIO
    {
        protected ITextIO RBuffer;

        protected ITextIO WBuffer;

        public Pty(
            ITextIO rBuffer,
            ITextIO wBuffer
        ) : base(AccessMode.O_RDWR) {
            RBuffer = rBuffer;
            WBuffer = wBuffer;
        }

        protected override void InternalTruncate() {
            //
        }

        protected override int InternalAppend(string data) {
            WBuffer.Write(data);

            return data.Length;
        }

        protected override bool CanMovePointer(int newPosition) {
            return false;
        }

        protected override string InternalRead(int length) {
            return RBuffer.Read(length);
        }
        protected override void InternalClose() {
            RBuffer = null;
            WBuffer = null;
        }
    }
}