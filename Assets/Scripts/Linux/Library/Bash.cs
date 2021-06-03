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

            Debug.Log("bash is running");
            Debug.Log("bash stdin: " + userSpace.Stdin);

            while (keepRunning) {
                string cwdName = PathUtils.BaseName(userSpace.Api.GetCwd());
                string login = userSpace.Api.GetLogin();

                Debug.Log("waiting bash input: ");
                string cmd = userSpace.Input(
                    $"[{login}@hacking01 {cwdName}]$",
                    ' '
                );
                Debug.Log("recv cmd: " + cmd);
                if (string.IsNullOrEmpty(cmd)) {
                    continue;
                }

                if (cmd == "exit") {
                    keepRunning = false;
                } else {
                    try {
                        string[] cmdLine = ParseCmd(userSpace, cmd);
                        int pid = userSpace.Api.StartProcess(cmdLine);
                        userSpace.Api.WaitPid(pid);
                    } catch (System.Exception exception) {
                        userSpace.Stderr.WriteLine(exception.Message);
                    }
                }
            }

            return 0;
        }

        protected string[] ParseCmd(UserSpace userSpace, string cmd) {
            string[] tokens = cmd.Split(' ');

            for (var i = 0; i < tokens.Length; i++) {
                string token = tokens[i];

                switch (token) {
                    case "$$": {
                        tokens[i] = userSpace.Api.GetPid().ToString();
                        break;
                    }
                }
            }

            return tokens;
        }
    }
}