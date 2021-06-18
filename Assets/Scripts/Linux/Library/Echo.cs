using System.Collections.Generic;
using Linux.Sys.RunTime;
using Linux.FileSystem;
using UnityEngine;

namespace Linux.Library
{    
    public class Echo : CompiledBin {
        public Echo(
            string absolutePath,
            int uid,
            int gid,
            int permission,
            FileType type
        ) : base(absolutePath, uid, gid, permission, type) { }

        public override int Execute(UserSpace userSpace) {
            List<string> args = new List<string>(userSpace.Api.GetArgs());

            // Exclude executable argument
            args.RemoveAt(0);

            userSpace.Print(string.Join(" ", args));
            return 0;
        }
    }
}