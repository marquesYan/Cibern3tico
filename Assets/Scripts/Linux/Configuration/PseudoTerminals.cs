using Linux.FileSystem;
using Linux.IO;
using Linux.Sys.IO;
using Linux.Sys.Input;

namespace Linux.Configuration
{
    public class PseudoTerminalTable {
        readonly object _ptyLock = new object();

        protected VirtualFileTree Fs;

        protected ITtyDriver TtyDriver;

        protected int Count;

        public PseudoTerminalTable(
            VirtualFileTree fs,
            ITtyDriver ttyDriver
        ) {
            Fs = fs;
            TtyDriver = ttyDriver;

            Count = 0;

            Fs.CreateDir(
                "/dev/pts",
                0, 0, 
                Perm.FromInt(7, 5, 5)
            );
        }

        public int Add(User user) {
            lock(_ptyLock) {
                int ptsFd = TtyDriver.UnlockPt(
                    $"/dev/pts/{Count}",
                    user.Uid, 0,
                    Perm.FromInt(6, 2, 0)
                );

                Count++;

                return ptsFd;
            }
        }

        public void Remove(string ptsFile) {
            TtyDriver.RemovePt(ptsFile);
        }
    }
}