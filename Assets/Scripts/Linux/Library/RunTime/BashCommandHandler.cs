using Linux.IO;
using Linux.Library.ShellInterpreter;
using Linux.FileSystem;
using Linux.Sys.RunTime;

namespace Linux.Library.RunTime
{
    public class BashCommandHandler : AbstractRunTimeHandler {

        public BashCommandHandler(KernelSpace api) : base(api) {}

        public override bool IsFileSupported(File executable) {
            using (ITextIO stream = UserSpace.Open(executable.Path, AccessMode.O_RDONLY)) {
                string[] lines = stream.ReadLines();

                if (lines.Length == 0) {
                    return false;
                }

                return lines[0].StartsWith("#!/usr/bin/bash");
            }
        }

        public override int Execute(File executable) {
            var bash = new BashProcess(UserSpace, false);

            using (ITextIO stream = UserSpace.Open(executable.Path, AccessMode.O_RDONLY)) {
                string[] lines = stream.ReadLines();

                foreach (string line in lines) {
                    string cmd = line.TrimStart();

                    if (string.IsNullOrEmpty(cmd) || cmd.StartsWith("#")) {
                        continue;
                    }

                    try {
                        if (!bash.ParseSetVariables(cmd)) {
                            bash.ParseAndStartCommands(cmd);
                        }
                    } catch (System.Exception e) {
                        UserSpace.Stderr.WriteLine($"-bash: {e.Message}");
                    }
                }
            }

            return 0;
        }
    }
}