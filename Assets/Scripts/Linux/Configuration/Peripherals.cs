using Linux.FileSystem;
using Linux.IO;
using Linux.Sys;

namespace Linux.Configuration
{    
    public class PeripheralsTable {
        protected VirtualFileTree Fs;

        public PeripheralsTable(VirtualFileTree fs) {
            Fs = fs;

            Fs.AddFrom(
                (Directory)Fs.Lookup("/sys/devices"),
                new Directory(
                    "/sys/devices/pci0000:00",
                    0, 0,
                    Perm.FromInt(7, 5, 5)
                )
            );
        }

        public void Add(Pci pci) {
            Directory devices = DataSource();

            var slotDirectory = new Directory(
                Fs.Combine(devices.Path, pci.Slot),
                0, 0,
                Perm.FromInt(7, 5, 5)
            );

            Fs.AddFrom(devices, slotDirectory);

            WriteSpec(slotDirectory, "vendor", pci.Vendor);
            WriteSpec(slotDirectory, "product", pci.Product);
            WriteSpec(slotDirectory, "dev", $"{pci.Major}:{pci.Minor}");
        }

        public Directory DataSource() {
            return (Directory)Fs.Lookup("/sys/devices/pci0000:00");
        }

        protected void WriteSpec(
            Directory directory, 
            string fileName, 
            string content
        ) {
            var file = new File(
                Fs.Combine(directory.Path, fileName),
                0, 0,
                Perm.FromInt(4, 4, 4)
            );

            Fs.AddFrom(directory, file);

            TextStreamWrapper stream = Fs.Open(file, AccessMode.O_WRONLY);

            try {
                stream.WriteLine(content);
            } finally {
                stream.Close();
            }
        }
    }
}