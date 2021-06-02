using Linux.Sys.RunTime;
using Linux.FileSystem;
using UnityEngine;

namespace Linux.Library
{    
    public class Init : CompiledBin {
        public Init(
            string absolutePath,
            int uid,
            int gid,
            int permission,
            FileType type
        ) : base(absolutePath, uid, gid, permission, type) { }

        public override int Execute(UserSpace userSpace) {
            int pid = userSpace.CreateProcess(
                new string[] { "/usr/sbin/ttyctl" }
            );

            userSpace.Print("getty pid: " + pid);

            int shPid = userSpace.CreateProcess(
                new string[] { "/usr/bin/bash" }
            );

            userSpace.WaitPid(shPid);

            return 0;
        }
    }
}