using System.Collections.Generic;
using Linux.Configuration;
using Linux.Sys.RunTime;
using Linux.FileSystem;
using Linux;
using UnityEngine;


namespace Linux.Library.ShellInterpreter
{
    public class BashProcess {
        protected UserSpace UserSpace;

        protected Dictionary<string, string> Environment;

        protected Dictionary<string, string> Variables;

        protected string Login;

        public BashProcess(UserSpace userSpace) {
            UserSpace = userSpace;
            Environment = userSpace.Api.GetEnviron();
            Login = userSpace.Api.GetLogin();

            Variables = new Dictionary<string, string>();

            SetupDefaultEnvironment();
        }

        public bool Run() {
            string cwdName = PathUtils.BaseName(UserSpace.Api.GetCwd());

            string cmd = UserSpace.Input(
                $"[{Login}@hacking01 {cwdName}]$",
                ' '
            );
            Debug.Log("recv cmd: " + cmd);
            if (string.IsNullOrEmpty(cmd)) {
                return true;
            }

            if (cmd == "exit") {
                return false;
            }

            try {
                string[] cmdLine = ParseCmd(cmd);
                int pid = UserSpace.Api.StartProcess(cmdLine);
                UserSpace.Api.WaitPid(pid);
            } catch (System.Exception exception) {
                UserSpace.Stderr.WriteLine(exception.Message);
            }

            return true;
        }

        protected void SetupDefaultEnvironment() {
            Environment.Add("PATH", "/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin");
            Environment.Add("OLDPWD", Environment["PWD"]);
        }

        protected string[] ParseCmd(string cmd) {
            List<string> tokens = new List<string>();
            Stack<char> stack = new Stack<char>();

            for (var i = 0; i < cmd.Length; i++) {
                char token = cmd[i];

                switch (token) {
                    // case '"':
                    // case '\'': {
                    //     stack.Push(token);
                    //     break;
                    // }

                    case '$': {
                        if (cmd[i + 1] == '$') {
                            i++;
                            tokens.Add(UserSpace.Api.GetPid().ToString());
                        }
                        break;
                    }

                    default: {
                        tokens.Add(token.ToString());
                        break;
                    }
                }
            }

            return string.Join("", tokens).Split(' ');
        }
    }
}