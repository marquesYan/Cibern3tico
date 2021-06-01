using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using Linux.FileSystem;
using Linux.IO;

namespace Linux.PseudoTerminal
{
    public class SecondaryPty : AbstractTextIO {
        public delegate int WAction(string data);

        protected ConcurrentQueue<string> ReadQueue;
        protected WAction WriteAction;

        public SecondaryPty(
            ConcurrentQueue<string> readQueue,
            WAction writeAction
        ) : base(AccessMode.O_RDWR) {
            ReadQueue = readQueue;
            WriteAction = writeAction;
        }

        protected override void InternalTruncate() {
            //
        }

        protected override int InternalAppend(string data) {
            WriteAction(data);

            return data.Length;
        }

        protected override bool CanMovePointer(int newPosition) {
            return false;
        }

        protected override string InternalRead(int length) {
            string output;

            while (!ReadQueue.TryDequeue(out output)) {
                Thread.Sleep(200);
            }

            return output;
        }

        protected override void InternalClose() {
            ReadQueue = null;
            WriteAction = null;
        }

        // protected bool TryDequeueRead(out string output) {
        //     output = null;

        //     try {
        //         output = ReadQueue.Dequeue();
        //         return true;
        //     } catch (System.InvalidOperationException) {
        //         return false;
        //     }
        // }
    }

    public class PrimaryPty : AbstractTextIO
    {
        protected List<ConcurrentQueue<string>> SecondaryPtyReadQueues;

        protected ITextIO RBuffer;

        protected ITextIO WBuffer;

        public PrimaryPty(
            ITextIO rBuffer,
            ITextIO wBuffer
        ) : base(AccessMode.O_RDWR) {
            RBuffer = rBuffer;
            WBuffer = wBuffer;
            SecondaryPtyReadQueues = new List<ConcurrentQueue<string>>();
        }

        public SecondaryPty CreateSecondary() {
            var rQueue = new ConcurrentQueue<string>();

            SecondaryPtyReadQueues.Add(rQueue);

            return new SecondaryPty(rQueue, Write);
        }

        protected override void InternalTruncate() {
            //
        }

        protected override int InternalAppend(string data) {
            WBuffer.Write(data);

            return data.Length;
        }

        protected override bool CanMovePointer(int newPosition) {
            return false;
        }

        protected override string InternalRead(int length) {
            string buffer = RBuffer.Read(length);

            SecondaryPtyReadQueues.ForEach(queue => {
                queue.Enqueue(buffer);
            });

            return buffer;
        }
        protected override void InternalClose() {
            RBuffer = null;
            WBuffer = null;
            SecondaryPtyReadQueues = null;
        }
    }
}