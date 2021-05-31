using System.Collections.Generic;
using Linux.Sys;
using Linux.Sys.Input;

namespace Linux.Sys.Drivers
{
    public class UsbControllerDriver : PciDriver
    {
        protected List<IUdevDriver> DeviceDrivers;

        public UsbControllerDriver(Linux.Kernel kernel) : base(kernel) {
            DeviceDrivers = new List<IUdevDriver>();
        }

        public override bool IsSupported(Pci pci) {
            return pci.Major == 189 && 
                    pci.Class == PciClass.INPUT;
        }

        public void Register(IUdevDriver driver) {
            DeviceDrivers.Add(driver);
        }

        public void Unregister(IUdevDriver driver) {
            DeviceDrivers.Remove(driver);
        }

        public override void Attach(Pci pci, GenericDevice input) {
            IUdevDriver driver = FindDriver(input);

            if (driver != null) {
                Kernel.UdTable.Add(pci, input, driver);
            }
        }

        protected IUdevDriver FindDriver(GenericDevice input) {
            return DeviceDrivers.Find(driver => driver.IsSupported(input));
        }
    }
}