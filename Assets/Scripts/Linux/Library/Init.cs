using Linux.Configuration;
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
            Linux.Kernel kernel = userSpace.Api.AccessKernel();

            userSpace.Api.Trap(
                ProcessSignal.SIGHUP,
                (int[] args) => {
                    // Graceful shutdown
                    kernel.KillAllChildProcesses(ProcessSignal.SIGTERM);

                    // Forced shutdown
                    kernel.KillAllChildProcesses(ProcessSignal.SIGKILL);
                }
            );

            if (userSpace.Api.FileExists("/run/init")) {
                // Start init service
                userSpace.Api.StartProcess(
                    new string[] { "/run/init" }
                );
            }

            Debug.Log("opening pty...");
            int pty = userSpace.Api.OpenPty();

            int shPid = userSpace.Api.StartProcess(
                new string[] { "/usr/sbin/login" },
                pty, pty, pty
            );

            userSpace.Api.WaitPid(shPid);

            int poweroffPid = userSpace.Api.StartProcess(
                new string[] { "/usr/sbin/poweroff" }
            );

            userSpace.Api.WaitPid(poweroffPid);

            return 0;
        }
    }
}