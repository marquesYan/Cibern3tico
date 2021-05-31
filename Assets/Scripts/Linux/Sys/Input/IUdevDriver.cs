using Linux.IO;
using Linux.Sys;
using Linux.Sys.IO;

namespace Linux.Sys.Input
{
    public interface IUdevDriver : IDeviceDriver
    {
        ITextIO CreateDevice();
    }
}