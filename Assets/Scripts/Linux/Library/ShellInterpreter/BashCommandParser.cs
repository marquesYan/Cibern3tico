using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Linux.Sys.RunTime;
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

    public class BashCommandParser {
        protected Regex SetVariablesRegex = new Regex(@"^([a-zA-Z_]+)=([a-zA-Z0-9_./]*)(?:;?)$");

        protected Regex ReplaceVariablesRegex = new Regex(@"\\?\$([a-zA-Z_]+|\$|\?|\!)");

        protected List<Token> Tokens;
        protected Stack<char> Stack;

        protected StringBuilder BuiltToken;

        protected UserSpace UserSpace;

        protected Dictionary<string, string> Environment;
        protected Dictionary<string, string> Variables;

        public BashCommandParser(
            UserSpace userSpace,
            Dictionary<string, string> environment,
            Dictionary<string, string> variables
        ) {
            UserSpace = userSpace;
            Tokens = new List<Token>();
            Stack = new Stack<char>();
            BuiltToken = new StringBuilder();
            Environment = environment;
            Variables = variables;
        }

        public List<CommandBuilder> ParseCommands(string cmdLine) {
            ParseToTokens(cmdLine);

            var commands = new List<CommandBuilder>();
            var currentCmd = new CommandBuilder();

            foreach (Token token in Tokens) {
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
                        string path = ReplaceShellVariables(token.Value);

                        // Add command line and clear to start new one
                        currentCmd.Stdout = UserSpace.ResolvePath(path);

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

        public string ReplaceShellVariables(string cmd) {
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

        public bool TryParseVariables(string cmd) {
            MatchCollection matches = SetVariablesRegex.Matches(cmd);

            foreach (Match match in matches) {
                GroupCollection groups = match.Groups;
                Variables.Add(groups[1].Value, groups[2].Value);
            }

            return matches.Count > 0;
        }

        protected void ParseToTokens(string cmdLine) {
            Tokens.Clear();
            Stack.Clear();
            BuiltToken.Clear();

            foreach(char token in cmdLine) {
                bool isStackEmpty = Stack.Count == 0;

                if (token == Token.OUT_REDIR) {
                    if (isStackEmpty || Stack.Peek() == Token.OUT_REDIR) {
                        Debug.Log("pushing redir token");
                        // Redirect token
                        Stack.Push(token);
                    } else if (Token.IsQuotes(token)) {
                        Debug.Log("pushing literal redir token to builtToken");
                        // Translate as literal '>'
                        BuiltToken.Append(token);
                    }
                } else if (Token.IsQuotes(token)) {
                    if (isStackEmpty) {
                        // Opening quotes
                        Stack.Push(token);
                    } else if (Stack.Peek() == token) {
                        // Closing quotes
                        Stack.Pop();

                        TokenType type;

                        if (token == Token.S_QUOTE) {
                            type = TokenType.SINGLE_QUOTED;
                        } else  {
                            type = TokenType.DOUBLE_QUOTED;
                        }

                        // Add built token as a whole token
                        Tokens.Add(
                            new Token(type, BuiltToken.ToString())
                        );

                        BuiltToken.Clear();
                    } else {
                        // Translate as literal quotes
                        BuiltToken.Append(token);
                    }
                } else {
                    switch(token) {
                        case Token.SEMICOLON:
                        case ' ': {
                            if (!isStackEmpty 
                                && Token.IsQuotes(Stack.Peek()))
                            {
                                BuiltToken.Append(token);
                                break;
                            }

                            if (BuiltToken.Length > 0) {
                                HandleRedirectionToken();
                            }

                            if (token == Token.SEMICOLON) {
                                // Now add colon separately
                                Tokens.Add(
                                    new Token(TokenType.END_OF_COMMAND, "")
                                );
                            }

                            break;
                        }

                        default: {
                            BuiltToken.Append(token);
                            break;
                        }
                    }
                }
            }

            if (Stack.Count > 0 && Token.IsQuotes(Stack.Peek())) {
                throw new System.ArgumentException(
                    "syntax error: ensure quotes are properly closed"
                );
            }

            if (BuiltToken.Length > 0) {
                HandleRedirectionToken();
            }
        }

        protected void HandleRedirectionToken() {
            TokenType type = TokenType.FREE_FORM;

            if (Stack.Count > 0 && Stack.Peek() == Token.OUT_REDIR) {
                Debug.Log("popping first redir token");

                type = TokenType.OUT_REDIR;
                Stack.Pop();

                if (Stack.Count > 0 && Stack.Peek() == Token.OUT_REDIR) {
                    Debug.Log("popping second redir token");
                    Stack.Pop();
                    type = TokenType.OUT_REDIR_APPEND;

                    // Sanity check
                    if (Stack.Count > 0 && Stack.Peek() == Token.OUT_REDIR) {
                        throw new System.ArgumentException(
                            "syntax error: unknow redirection symbol '>>>'"
                        );
                    }
                }
            }

            Debug.Log("adding token with type: " + type);
            // Add built token without colon
            Tokens.Add(
                new Token(type, BuiltToken.ToString())
            );

            BuiltToken.Clear();
        }
    }
}