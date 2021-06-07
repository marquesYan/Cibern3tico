using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Linux.Configuration;
using Linux.Sys.RunTime;
using Linux.FileSystem;
using Linux.Library.ShellInterpreter.Builtins;
using Linux;
using UnityEngine;


namespace Linux.Library.ShellInterpreter
{
    public enum TokenType {
        SINGLE_QUOTED,
        DOUBLE_QUOTED,
        FREE_FORM,
        END_OF_COMMAND,
    }

    public class Token {
        public readonly TokenType Type;

        public readonly string Value;

        public Token(TokenType type, string value) {
            Type = type;
            Value = value;
        }
    }

    public class BashProcess {
        protected Regex SetVariablesRegex = new Regex(@"^([a-zA-Z_]+)=([a-zA-Z0-9_]*)(?:;?)$");

        protected Regex ReplaceVariablesRegex = new Regex(@"\\?\$([a-zA-Z_]+|\$|\?|\!)");

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
                    ParseAndStartCommands(cmd);  
                }
            } catch (System.Exception exception) {
                UserSpace.Stderr.WriteLine($"-bash: {exception.Message}");
            }

            return true;
        }

        protected void ParseAndStartCommands(string cmd) {
            List<string[]> commands = SendCmdToParseChain(cmd);

            foreach (string[] cmdLine in commands) {
                if (cmdLine.Length > 0) {
                    if (IsBuiltin(cmdLine[0])) {
                        RunBuiltin(cmdLine);
                    } else {
                        RunCommand(cmdLine);
                    }
                }
            } 
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
            cmdLine = ParseCommandFile(cmdLine);

            int pid = UserSpace.Api.StartProcess(cmdLine);
            UserSpace.Api.WaitPid(pid);
        }

        protected void RegisterBuiltins() {
            Builtins["cd"] = new Cd(this);
            Builtins["env"] = new Env(this);
            Builtins["export"] = new Export(this);
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

        protected List<string[]> SendCmdToParseChain(string cmd) {
            List<Token> tokens = ParseToTokens(cmd);

            var finalTokens = new List<string[]>();
            var currentCmdLine = new List<string>();

            foreach (Token token in tokens) {
                switch(token.Type) {
                    case TokenType.SINGLE_QUOTED: {
                        // Add command line token directly
                        currentCmdLine.Add(token.Value);
                        break;
                    }

                    case TokenType.END_OF_COMMAND: {
                        // Add command line and clear to start new one
                        finalTokens.Add(currentCmdLine.ToArray());
                        currentCmdLine.Clear();
                        break;
                    }

                    case TokenType.FREE_FORM:
                    case TokenType.DOUBLE_QUOTED: {
                        // Replace variables 
                        string replaced = ReplaceShellVariables(token.Value);
                        currentCmdLine.Add(replaced);
                        break;
                    }
                }
            }

            if (currentCmdLine.Count > 0) {
                // Add any remaining command
                finalTokens.Add(currentCmdLine.ToArray());
            }

            return finalTokens;
        }

        protected bool IsBuiltin(string cmd) {
            return Builtins.ContainsKey(cmd);
        }

        // Parsers

        protected List<Token> ParseToTokens(string cmd) {
            var tokens = new List<Token>();
            var stack = new Stack<char>();

            var builtToken = new StringBuilder();

            foreach(char token in cmd) {
                if (token == '\'' || token == '"') {
                    if (stack.Count == 0) {
                        // Opening quotes
                        stack.Push(token);
                    } else if (stack.Peek() == token) {
                        // Closing quotes
                        stack.Pop();

                        TokenType type;

                        if (token == '\'') {
                            type = TokenType.SINGLE_QUOTED;
                        } else  {
                            type = TokenType.DOUBLE_QUOTED;
                        }

                        // Add built token as a whole token
                        tokens.Add(
                            new Token(type, builtToken.ToString())
                        );
                        builtToken.Clear();
                    } else {
                        // Translate as literal quotes
                        builtToken.Append(token);
                    }
                } else {
                    switch(token) {
                        case ' ': {
                            if (builtToken.Length > 0) {
                                // Add built token without colon
                                tokens.Add(
                                    new Token(TokenType.FREE_FORM, builtToken.ToString())
                                );
                                builtToken.Clear();
                            }
                            break;
                        }

                        case ';': {
                            if (builtToken.Length > 0) {
                                // Add built token without colon
                                tokens.Add(
                                    new Token(TokenType.FREE_FORM, builtToken.ToString())
                                );
                                builtToken.Clear();
                            }

                            // Now add colon separately
                            tokens.Add(
                                new Token(TokenType.END_OF_COMMAND, ";")
                            );
                            break;
                        }

                        default: {
                            builtToken.Append(token);
                            break;
                        }
                    }
                }
            }

            if (stack.Count > 0) {
                throw new System.ArgumentException(
                    "syntax error: ensure quotes are properly closed"
                );
            }

            if (builtToken.Length > 0) {
                // Add remaning token data
                tokens.Add(
                    new Token(TokenType.FREE_FORM, builtToken.ToString())
                );
            }

            return tokens;
        }

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
            MatchCollection matches = ReplaceVariablesRegex.Matches(cmd);

            foreach (Match match in matches) {
                GroupCollection groups = match.Groups;

                // Skip escapped
                if (groups[0].Value.StartsWith("\\")) {
                    continue;
                }

                string key = groups[1].Value;
                string value;

                if (Variables.ContainsKey(key)) {
                    value = Variables[key];
                } else if (Environment.ContainsKey(key)) {
                    value = Environment[key];
                } else {
                    value = "";
                }

                cmd = cmd.Replace($"${key}", value);
            }
            
            return cmd;
        }

        protected void CompileWhile(string cmd) {
            var regex = new Regex(@"while (\$[a-zA-Z_]+|\w+) (==|!=) (\$[a-zA-Z_]+|\w+);\s*do\s(.*\s?;)\s*done");
        }
    }
}