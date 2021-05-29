using Linux.FileSystem;
using Linux.PseudoTerminal;
using Linux;

namespace Linux.Devices {
    public class TtyDevice {
        VirtualTerminal Terminal { get; set; }

        public int Write(string[] data) {
            int written = 0;

            foreach (string stream in data) {
                written += Terminal.Write(stream);
            }
            
            return written;
        }

        public string Read() {
            return Terminal.ReadLine();
        }
    }
}