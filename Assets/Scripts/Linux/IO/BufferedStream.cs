using System.Text;
using System.Collections.Generic;

namespace Linux.IO
{
    public class BufferedStream : AbstractTextIO {
        protected StringBuilder Buffer = new StringBuilder();

        public BufferedStream(int mode) : base(mode) { }

        public static string ExhaustBuffer(
            StringBuilder buffer,
            int index,
            int length
        ) {
            string buf = buffer.ToString();
            bool isComplete = length == -1;

            if (isComplete) {
                length = buffer.Length;
            }

            buf = buf.Substring(index, length);
            buffer.Remove(index, length);

            return buf;
        }

        protected override void InternalTruncate() {
            Buffer.Clear();
        }

        protected override bool CanMovePointer(int newPosition) {
            return newPosition < Buffer.Length;
        }

        protected override int InternalAppend(string data) {
            Buffer.Append(data);
            return data.Length;
        }

        protected override string InternalRead(int length) {
            return ExhaustBuffer(Buffer, Pointer, length);
        }

        protected override void InternalClose() {
            Buffer = null;
        }
    }
}