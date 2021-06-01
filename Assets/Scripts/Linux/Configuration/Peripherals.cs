using Linux.FileSystem;
using Linux.IO;
using Linux.Sys;

namespace Linux.Configuration
{    
    public class PeripheralsTable {
        protected VirtualFileTree Fs;

        public PeripheralsTable(VirtualFileTree fs) {
            Fs = fs;

            Fs.CreateDir(
                "/sys/devices",
                0, 0,
                Perm.FromInt(7, 5, 5)
            );

            Fs.CreateDir(
                "/sys/devices/pci0000:00",
                0, 0,
                Perm.FromInt(7, 5, 5)
            );
        }

        public void Add(Pci pci) {
            File devicesDir = DataSource();

            var slotDirectory = new File(
                PathUtils.Combine(devicesDir.Path, pci.Slot),
                0, 0,
                Perm.FromInt(7, 5, 5),
                FileType.F_DIR
            );

            Fs.AddFrom(devicesDir, slotDirectory);

            WriteSpec(slotDirectory, "vendor", pci.Vendor);
            WriteSpec(slotDirectory, "product", pci.Product);
            WriteSpec(slotDirectory, "class", pci.Class);
            WriteSpec(slotDirectory, "dev", $"{pci.Major}:{pci.Minor}");
        }

        public Pci[] ToArray() {
            File devicesDir = DataSource();

            Pci[] devices = new Pci[devicesDir.ChildsCount()];

            int i = 0;
            foreach(File child in devicesDir.ListChilds()) {
                if (child.Type == FileType.F_DIR) {
                    Pci pci = DirectoryToPci(child);

                    if (pci != null) {
                        devices[i] = pci;
                    }
                }

                i++;
            }

            return devices;
        }

        public File DataSource() {
            return Fs.Lookup("/sys/devices/pci0000:00");
        }

        protected Pci DirectoryToPci(File file) {
            string[] numbers = ReadSpec(file.FindChild("dev")).Split(':');

            int major;
            if (!int.TryParse(numbers[0], out major)) {
                return null;
            }

            int minor;
            if (!int.TryParse(numbers[1], out minor)) {
                return null;
            }

            return new Pci(
                ReadSpec(file.FindChild("vendor")),
                ReadSpec(file.FindChild("product")),
                file.Name,
                major,
                minor,
                ReadSpec(file.FindChild("class"))
            );
        }

        protected void WriteSpec(
            File directory,
            string fileName, 
            string content
        ) {
            File file = Fs.Create(
                PathUtils.Combine(directory.Path, fileName),
                0, 0,
                Perm.FromInt(4, 4, 4)
            );

            using(ITextIO stream = Fs.Open(file.Path, AccessMode.O_WRONLY))
            {
                stream.WriteLine(content);
            }
        }

        protected string ReadSpec(File file) {
            using(ITextIO stream = Fs.Open(file.Path, AccessMode.O_RDONLY))
            {
                return stream.ReadLine();
            }
        }
    }
}