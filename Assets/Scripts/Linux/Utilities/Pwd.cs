using System.Collections.Generic;
using Linux.FileSystem;

namespace Linux.Utilities {
    public class PwdUtility : AbstractUtility {
        public PwdUtility(
            string absolutePath,
            int uid,
            int gid, 
            int permission
        ) : base(absolutePath, uid, gid, permission) { }

        public override int Execute(string[] args) {
            // Subsystem.Singleton.AddStream(Subsystem.Singleton.Sh.Cwd.Path);
            return 0;
        }
    }
}