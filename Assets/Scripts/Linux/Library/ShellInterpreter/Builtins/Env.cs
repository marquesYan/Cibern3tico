using System.Collections.Generic;
using System.Text;
using Linux.Sys.RunTime;
using Linux.IO;

namespace Linux.Library.ShellInterpreter.Builtins
{    
    public class Env : AbstractShellBuiltin {
        public Env(BashProcess bash) : base(bash) { }

        public override int Execute(string[] args) {
            var parser = new ArgumentParser.GenericArgParser(
                UserSpace,
                "Usage: {0} [NAME=VALUE]... [COMMAND [ARG]...]",
                "Set each NAME to VALUE in the environment and run COMMAND.",
                "env"
            );

            List<string> arguments = parser.Parse(args);

            string dir;

            if (arguments.Count == 0) {
                StringBuilder buffer = new StringBuilder();

                foreach (KeyValuePair<string, string> kvp in Bash.Environment) {
                    buffer.AppendFormat("{0}={1}", kvp.Key, kvp.Value);
                    buffer.AppendLine();
                }

                UserSpace.Print(buffer.ToString(), "");
                return 0;
            }

            return 0;
        }
    }
}