using System.Threading;
using System.Text;
using System.Collections.Generic;
using Linux.IO;

namespace Linux.Sys.IO
{
    public class CharacterDevice : AbstractTextIO, IoctlDevice
    {
        protected Queue<string> Buffer = new Queue<string>();

        public CharacterDevice(int mode) : base(mode) { }

        public virtual void Ioctl(ushort signal, ref ushort[] args) {
            //
        }

        protected override void InternalTruncate() {
            Buffer.Clear();
        }

        protected override int InternalAppend(string data) {
            foreach (char inputChar in data) {
                Buffer.Enqueue(inputChar.ToString());
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