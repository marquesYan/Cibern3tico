using System.Collections.Generic;
using Linux.FileSystem;
using Linux.Sys;
using Linux.Sys.Input;
using Linux.IO;

namespace Linux.Configuration
{
    public class UEvent {
        public readonly int Id;

        public readonly GenericDevice Device;
        public readonly IUdevDriver Driver;
        public readonly string FilePath;

        public UEvent(
            int id,
            GenericDevice device,
            IUdevDriver driver
        ) {
            Id = id;
            Device = device;
            Driver = driver;
            FilePath = $"/dev/input/event{Id}";
        }
    }

    public class UdevTable {
        protected Dictionary<Pci, UEvent> Events;

        protected VirtualFileTree Fs;

        public UdevTable(VirtualFileTree fs) {
            Fs = fs;
            Events = new Dictionary<Pci, UEvent>();

            Fs.CreateDir(
                "/dev/input",
                0, 0, 
                Perm.FromInt(7, 5, 5)
            );
        }

        public void Close() {
            foreach(UEvent uEvent in Events.Values) {
                if (Fs.Lookup(uEvent.FilePath) != null) {
                    using (ITextIO stream = Fs.Open(uEvent.FilePath, AccessMode.O_WRONLY)) {
                        stream.WriteLine("");
                    }
                }
            }

            Events = null;
        }

        public void Add(
            Pci pci,
            GenericDevice device,
            IUdevDriver driver
        ) {
            var uEvent = new UEvent(
                Events.Count,
                device,
                driver
            );
            
            Events.Add(pci, uEvent);

            Fs.Create(
                uEvent.FilePath,
                0, 0,
                Perm.FromInt(6, 6, 0),
                FileType.F_CHR,
                driver.CreateDevice()
            );
        }

        public UEvent LookupId(int id) {
            foreach(UEvent uEvent in Events.Values) {
                if (uEvent.Id == id) {
                    return uEvent;
                }
            }

            return null;
        }

        public UEvent LookupByPci(Pci pci) {
            UEvent uEvent;
            if (Events.TryGetValue(pci, out uEvent)) {
                return uEvent;
            }
            
            return null;
        }

        public UEvent LookupByType(DevType type) {
            foreach(UEvent uEvent in Events.Values) {
                if (uEvent.Device.Type == type) {
                    return uEvent;
                }
            }

            return null;
        }
    }
}