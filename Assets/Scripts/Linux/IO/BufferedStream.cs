using System.Collections.Generic;

namespace Linux.IO
{
    public class BufferedStreamWriter {
        protected List<string> Buffer;

        public int Size { get; protected set; }

        public BufferedStreamWriter(int maxSize) {
            Buffer = new List<string>();
            Size = maxSize;
        }

        public int Write(string message) {
            int written = message.Length;

            Buffer.Add(message);

            if (Buffer.Count > Size) {
                written += Buffer[0].Length;
                Buffer.RemoveAt(0);
            }

            return written;
        }

        public string[] ToArray() {
            return Buffer.ToArray();
        }

        public void Clear() {
            Buffer.Clear();
        }
    }
}