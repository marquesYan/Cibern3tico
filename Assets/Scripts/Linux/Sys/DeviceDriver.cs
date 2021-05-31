using Linux.Sys;
using Linux.Sys.IO;

namespace Linux.Sys
{
    public interface IDeviceDriver
    {
        bool IsSupported(GenericDevice device);

        void Handle(IRQCode interrupt);
    }
}