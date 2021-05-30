using Linux.FileSystem;
using Linux.Sys.IO;

namespace Linux.Sys.Input
{
    public interface IUdevDriver
    {
        CharacterDevice CreateDevice();

        void Handle(IRQCode interrupt);
    }
}