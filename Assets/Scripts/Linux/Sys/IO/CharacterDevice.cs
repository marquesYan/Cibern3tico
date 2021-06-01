using System.Threading;
using System.Text;
using System.Collections.Generic;
using Linux.IO;

namespace Linux.Sys.IO
{
    public class CharacterDevice : AbstractTextIO
    {
        protected Queue<string> Buffer = new Queue<string>();

        public CharacterDevice(int mode) : base(mode) { }

        protected override void InternalTruncate() {
            Buffer.Clear();
        }

        protected override int InternalAppend(string data) {
            foreach(string input in data.Split()) {
                Buffer.Enqueue(input);
            }

            return data.Length;
        }

        protected override bool CanMovePointer(int newPosition) {
            return false;
        }

        protected override string InternalRead(int length) {
            string outputChar;

            while (!TryDequeue(out outputChar)) {
                Thread.Sleep(200);
            }

            return outputChar;
        }
        protected override void InternalClose() {
            Buffer = null;
        }

        protected bool TryDequeue(out string output) {
            output = null;

            try {
                output = Buffer.Dequeue();                
                return true;
            } catch (System.InvalidOperationException) {
                return false;
            }
        }
    }
}