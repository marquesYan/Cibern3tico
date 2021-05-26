using System.IO;
using Linux.FileSystem;
using Linux;

namespace Linux.Devices {
    public abstract class AbstractDevice : AbstractFile {
        public AbstractDevice(string path, Perms[] permissions) : base(path, permissions) { }

        public override int Write(string[] data) {
            throw new System.InvalidOperationException("Attempt to write in special file");
            return -1;
        }

        public override int Execute(string[] arguments) {
            throw new System.InvalidOperationException("Attempt to execute in special file");
            return -1;
        }
    }
}