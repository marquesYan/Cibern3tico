using System.Collections.Generic;

namespace Linux.PseudoTerminal
{
    public class BufferedStreamWriter {
        public List<string> Messages { get; protected set; }

        public int Size { get; protected set; }

        public BufferedStreamWriter(int maxSize) {
            Messages = new List<string>();
            Size = maxSize;
        }

        public int Write(string message) {
            int written = message.Length;

            Messages.Add(message);

            if (Messages.Count > Size) {
                written += Messages[0].Length;
                Messages.RemoveAt(0);
            }

            return written;
        }

        public void Clear() {
            Messages.Clear();
        }
    }
}