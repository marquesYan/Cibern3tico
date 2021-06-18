using Linux.Sys.RunTime;
using Linux.FileSystem;

namespace Linux.Library
{    
    public class True : CompiledBin {
        public True(
            string absolutePath,
            int uid,
            int gid,
            int permission,
            FileType type
        ) : base(absolutePath, uid, gid, permission, type) { }

        public override int Execute(UserSpace userSpace) {
            return 0;
        }
    }
}