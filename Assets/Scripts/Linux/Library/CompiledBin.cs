using Linux.Sys.RunTime;
using Linux.FileSystem;

namespace Linux.Library
{    
    public abstract class CompiledBin : File {
        public CompiledBin(
            string absolutePath,
            int uid,
            int gid,
            int permission,
            FileType type
        ) : base(absolutePath, uid, gid, permission, type) { }

        public abstract int Execute(UserSpace userSpace);
    }
}