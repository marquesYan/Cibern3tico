using UnityEngine;
using Linux.FileSystem;
using Linux.Utilities;
using Linux;

namespace Linux.Utilities.Sbin {
    public class InitUtility : AbstractUtility {
        public InitUtility(
            string absolutePath, 
            int uid,
            int gid,
            int permission
        ) : base(absolutePath, uid, gid, permission) { }

        public override int Execute(string[] arguments) {
            Debug.Log("Kernel just called init");
            return 0;
        }
    }
}