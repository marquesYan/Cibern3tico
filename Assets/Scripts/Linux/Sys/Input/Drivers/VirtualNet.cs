using Linux.IO;
using Linux.Sys;
using Linux.Sys.IO;
using Linux.Net;

namespace Linux.Sys.Input.Drivers
{
    public class VirtualNetDriver : IUdevDriver
    {
        protected VirtualCable BackendDevice;

        public VirtualNetDriver(VirtualCable vtCable) {
            BackendDevice = vtCable;
        }

        public bool IsSupported(GenericDevice device) {
            return device.VendorId == 255 &&
                    device.Type == DevType.NETWORK;
        }

        public NetInterface GetNetInterface() {
            return new NetInterface(BackendDevice, "d4:01:29:9d:10:e2");
        }

        public void Handle(IRQCode code) {
            //
        }

        public ITextIO CreateDevice() {
            return BackendDevice;
        }
    }
}