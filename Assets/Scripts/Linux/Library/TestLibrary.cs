using Linux.Sys.RunTime;
using Linux.FileSystem;
using UnityEngine;

namespace Linux.Library
{    
    public class TestLibrary : CompiledBin {
        public TestLibrary(
            string absolutePath,
            int uid,
            int gid,
            int permission,
            FileType type
        ) : base(absolutePath, uid, gid, permission, type) { }

        public override int Execute(UserSpace userSpace) {
            string answer = userSpace.Input("confirm? [Y/n]");
            userSpace.Print("answer is " + answer);
            return 0;
        }
    }
}