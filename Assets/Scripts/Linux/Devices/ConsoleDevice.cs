using System.IO;
using Linux.FileSystem;
using Linux;

namespace Linux.Devices {
    public class ConsoleDevice : AbstractDevice {
        string BootFile { get; set; }

        public ConsoleDevice(
            string bootFile, 
            string absolutePath, 
            int uid,
            int gid, 
            int permission
        ) : base(absolutePath, uid, gid, permission) {
            BootFile = bootFile;
        }

        public override string Read() {
            return File.ReadAllText(BootFile);
        }
    }
}