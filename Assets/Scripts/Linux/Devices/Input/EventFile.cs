using System.Collections.Concurrent;
using System.Threading;
using Linux.FileSystem;

namespace Linux.Devices.Input {
    public class EventFile {
        public DriverEvent<string> Driver { get; protected set; }

        ConcurrentQueue<string> _internalQueue;

        // public EventFile(
        //     string absolutePath,
        //     int uid,
        //     int gid,
        //     int permission,
        //     DriverEvent<string> driver
        // ) : base(absolutePath, uid, gid, permission) {
        //     Driver = driver;

        //     _internalQueue = new ConcurrentQueue<string>();
        //     Driver.Subscribe(_internalQueue);
        // }

        // public EventFile(
        //     string absolutePath,
        //     int uid,
        //     int gid,
        //     int permission
        // ) : base(absolutePath, uid, gid, permission) {
        //     Driver = new DriverEvent<string>();

        //     _internalQueue = new ConcurrentQueue<string>();
        //     Driver.Subscribe(_internalQueue);
        // }

        // public  int Write(string[] data) {
        //     int written = 0;

        //     foreach(string key in data) {
        //         written += key.Length;
        //         Driver.Receive(key);
        //     }

        //     return written;
        // }

        // public  int Append(string[] data) {
        //     return Write(data);
        // }

        public  string Read() {
            // string data;

            // while (! _internalQueue.TryDequeue(out data)) {
            //     Thread.Sleep(200);
            // }

            return "";
        }

        public EventFile Duplicate() {
            // return new EventFile(
            //     Path,
            //     Uid,
            //     Gid,
            //     Perm,
            //     Driver
            // );
            return null;
        }

        // public  int Execute() {
        //     throw new System.InvalidOperationException("Attempt to execute in special file");
        //     return -1;
        // }
    }
}