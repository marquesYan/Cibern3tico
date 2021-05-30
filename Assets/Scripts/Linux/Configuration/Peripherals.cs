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
            WriteSpec(slotDirectory, "dev", $"{pci.Major}:{pci.Minor}");
        }

        public File DataSource() {
            return Fs.Lookup("/sys/devices/pci0000:00");
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

            ITextIO stream = Fs.Open(file, AccessMode.O_WRONLY);

            try {
                stream.WriteLine(content);
            } finally {
                stream.Close();
            }
        }
    }
}