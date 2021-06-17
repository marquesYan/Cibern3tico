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

            BashProcess bashProc = new BashProcess(userSpace);

            bool keepRunning = true;

            while (eventSet && keepRunning) {
                keepRunning = bashProc.Run();
            }

            return 0;
        }
    }
}