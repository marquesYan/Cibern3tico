using Linux.FileSystem;
using Linux.IO;
using Linux.Sys.IO;
using Linux.Sys.Input;

namespace Linux.Configuration
{
    public class PseudoTerminalTable {
        protected VirtualFileTree Fs;
        protected ITtyDriver TtyDriver;

        public int Count { get; protected set;}

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

            Fs.Create(
                "/dev/pts/ptmx",
                0, 0,
                Perm.FromInt(6, 6, 6)
            );
        }

        public int Add(User user) {
            int ptsFd = TtyDriver.UnlockPt(
                $"/dev/pts/{Count}",
                user.Uid, 0,
                Perm.FromInt(6, 2, 0)
            );

            Count++;

            return ptsFd;
        }
    }
}