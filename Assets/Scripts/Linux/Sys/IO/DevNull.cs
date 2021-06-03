using Linux.IO;
using Linux.FileSystem;

namespace Linux.Sys.IO
{
    public class DevNull : AbstractTextIO {
        public DevNull() : base(AccessMode.O_RDWR) { }

        protected override void InternalTruncate() {
            //
        }

        protected override bool CanMovePointer(int newPosition) {
            return true;
        }

        protected override int InternalAppend(string data) {
            return 0;
        }

        protected override string InternalRead(int length) {
            return "";
        }

        protected override void InternalClose() {
            //
        }
    }
}