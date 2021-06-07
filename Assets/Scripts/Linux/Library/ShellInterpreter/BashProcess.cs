using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Linux.Configuration;
using Linux.Sys.RunTime;
using Linux.FileSystem;
using Linux.Library.ShellInterpreter.Builtins;
using Linux.IO;
using Linux;
using UnityEngine;


namespace Linux.Library.ShellInterpreter
{
    public enum TokenType {
        SINGLE_QUOTED,
        DOUBLE_QUOTED,
        FREE_FORM,
        END_OF_COMMAND,
        OUT_REDIR,
        OUT_REDIR_APPEND,
        IN_REDIR,
    }

    public class Token {
        public const char SEMICOLON = ';';
        public const char S_QUOTE = '\'';
        public const char D_QUOTE = '"';
        public const char OUT_REDIR = '>';
        public const char IN_REDIR = '<';

        public static bool IsQuotes(char input) {
            return input == S_QUOTE || input == D_QUOTE;
        }

        public readonly TokenType Type;

        public readonly string Value;

        public Token(TokenType type, string value) {
            Type = type;
            Value = value;
        }
    }

    public class CommandBuilder {
        public List<string> CmdLine;

        public bool AppendStdout = false;

        public string Stdout;
        public string Stdin;
        public string Stderr;

        public CommandBuilder() {
            CmdLine = new List<string>();
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
                // UserSpace.Stderr.WriteLine(exception.ToString());
            }

            return true;
        }

        protected void SetReturnCode(int retCode) {
            Environment["?"] = retCode.ToString();
        }

        protected void ParseAndStartCommands(string cmd) {
            List<CommandBuilder> commands = SendCmdToParseChain(cmd);

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

        protected List<CommandBuilder> SendCmdToParseChain(string cmd) {
            List<Token> tokens = ParseToTokens(cmd);

            var commands = new List<CommandBuilder>();
            var currentCmd = new CommandBuilder();

            foreach (Token token in tokens) {
                switch(token.Type) {
                    case TokenType.SINGLE_QUOTED: {
                        // Add command line token directly
                        currentCmd.CmdLine.Add(token.Value);
                        break;
                    }

                    case TokenType.END_OF_COMMAND: {
                        // Add command line and clear to start new one
                        commands.Add(currentCmd);
                        currentCmd = new CommandBuilder();
                        break;
                    }

                    case TokenType.OUT_REDIR_APPEND:
                    case TokenType.OUT_REDIR: {
                        // Add command line and clear to start new one
                        currentCmd.Stdout = UserSpace.ResolvePath(token.Value);

                        currentCmd.AppendStdout = token.Type == TokenType.OUT_REDIR_APPEND;
                        commands.Add(currentCmd);

                        currentCmd = new CommandBuilder();
                        break;
                    }

                    case TokenType.FREE_FORM:
                    case TokenType.DOUBLE_QUOTED: {
                        // Replace variables 
                        string replaced = ReplaceShellVariables(token.Value);
                        currentCmd.CmdLine.Add(replaced);
                        break;
                    }
                }
            }

            if (currentCmd.CmdLine != null) {
                // Add any remaining command
                commands.Add(currentCmd);
            }

            return commands;
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
                bool isStackEmpty = stack.Count == 0;

                if (token == Token.OUT_REDIR) {
                    if (isStackEmpty) {
                        // Redirect token
                        stack.Push(token);
                    } else if (stack.Peek() == Token.OUT_REDIR) {
                        stack.Pop();

                        // Sanity check
                        if (stack.Peek() == Token.OUT_REDIR) {
                            throw new System.ArgumentException(
                                "syntax error: unknow redirection symbol '>>>'"
                            );
                        }

                        // Put back on stack
                        stack.Push(token);
                    } else if (Token.IsQuotes(token)) {
                        // Translate as literal '>'
                        builtToken.Append(token);
                    }
                } else if (Token.IsQuotes(token)) {
                    if (isStackEmpty) {
                        // Opening quotes
                        stack.Push(token);
                    } else if (stack.Peek() == token) {
                        // Closing quotes
                        stack.Pop();

                        TokenType type;

                        if (token == Token.S_QUOTE) {
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
                        case Token.SEMICOLON:
                        case ' ': {
                            if (builtToken.Length > 0) {
                                TokenType type = TokenType.FREE_FORM;

                                if (!isStackEmpty && stack.Peek() == Token.OUT_REDIR) {
                                    type = TokenType.OUT_REDIR;
                                    stack.Pop();

                                    if (stack.Count > 0 && stack.Peek() == Token.OUT_REDIR) {
                                        stack.Pop();
                                        type = TokenType.OUT_REDIR_APPEND;
                                    }
                                }

                                // Add built token without colon
                                tokens.Add(
                                    new Token(type, builtToken.ToString())
                                );
                                builtToken.Clear();
                            }

                            if (token == Token.SEMICOLON) {
                                // Now add colon separately
                                tokens.Add(
                                    new Token(TokenType.END_OF_COMMAND, "")
                                );
                            }

                            break;
                        }

                        default: {
                            builtToken.Append(token);
                            break;
                        }
                    }
                }
            }

            if (stack.Count > 0 && Token.IsQuotes(stack.Peek())) {
                throw new System.ArgumentException(
                    "syntax error: ensure quotes are properly closed"
                );
            }

            if (builtToken.Length > 0) {
                TokenType type = TokenType.FREE_FORM;

                if (stack.Count > 0 && stack.Peek() == Token.OUT_REDIR) {
                    type = TokenType.OUT_REDIR;
                }

                // Add remaning token data
                tokens.Add(
                    new Token(type, builtToken.ToString())
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