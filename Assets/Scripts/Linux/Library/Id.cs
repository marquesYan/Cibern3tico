using System.Collections.Generic;
using Linux.Sys.RunTime;
using Linux.IO;
using Linux.FileSystem;

namespace Linux.Library
{    
    public class Id : CompiledBin {
        public Id(
            string absolutePath,
            int uid,
            int gid,
            int permission,
            FileType type
        ) : base(absolutePath, uid, gid, permission, type) { }

        public override int Execute(UserSpace userSpace) {
            var parser = new ArgumentParser.GenericArgParser(
                userSpace,
                "Usage: {0}",
                "Show user information"
            );

            List<string> arguments = parser.Parse();

            userSpace.Print($"{userSpace.Api.GetUid()}");

            return 0;
        }
    }
}