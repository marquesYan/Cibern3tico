using System.Collections.Generic;
using Linux.Configuration;
using Linux.Sys.RunTime;
using Linux.FileSystem;
using Linux.PseudoTerminal;
using Linux.Library.ShellInterpreter.Builtins;
using Linux.IO;
using Linux.Sys.IO;
using Linux.Sys.Input.Drivers.Tty;
using UnityEngine;


namespace Linux.Library.ShellInterpreter
{
    public class BashProcess {

        protected Dictionary<string, AbstractShellBuiltin> Builtins;

        protected BashCommandParser CommandParser;

        protected string Login;

        protected bool ShowPrompt;

        protected BashHistory History;

        public UserSpace UserSpace { get; protected set; }

        public Dictionary<string, string> Environment { get; protected set; }

        public Dictionary<string, string> Variables { get; protected set; }

        public BashProcess(UserSpace userSpace, bool autoCookPty) {
            UserSpace = userSpace;
            Environment = userSpace.Api.GetEnviron();
            Login = userSpace.Api.GetLogin();
            History = new BashHistory(userSpace);

            ShowPrompt = true;

            Variables = new Dictionary<string, string>();
            Builtins = new Dictionary<string, AbstractShellBuiltin>();
            CommandParser = new BashCommandParser(
                userSpace,
                Environment,
                Variables
            );

            SetupDefaultEnvironment();
            RegisterBuiltins();

            if (autoCookPty) {
                CookPty();
            }
        }

        public BashProcess(UserSpace userSpace) : this(userSpace, true) {}

        public bool Run() {
            string cwdName = PathUtils.BaseName(UserSpace.Api.GetCwd());

            string prompt = $"[{Login}@hacking01 {cwdName}]$";

            if (ShowPrompt) {
                UserSpace.Print(prompt, " ");
            }

            string originalCmd = UserSpace.Stdin.ReadUntil(
                $"{AbstractTextIO.LINE_FEED}",
                CharacterControl.C_DUP_ARROW,
                CharacterControl.C_DDOWN_ARROW
            );

            if (string.IsNullOrEmpty(originalCmd)) {
                ShowPrompt = true;
                return true;
            }

            string cmd = originalCmd.Trim();

            if (cmd == "exit") {
                return false;
            }

            if (cmd == CharacterControl.C_DUP_ARROW
                || cmd == CharacterControl.C_DDOWN_ARROW) {
                string lastCommand;
                
                if (cmd == CharacterControl.C_DUP_ARROW) {
                    lastCommand = History.Last();
                } else {
                    lastCommand = History.Next();
                }

                if (lastCommand == null) {
                    lastCommand = "";
                }

                UserSpace.Print("\r", "");
                UserSpace.Print(prompt, " ");

                var bufClearArray = new string[] {
                    CharacterControl.C_CLEAR_BUFFER
                };
                ((IoctlDevice)UserSpace.Stdin).Ioctl(
                    PtyIoctl.TIO_SEND_KEY,
                    ref bufClearArray
                );
                
                var cmdArray = new string[] { lastCommand };
                ((IoctlDevice)UserSpace.Stdin).Ioctl(
                    PtyIoctl.TIO_SEND_KEY,
                    ref cmdArray
                );

                ShowPrompt = false;

                return true;
            }

            try {
                if (!ParseSetVariables(cmd)) {
                    ParseAndStartCommands(cmd);  
                }

                ShowPrompt = true;

                History.Add(cmd);
            } catch (System.Exception exception) {
                UserSpace.Stderr.WriteLine($"-bash: {exception.Message}");
                // UserSpace.Stderr.WriteLine(exception.ToString());
            }

            return true;
        }

        protected void SetReturnCode(int retCode) {
            Variables["?"] = retCode.ToString();
        }

        public void ParseAndStartCommands(string cmd) {
            List<CommandBuilder> commands = CommandParser.ParseCommands(cmd);

            foreach (CommandBuilder command in commands) {
                if (command.CmdLine != null) {
                    string[] cmdLine = command.CmdLine.ToArray();

                    if (cmdLine.Length > 0) {
                        int retCode;
                        if (IsBuiltin(cmdLine[0])) {
                            retCode = RunBuiltin(command);
                        } else {
                            retCode = RunCommand(command);
                        }

                        SetReturnCode(retCode);
                    }
                }
            }
        }

        public bool ParseSetVariables(string cmd) {
            return CommandParser.TryParseVariables(cmd);
        }

        protected void CookPty() {
            if (UserSpace.Stdin is IoctlDevice) {
                var pts = (IoctlDevice)UserSpace.Stdin;

                // Disable auto control of DownArrow character
                var downArrowArray = new string[] {
                    CharacterControl.C_DDOWN_ARROW
                };
                pts.Ioctl(
                    PtyIoctl.TIO_DEL_SPECIAL_CHARS,
                    ref downArrowArray
                );

                // Disable auto control of UpArrow character
                var upArrowArray = new string[] {
                    CharacterControl.C_DUP_ARROW
                };
                pts.Ioctl(
                    PtyIoctl.TIO_DEL_SPECIAL_CHARS,
                    ref upArrowArray
                );

                // Enable unbuffered operations on UpArrow character
                pts.Ioctl(
                    PtyIoctl.TIO_ADD_UNBUFFERED_CHARS,
                    ref upArrowArray
                );

                pts.Ioctl(
                    PtyIoctl.TIO_ADD_UNBUFFERED_CHARS,
                    ref downArrowArray
                );

                // Disable buffering, so we can receive the UpArrow
                // just when pressed by user
                var flagArray = new int[] { PtyFlags.BUFFERED };
                pts.Ioctl(
                    PtyIoctl.TIO_UNSET_FLAG,
                    ref flagArray
                );
            }
        }

        protected int RunBuiltin(CommandBuilder command) {
            AbstractShellBuiltin builtin = Builtins[command.CmdLine[0]];

            try {
                return builtin.Execute(command.CmdLine.ToArray());
            } catch (ExitProcessException e) {
                return e.ExitCode;
            }
        }

        protected int RunCommand(CommandBuilder command) {
            string[] cmdLine = ParseCommandFile(command.CmdLine.ToArray());

            int stdin = 0;
            int stdout = 1;
            int stderr = 2;

            if (command.Stdout != null) {
                stdout = UserSpace.Api.Open(
                    command.Stdout,
                    command.AppendStdout ? AccessMode.O_APONLY : AccessMode.O_WRONLY
                );
            }

            int pid = UserSpace.Api.StartProcess(
                cmdLine,
                stdin,
                stdout,
                stderr
            );

            return UserSpace.Api.WaitPid(pid);
        }

        protected void RegisterBuiltins() {
            Builtins["cd"] = new Cd(this);
            Builtins["env"] = new Env(this);
            Builtins["export"] = new Export(this);
            Builtins["read"] = new Read(this);
        }

        protected void SetupDefaultEnvironment() {
            if (!Environment.ContainsKey("PATH")) {
                Environment.Add(
                    "PATH",
                    "/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin"
                );
            }

            Environment["OLDPWD"] = Environment["PWD"];

            Variables["$"] = UserSpace.Api.GetPid().ToString();

            SetReturnCode(0);
        }

        protected string SearchFile(string fileName) {
            string foundFile = null;

            foreach (string path in Environment["PATH"].Split(':')) {
                if (UserSpace.Api.FileExists(path)) {
                    try {
                        foundFile = FindFileByPath(path, fileName);
                    } catch (System.Exception e) {
                        Debug.Log("failed when searching file:" + path);
                        Debug.Log(e.ToString());
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

        protected bool IsBuiltin(string cmd) {
            return Builtins.ContainsKey(cmd);
        }

        // Parsers

        protected string[] ParseCommandFile(string[] cmd) {
            if (!PathUtils.IsAbsPath(cmd[0]) && !IsBuiltin(cmd[0])) {
                string filePath = SearchFile(cmd[0]);
                
                if (filePath == null) {
                    throw new System.InvalidOperationException(
                        $"command not found: {cmd[0]}"   
                    );
                }

                cmd[0] = filePath;
            }
            
            return cmd;
        }

        protected void CompileWhile(string cmd) {
            // var regex = new Regex(@"while (\$[a-zA-Z_]+|\w+) (==|!=) (\$[a-zA-Z_]+|\w+);\s*do\s(.*\s?;)\s*done");
        }
    }
}