using Linux.IO;
using Linux.Sys.IO;

namespace Linux.PseudoTerminal
{
    public enum IoctlSignals {
        T_RAW
    }

    public class SecondaryPty : CharacterDevice {
        public SecondaryPty() : base(AccessMode.O_RDWR) { }

        public void Ioctl(int signal, params string[] args) {
            //
        }
    }

    public class PrimaryPty : CharacterDevice
    {
        public PrimaryPty() : base(AccessMode.O_RDWR) { }

        public void Ioctl(int signal, params string[] args) {
            //
        }
    }
}