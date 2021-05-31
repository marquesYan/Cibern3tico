using System.Collections.Generic;
using Linux.Sys;
using Linux.Sys.Input;

namespace Linux.Sys.Drivers
{
    public class UsbControllerDriver : IPciDriver
    {
        protected List<IUdevDriver> DeviceDrivers;

        public UsbControllerDriver() {
            DeviceDrivers = new List<IUdevDriver>();
        }

        public bool IsSupported(Pci pci) {
            return pci.Major == 189 && 
                    pci.Class == PciClass.INPUT;
        }

        public void Register(IUdevDriver driver) {
            DeviceDrivers.Add(driver);
        }

        public void Unregister(IUdevDriver driver) {
            DeviceDrivers.Remove(driver);
        }

        public IDeviceDriver FindDevDriver(GenericDevice input) {
            return DeviceDrivers.Find(driver => driver.IsSupported(input));
        }
    }
}