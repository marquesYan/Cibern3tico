using Linux.FileSystem;
using Linux.PseudoTerminal;
using Linux;

namespace Linux.Devices {
    public class TtyDevice : AbstractDevice {
        VirtualTerminal Terminal;

        public TtyDevice(VirtualTerminal vt, string path, Perms[] permissions) : base(path, permissions) {
            Terminal = vt;
        }

        public override int Write(string[] data) {
            int written = 0;

            foreach (string stream in data) {
                written += Terminal.Write(stream);
            }
            
            return written;
        }

        public override string Read() {
            return Terminal.Read();
        }
        
    }
}