using Linux.IO;

namespace Linux.Sys.IO
{
    public interface IoctlDevice : ITextIO {
        void Ioctl(int signal, params string[] args);
    }
}