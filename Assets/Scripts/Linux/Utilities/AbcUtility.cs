using Linux.FileSystem;
using Linux;

namespace Linux.Utilities {
    public abstract class AbstractUtility : AbstractFile {
        public AbstractUtility(
            string absolutePath, 
            int uid,
            int gid, 
            int permission
        ) : base(absolutePath, uid, gid, permission) { }

        public override int Write(string[] data) {
            throw new System.InvalidOperationException("Attempt to write in special file");
            return -1;
        }

        public override int Append(string[] data) {
            throw new System.InvalidOperationException("Attempt to append in special file");
            return -1;
        }

        public override string Read() {
            return "Binary data.";
        }
    }
}