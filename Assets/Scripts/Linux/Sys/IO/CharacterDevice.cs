using System.Threading;
using System.Text;
using System.Collections.Generic;
using Linux.IO;
using Linux;

namespace Linux.Sys.IO
{
    public class CharacterDevice : AbstractTextIO, IoctlDevice
    {
        protected Queue<string> Buffer = new Queue<string>();

        public CharacterDevice(int mode) : base(mode) { }

        public override int GetLength() {
            return Buffer.Count;
        }

        public virtual void Ioctl(ushort signal, ref ushort[] args) {
            //
        }

        public virtual void Ioctl(ushort signal, string arg) {
            //
        }

        protected override void InternalTruncate() {
            Buffer.Clear();
        }

        protected override int InternalAppend(string data) {
            Buffer.Enqueue(data);
            return data.Length;
        }

        protected override bool CanMovePointer(int newPosition) {
            return false;
        }

        protected override string InternalRead(int length) {
            string outputChar = null;

            while (Kernel.IsRunning && !TryDequeue(out outputChar)) {
                Thread.Sleep(200);
            }

            // Ensure return a string object
            if (outputChar == null) {
                outputChar = "";
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