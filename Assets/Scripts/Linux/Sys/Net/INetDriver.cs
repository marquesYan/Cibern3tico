using Linux.Sys;
using Linux.Net;

namespace Linux.Sys.Net
{
    public interface INetDriver : IDeviceDriver
    {
        NetInterface CreateInterface();
    }
}