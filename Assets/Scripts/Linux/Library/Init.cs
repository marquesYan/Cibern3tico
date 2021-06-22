using System.Threading;
using Linux.Configuration;
using Linux.Sys.RunTime;
using Linux.FileSystem;
using Linux.IO;
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

            bool eventSet = true;

            userSpace.Api.Trap(
                ProcessSignal.SIGHUP,
                (int[] args) => {
                    // Graceful shutdown
                    kernel.KillAllChildProcesses(ProcessSignal.SIGTERM);

                    // Forced shutdown
                    kernel.KillAllChildProcesses(ProcessSignal.SIGKILL);

                    eventSet = false;
                }
            );

            if (userSpace.Api.FileExists("/run/init")) {
                int logFd = userSpace.Api.Open("/var/log/init.log", AccessMode.O_WRONLY);
                // Start init service
                userSpace.Api.StartProcess(
                    new string[] { "/run/init" },
                    0,
                    logFd,
                    logFd
                );
            }

            int pty = userSpace.Api.OpenPty();

            int shPid = userSpace.Api.StartProcess(
                new string[] { "/usr/sbin/login" },
                pty, pty, pty
            );

            Process mainProc = kernel.ProcTable.LookupPid(shPid);

            while (eventSet && mainProc.MainTask.IsAlive) {
                // Do kernel mantainance

                foreach (Process proc in kernel.ProcTable.GetProcesses()) {
                    if (proc.MainTask.ThreadState == ThreadState.Stopped) {
                        kernel.ProcTable.Remove(proc);
                    }
                }

                Thread.Sleep(500);
            }

            int poweroffPid = userSpace.Api.StartProcess(
                new string[] { "/usr/sbin/poweroff" }
            );

            userSpace.Api.WaitPid(poweroffPid);

            return 0;
        }
    }
}