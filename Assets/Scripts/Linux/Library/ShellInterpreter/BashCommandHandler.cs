using Linux.IO;
using Linux.FileSystem;
using Linux.Sys.RunTime;

namespace Linux.Library.ShellInterpreter
{
    public class BashCommandHandler {
        protected UserSpace UserSpace;

        public BashCommandHandler(KernelSpace api) {
            UserSpace = new UserSpace(api);
        }

        public bool IsFileSupported(File executable) {
            using (ITextIO stream = UserSpace.Open(executable.Path, AccessMode.O_RDONLY)) {
                string[] lines = stream.ReadLines();

                if (lines.Length == 0) {
                    return false;
                }

                return lines[0].StartsWith("#!/usr/bin/bash");
            }
        }

        public int Execute(File executable) {
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