using System.Collections.Generic;

namespace Linux.IO
{
    public class LimitedStream : AbstractTextIO {
        public int Size { get; protected set; }

        public List<string> Buffer { get; protected set; }

        public LimitedStream(
            int maxSize
        ) : base(AccessMode.O_RDWR) {
            Size = maxSize;
        }

        public override string[] ReadLines() {
            return Buffer.ToArray();
        }

        protected override int InternalWrite(string data)
        {
            return InternalAppend(data);
        }

        protected override int InternalAppend(string data) {
            int written = data.Length;

            Buffer.Add(data);

            if (Buffer.Count > Size) {
                written += Buffer[0].Length;
                Buffer.RemoveAt(0);
            }

            return written;
        }

        protected override void InternalClose() {
            Buffer = null;
        }

        protected override string InternalRead()
        {
            throw new System.ArgumentException(
                "Can not Read from LimitedStream, instead call ReadLines()"
            );
        }
    }
}