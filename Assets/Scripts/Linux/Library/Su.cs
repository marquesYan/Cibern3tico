using System.Collections.Generic;
using Linux.Sys.RunTime;
using Linux.IO;
using Linux.FileSystem;

namespace Linux.Library
{    
    public class Su : CompiledBin {
        public Su(
            string absolutePath,
            int uid,
            int gid,
            int permission,
            FileType type
        ) : base(absolutePath, uid, gid, permission, type) { }

        public override int Execute(UserSpace userSpace) {
            var parser = new ArgumentParser.GenericArgParser(
                userSpace,
                "Usage: {0} [USER]",
                "Change the effective user ID and group ID to that of USER"
            );

            List<string> arguments = parser.Parse();

            string user;

            if (arguments.Count < 1) {
                user = "root";
            } else {
                user = arguments[0];
            }

            try {
                int pid = userSpace.Api.RunAs(user);
                userSpace.Api.WaitPid(pid);
            } catch (System.Exception e) {
                userSpace.Stderr.WriteLine($"su: {e.Message}");
                return 1;
            }

            return 0;
        }
    }
}