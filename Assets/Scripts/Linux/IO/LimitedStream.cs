using System.Text;

namespace Linux.IO
{
    public class LimitedStream : BufferedStream {
        protected StringBuilder Buffer = new StringBuilder();

        public int Length { get; protected set; }

        public int Size { get; protected set; }

        // public int Length {
        //     get {
        //         return Buffer.Length;
        //     }
        // }

        public LimitedStream(
            int maxSize
        ) : base(AccessMode.O_RDWR) {
            Size = maxSize;
            Length = 0;
        }

        public void Remove() {
            Buffer.Remove(Pointer, 1);
            Length--;
        }

        protected override void InternalTruncate() {
            Buffer.Clear();
        }

        protected override bool CanMovePointer(int newPosition) {
            return newPosition < Length;
        }

        protected override int InternalAppend(string data) {
            Buffer.Insert(Pointer, data);

            int written = data.Length;

            if (Buffer.Length > Size) {
                written -= 1;
                Buffer.Remove(0, 1);
            }

            Length += written;

            return written;
        }

        protected override string InternalRead(int length) {
            return Buffer.ToString();
        }

        protected override void InternalClose() {
            Buffer = null;
        }
    }
}