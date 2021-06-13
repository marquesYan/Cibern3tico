using Linux.Sys;
using Linux.Sys.IO;
using Linux.Sys.Net;
using Linux.Net;

namespace Linux.Sys.Net.Drivers
{
    public class VirtualNetDriver : INetDriver
    {
        protected NetInterface Interface;

        public VirtualNetDriver(NetInterface interface_) {
            Interface = interface_;
        }

        public bool IsSupported(GenericDevice device) {
            return device.VendorId == 255 &&
                    device.Type == DevType.NETWORK;
        }

        public void Handle(IRQCode code) {
            //
        }

        public NetInterface CreateInterface() {
            return Interface;
        }
    }
}