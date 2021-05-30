using System.Collections.Generic;

namespace Linux.IO
{
    public class BufferedStream : AbstractTextIO {
        protected string Buffer = "";

        public BufferedStream(int mode) : base(mode) { }

        protected override int InternalWrite(string data) {
            Buffer = data;
            return data.Length;
        }

        protected override int InternalAppend(string data) {
            return InternalWrite(Buffer + data);
        }

        protected override string InternalRead() {
            return Buffer;
        }

        protected override void InternalClose() {
            Buffer = null;
        }
    }
}