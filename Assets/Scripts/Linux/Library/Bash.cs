using Linux.Sys.RunTime;
using Linux.FileSystem;
using Linux.PseudoTerminal;
using Linux;
using UnityEngine;

namespace Linux.Library
{    
    public class Bash : CompiledBin {
        public Bash(
            string absolutePath,
            int uid,
            int gid,
            int permission,
            FileType type
        ) : base(absolutePath, uid, gid, permission, type) { }

        public override int Execute(UserSpace userSpace) {
            bool keepRunning = true;

            while (keepRunning) {
                string cwdName = PathUtils.BaseName(userSpace.GetCwd());
                string login = userSpace.GetLogin();

                string cmd = userSpace.Input(
                    $"[{login}@hacking01 {cwdName}]$",
                    ' '
                );

                if (string.IsNullOrEmpty(cmd)) {
                    continue;
                }

                if (cmd == "exit") {
                    keepRunning = false;
                } else {
                    try {
                        int pid = userSpace.CreateProcess(cmd.Split(' '));
                        userSpace.WaitPid(pid);
                    } catch (System.Exception exception) {
                        userSpace.Stderr.WriteLine(exception.Message);
                    }
                }
            }

            return 0;
        }
    }
}