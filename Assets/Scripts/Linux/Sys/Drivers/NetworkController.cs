using Linux.Sys;
using Linux.Sys.Net.Drivers;
using Linux.Net;

namespace Linux.Sys.Drivers
{
    public class NetworkControllerDriver : IPciDriver
    {
        protected VirtualEthernetTransport Transport;

        public NetworkControllerDriver(VirtualEthernetTransport transport) {
            Transport = transport;
        }

        public bool IsSupported(Pci pci) {
            return pci.Major == 8086 && 
                    pci.Class == PciClass.NET;
        }

        public IDeviceDriver FindDevDriver(GenericDevice input) {
            return new VirtualNetDriver(
                new NetInterface(Transport, input.Options["hwAddress"])
            );
        }
    }
}