using System.Collections.Generic;
using Linux.FileSystem;
using Linux.IO;
using Linux.PseudoTerminal;

namespace Linux.Configuration
{
    public class PseudoTerminalTable {
        protected List<string> Ptys;

        protected VirtualFileTree Fs;

        public PrimaryPty ControllingPty;

        public PseudoTerminalTable(VirtualFileTree fs) {
            Fs = fs;
            Ptys = new List<string>();

            Fs.CreateDir(
                "/dev/pts",
                0, 0, 
                Perm.FromInt(7, 5, 5)
            );
        }

        public void SetControllingPty(PrimaryPty pty) {
            ControllingPty = pty;

            Fs.Create(
                "/dev/pts/pmtx",
                0, 0,
                Perm.FromInt(0, 0, 0),
                FileType.F_CHR,
                ControllingPty
            );
        }

        public File Add(User user) {
            if (ControllingPty == null) {
                return null;
            }

            SecondaryPty pts = ControllingPty.CreateSecondary();

            var ptsFile = Fs.Create(
                $"/dev/pts/{Ptys.Count}",
                user.Uid, 0,
                Perm.FromInt(6, 2, 0),
                FileType.F_CHR,
                pts
            );

            Ptys.Add(ptsFile.Path);

            return ptsFile;
        }
    }
}