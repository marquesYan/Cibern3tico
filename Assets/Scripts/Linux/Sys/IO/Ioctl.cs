using Linux.IO;

namespace Linux.Sys.IO
{
    public interface IoctlDevice : ITextIO {
        void Ioctl(ushort signal, ref ushort[] args);

        void Ioctl(ushort signal, string args);
    }
}