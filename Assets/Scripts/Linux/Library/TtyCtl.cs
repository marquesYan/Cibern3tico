using Linux.Sys.RunTime;
using Linux.FileSystem;
using Linux.PseudoTerminal;
using Linux;
using UnityEngine;

namespace Linux.Library
{    
    public class TtyCtl : CompiledBin {
        public TtyCtl(
            string absolutePath,
            int uid,
            int gid,
            int permission,
            FileType type
        ) : base(absolutePath, uid, gid, permission, type) { }

        public override int Execute(UserSpace userSpace) {
            // Kernel kernel = userSpace.AccessKernelSpace().Kernel;
            // PrimaryPty pty = kernel.PtyTable.ControllingPty;

            // if (pty == null) {
            //     return 1;
            // }

            // string key;

            // while (true) {
            //     key = pty.Read();
            //     pty.Write(key);
            // }

            return 0;
        }
    }
}