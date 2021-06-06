using System.Collections.Generic;
using Linux.Sys.RunTime;
using Linux.IO;

namespace Linux.Library.ShellInterpreter.Builtins
{    
    public class Cd : AbstractShellBuiltin {
        public Cd(BashProcess bash) : base(bash) { }

        public override int Execute(string[] args) {
            var parser = new ArgumentParser.GenericArgParser(
                UserSpace,
                "Usage: {0} [OPTION]... [DIR]",
                "Change the shell working directory",
                "cd"
            );

            List<string> arguments = parser.Parse(args);

            string dir;

            if (arguments.Count == 0) {
                if (Bash.Environment.ContainsKey("HOME")) {
                    dir = Bash.Environment["HOME"];
                } else {
                    UserSpace.Print("cd: HOME not set");
                    return 1;
                }
            } else {
                dir = arguments[0];
            }

            string oldWd = Bash.Environment["PWD"];

            UserSpace.Api.ChangeDirectory(dir);

            Bash.Environment["OLDPWD"] = oldWd;
            Bash.Environment["PWD"] = dir;

            return 0;
        }
    }
}