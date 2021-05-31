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

        public readonly ITextIO DevPointer;

        public UEvent(
            int id,
            GenericDevice device,
            IUdevDriver driver,
            ITextIO devPointer
        ) {
            Id = id;
            Device = device;
            Driver = driver;
            DevPointer = devPointer;
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

        public void Add(
            Pci pci,
            GenericDevice device,
            IUdevDriver driver
        ) {
            var uEvent = new UEvent(
                Events.Count,
                device,
                driver,
                driver.CreateDevice()
            );
            
            Events.Add(pci, uEvent);
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

        public File DataSource() {
            return Fs.Lookup("/dev/input");
        }
    }
}