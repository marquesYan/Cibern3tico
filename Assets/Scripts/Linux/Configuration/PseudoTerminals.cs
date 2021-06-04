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
            CharacterDevice pts = TtyDriver.GetPt();

            File ptsFile = Fs.Create(
                $"/dev/pts/{Count}",
                user.Uid, 0,
                Perm.FromInt(6, 2, 0),
                FileType.F_CHR,
                pts
            );

            Count++;

            int ptsFd = TtyDriver.UnlockPt(ptsFile.Path);

            return ptsFd;
        }
    }
}