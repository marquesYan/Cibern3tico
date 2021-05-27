using Linux.FileSystem;
using Linux.PseudoTerminal;
using Linux;

namespace Linux.Devices {
    public class TtyDevice : AbstractDevice {
        VirtualTerminal Terminal { get; set; }

        public TtyDevice(
            VirtualTerminal vt, 
            string absolutePath, 
            int uid,
            int gid,
            int permission
        ) : base(absolutePath, uid, gid, permission) {
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
            return Terminal.ReadLine();
        }
    }
}