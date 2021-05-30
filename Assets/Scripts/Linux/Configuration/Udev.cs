using System.Collections.Generic;
using Linux.FileSystem;
using Linux.Sys;
using Linux.Sys.Input;
using Linux.Sys.IO;

namespace Linux.Configuration
{
    public class UEvent {
        public readonly int Id;

        public readonly GenericDevice Device;
        public readonly IUdevDriver Driver;

        public readonly CharacterDevice CharDev;

        public UEvent(
            int id,
            GenericDevice device,
            IUdevDriver driver,
            CharacterDevice charDev
        ) {
            Id = id;
            Device = device;
            Driver = driver;
            CharDev = charDev;
        }
    }

    public class UdevTable {
        protected Dictionary<string, UEvent> Events;

        protected VirtualFileTree Fs;

        public UdevTable(VirtualFileTree fs) {
            Fs = fs;
            Events = new Dictionary<string, UEvent>();

            Fs.CreateDir(
                "/dev/input",
                0, 0, 
                Perm.FromInt(7, 5, 5)
            );
        }

        public void Add(GenericDevice device, IUdevDriver driver) {
            if (Events.ContainsKey(device.Id)) {
                throw new System.ArgumentException(
                    "Device already exists"
                );
            }

            var uEvent = new UEvent(
                Events.Count,
                device,
                driver,
                driver.CreateDevice()
            );
            
            Events.Add(device.Id, uEvent);
        }

        public UEvent LookupId(int id) {
            foreach(UEvent uEvent in Events.Values) {
                if (uEvent.Id == id) {
                    return uEvent;
                }
            }

            return null;
        }

        public UEvent LookupByDeviceId(string id) {
            UEvent uEvent;
            if (Events.TryGetValue(id, out uEvent)) {
                return uEvent;
            }
            
            return null;
        }

        public File DataSource() {
            return Fs.Lookup("/dev/input");
        }
    }
}