using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using PushdownAutomaton;
using Linux.Configuration;
using Linux.Sys.RunTime;
using Linux.FileSystem;
using Linux.Library.ShellInterpreter.Builtins;
using Linux;
using UnityEngine;


namespace Linux.Library.ShellInterpreter
{
    public class BashProcess {
        protected Regex SetVariablesRegex = new Regex(@"^([a-zA-Z_]+)=([a-zA-Z0-9_]*)(?:;?)$");

        protected Dictionary<string, string> Variables;

        protected Dictionary<string, AbstractShellBuiltin> Builtins;

        protected string Login;

        public UserSpace UserSpace { get; protected set; }

        public Dictionary<string, string> Environment { get; protected set; }

        public BashProcess(UserSpace userSpace) {
            UserSpace = userSpace;
            Environment = userSpace.Api.GetEnviron();
            Login = userSpace.Api.GetLogin();

            Variables = new Dictionary<string, string>();
            Builtins = new Dictionary<string, AbstractShellBuiltin>();

            SetupDefaultEnvironment();
            RegisterBuiltins();
        }

        public bool Run() {
            string cwdName = PathUtils.BaseName(UserSpace.Api.GetCwd());

            string cmd = UserSpace.Input(
                $"[{Login}@hacking01 {cwdName}]$",
                " "
            );
            Debug.Log("recv cmd: " + cmd);
            if (string.IsNullOrEmpty(cmd)) {
                return true;
            }

            if (cmd == "exit") {
                return false;
            }

            try {
                if (!ParseSetVariables(cmd)) {
                    string[] cmdLine = SendCmdToParseChain(cmd);

                    if (IsBuiltin(cmdLine[0])) {
                        RunBuiltin(cmdLine);
                    } else {
                        RunCommand(cmdLine);
                    }   
                }
            } catch (System.Exception exception) {
                UserSpace.Stderr.WriteLine($"-bash: {exception.Message}");
            }

            return true;
        }

        protected int RunBuiltin(string[] cmdLine) {
            AbstractShellBuiltin builtin = Builtins[cmdLine[0]];

            try {
                return builtin.Execute(cmdLine);
            } catch (ExitProcessException e) {
                return e.ExitCode;
            }
        }

        protected void RunCommand(string[] cmdLine) {
            int pid = UserSpace.Api.StartProcess(cmdLine);
            UserSpace.Api.WaitPid(pid);
        }

        protected void RegisterBuiltins() {
            Builtins["cd"] = new Cd(this);
            Builtins["env"] = new Env(this);
        }

        protected void SetupDefaultEnvironment() {
            Environment.Add("PATH", "/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin");
            Environment.Add("OLDPWD", Environment["PWD"]);

            Variables["$"] = UserSpace.Api.GetPid().ToString();
        }

        protected string SearchFile(string fileName) {
            string foundFile = null;

            foreach (string path in Environment["PATH"].Split(':')) {
                if (UserSpace.Api.FileExists(path)) {
                    try {
                        foundFile = FindFileByPath(path, fileName);
                    } catch (System.Exception e) {
                        Debug.Log("failed when searching file:" + path);
                    }

                    if (foundFile != null) {
                        return foundFile;
                    }
                }
            }

            return null;
        }

        protected string FindFileByPath(string path, string fileName) {
            List<ReadOnlyFile> files = UserSpace.Api.ListDirectory(path);

            return files.Find(roFile => roFile.Name == fileName).Path;
        }

        protected string[] SendCmdToParseChain(string cmd) {
            cmd = ReplaceShellVariables(cmd);
            string[] cmdLine = cmd.Split(' ');
            string[] cmdResolution = ParseCommandFile(cmdLine);
            return cmdResolution;
        }

        protected bool IsBuiltin(string cmd) {
            return Builtins.ContainsKey(cmd);
        }

        protected string[] ParseCommandFile(string[] cmd) {
            if (!PathUtils.IsAbsPath(cmd[0]) && !IsBuiltin(cmd[0])) {
                string filePath = SearchFile(cmd[0]);
                
                if (filePath == null) {
                    throw new System.InvalidOperationException(
                        "command not found"   
                    );
                }

                cmd[0] = filePath;
            }
            
            return cmd;
        }

        protected bool ParseSetVariables(string cmd) {
            MatchCollection matches = SetVariablesRegex.Matches(cmd);

            if (matches.Count == 0) {
                return false;
            }

            foreach (Match match in matches) {
                GroupCollection groups = match.Groups;
                Variables[groups[1].Value] = groups[2].Value;
            }

            return true;
        }

        protected string ReplaceShellVariables(string cmd) {
            foreach (KeyValuePair<string, string> kvp in Variables) {
                cmd = new Regex(
                    @"(\$" + Regex.Escape(kvp.Key) + ")"
                ).Replace(cmd, kvp.Value);
            }
            
            return cmd;
        }
    }
}