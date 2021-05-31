using Linux.FileSystem;
using Linux.IO;
using Linux.Sys;
using Linux.Sys.IO;

namespace Linux.Sys.Input
{
    public interface IUdevDriver
    {
        bool IsSupported(GenericDevice device);

        ITextIO CreateDevice();

        void Handle(IRQCode interrupt);
    }
}