using System.Collections.Generic;
using System.Text;
using Linux.Sys.RunTime;
using Linux.IO;

namespace Linux.Library.ShellInterpreter.Builtins
{    
    public class Export : AbstractShellBuiltin {
        public Export(BashProcess bash) : base(bash) { }

        public override int Execute(string[] args) {
            var parser = new ArgumentParser.GenericArgParser(
                UserSpace,
                "Usage: {0} [NAME=VALUE]...",
                "Set each NAME to VALUE in shell environment.",
                "export"
            );

            List<string> arguments = parser.Parse(args);

            foreach(string value in arguments) {
                string[] tokens = value.Split('=');

                if (tokens.Length < 2) {
                    throw new System.ArgumentException(
                        $"invalid syntax: {value}"
                    );
                }

                Bash.Environment[tokens[0]] = tokens[1];
            }

            return 0;
        }
    }
}