using System.Collections.Generic;
using Linux.FileSystem;

namespace Linux.Utilities {
    public class PwdUtility : AbstractFile {
        public PwdUtility(string path, Perms[] permissions) : base(path, permissions) { }

        public override int Write(string[] data) {
            throw new System.InvalidOperationException("Attempt to write in special file");
            return -1;
        }

        public override string Read() {
            return "Binary data.";
        }

        public override int Execute(string[] args) {
            // Subsystem.Singleton.AddStream(Subsystem.Singleton.Sh.Cwd.Path);
            return 0;
        }
    }
}