using System.Collections.Generic;
using Linux.Sys;

namespace Linux.Sys.Drivers
{
    public class UsbControllerDriver : PciDriver
    {
        // List<

        public UsbControllerDriver(Linux.Kernel kernel) : base(kernel) {}

        public override bool IsSupported(Pci pci) {
            return pci.Major == 189;
        }

        public override void Attach(Pci pci, GenericDevice input) {
            

        }
    }
}