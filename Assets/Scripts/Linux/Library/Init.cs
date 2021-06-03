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
            // int pid = userSpace.CreateProcess(
            //     new string[] { "/usr/sbin/ttyctl" }
            // );

            // userSpace.Print("getty pid: " + pid);
            Debug.Log("opening pty...");
            int pty = userSpace.Api.OpenPty();

            int shPid = userSpace.Api.StartProcess(
                new string[] { "/usr/bin/bash" },
                pty, pty, pty
            );

            userSpace.Api.WaitPid(shPid);

            return 0;
        }
    }
}