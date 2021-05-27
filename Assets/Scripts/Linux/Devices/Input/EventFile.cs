using System.Collections.Concurrent;
using System.Threading;
using Linux.FileSystem;

namespace Linux.Devices.Input {
    public class EventFile : AbstractFile {
        public DriverEvent<string> Driver { get; protected set; }

        ConcurrentQueue<string> _internalQueue;

        public EventFile(
            string absolutePath, 
            int uid,
            int gid,
            int permission
        ) : base(absolutePath, uid, gid, permission) {
            Driver = new DriverEvent<string>();

            _internalQueue = new ConcurrentQueue<string>();
            Driver.Subscribe(_internalQueue);
        }

        public override int Write(string[] data) {
            int written = 0;

            foreach(string key in data) {
                written += key.Length;
                Driver.Receive(key);
            }

            return written;
        }

        public override int Append(string[] data) {
            return Write(data);
        }

        public override string Read() {
            string data;

            while (! _internalQueue.TryDequeue(out data)) {
                Thread.Sleep(200);
            }

            return data;
        }

        public override int Execute(string[] arguments) {
            throw new System.InvalidOperationException("Attempt to execute in special file");
            return -1;
        }
    }
}