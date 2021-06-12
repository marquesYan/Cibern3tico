using System.Collections.Generic;
using Linux.Sys.RunTime;
using Linux.FileSystem;
using Linux.Library.ArgumentParser;
using Linux;

namespace Linux.Library
{    
    public class Poweroff : CompiledBin {
        public Poweroff(
            string absolutePath,
            int uid,
            int gid,
            int permission,
            FileType type
        ) : base(absolutePath, uid, gid, permission, type) { }

        public override int Execute(UserSpace userSpace) {
            var parser = new GenericArgParser(
                userSpace,
                "Usage: {0}",
                "Power off the system"
            );

            List<string> arguments = parser.Parse();

            Kernel kernel = userSpace.Api.AccessKernel();
            kernel.ScheduleShutdown();

            return 0;
        }
    }
}