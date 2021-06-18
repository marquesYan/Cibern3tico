using System.Collections.Generic;
using Linux.Configuration;
using Linux.FileSystem;
using Linux.Library.ShellInterpreter;
using Linux.Sys.RunTime;

namespace Linux.Library
{    
    public class Bash : CompiledBin {
        public Bash(
            string absolutePath,
            int uid,
            int gid,
            int permission,
            FileType type
        ) : base(absolutePath, uid, gid, permission, type) { }

        public override int Execute(UserSpace userSpace) {
            bool eventSet = true;

            userSpace.Api.Trap(ProcessSignal.SIGTERM, (int[] args) => {
                eventSet = false;
            });

            var parser = new ArgumentParser.GenericArgParser(
                userSpace,
                "Usage: {0} [COMMAND]",
                "Bourne-Again SHell"
            );

            List<string> arguments = parser.Parse();

            BashProcess bashProc = new BashProcess(userSpace);

            if (arguments.Count > 0) {
                bashProc.ParseAndStartCommands(string.Join(" ", arguments));
                string exitCodeStr = bashProc.Variables["?"];
                int exitCode;

                if (int.TryParse(exitCodeStr, out exitCode)) {
                    return exitCode;
                }

                return 1;
            }

            bool keepRunning = true;

            while (eventSet && keepRunning) {
                keepRunning = bashProc.Run();
            }

            return 0;
        }
    }
}