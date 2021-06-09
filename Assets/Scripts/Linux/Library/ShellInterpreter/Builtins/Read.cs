using System.Collections.Generic;
using Linux.Library.ShellInterpreter;
using Linux.FileSystem;

namespace Linux.Library.ShellInterpreter.Builtins {
    public class Read : AbstractShellBuiltin {
        public Read(BashProcess bash) : base(bash) { }

        public override int Execute(string[] args) {
            var parser = new ArgumentParser.GenericArgParser(
                UserSpace,
                "Usage: {0} [OPTION]... [FIELD]...", 
                "Read a line from the standard input and split it into FIELDs.",
                "read"
            );

            string prompt = null;

            parser.AddArgument<string>(
                "p|prompt=",
                "output the string {PROMPT} without a trailing newline before attempting to read",
                (string p) => prompt = p
            );

            List<string> arguments = parser.Parse(args);

            if (arguments.Count == 0) {
                parser.ShowHelpInfo();
                return 127;
            }

            if (prompt != null) {
                UserSpace.Print(prompt, "");
            }

            string variable = arguments[0];
            string data = UserSpace.Stdin.ReadLine();

            Bash.Variables[variable] = data;

            return 0;
        }
    }
}