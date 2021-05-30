using System.Threading;
using System.Collections.Generic;
using Linux.IO;

namespace Linux.Sys.IO
{
    public class CharacterDevice : AbstractTextIO
    {
        protected Queue<string> Buffer;

        public CharacterDevice(int mode) : base(mode) {
            Buffer = new Queue<string>();
        }

        protected override int InternalWrite(string data) {
            return InternalAppend(data);
        }
        protected override int InternalAppend(string data) {
            foreach(string input in data.Split()) {
                Buffer.Enqueue(input);
            }

            return data.Length;
        }
        protected override string InternalRead() {
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