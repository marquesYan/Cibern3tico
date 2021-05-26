using System.IO;
using Linux.FileSystem;
using Linux;

namespace Linux.Devices {
    public class ConsoleDevice : AbstractDevice {
        public string BootFile;

        public ConsoleDevice(string bootFile, string path, Perms[] permissions) : base(path, permissions) {
            BootFile = bootFile;
        }

        public override string Read() {
            return File.ReadAllText(BootFile);
        }
    }
}