using Linux.Sys.RunTime;
using Linux.FileSystem;
using UnityEngine;

namespace Linux.Library
{    
    public class Echo : CompiledBin {
        public Echo(
            string absolutePath,
            int uid,
            int gid,
            int permission,
            FileType type
        ) : base(absolutePath, uid, gid, permission, type) { }

        public override int Execute(UserSpace userSpace) {
            string[] args = userSpace.GetArgs();
            args[0] = "";
            userSpace.Print(string.Join("", args));
            return 0;
        }
    }
}