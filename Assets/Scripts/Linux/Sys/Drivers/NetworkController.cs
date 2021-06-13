using Linux.Sys;
using Linux.Sys.Net.Drivers;
using Linux.Net;

namespace Linux.Sys.Drivers
{
    public class NetworkControllerDriver : IPciDriver
    {
        protected VirtualCable VtCable;

        public NetworkControllerDriver(VirtualCable vtCable) {
            VtCable = vtCable;
        }

        public bool IsSupported(Pci pci) {
            return pci.Major == 8086 && 
                    pci.Class == PciClass.NET;
        }

        public IDeviceDriver FindDevDriver(GenericDevice input) {
            return new VirtualNetDriver(
                new NetInterface(VtCable, input.Options["hwAddress"])
            );
        }
    }
}