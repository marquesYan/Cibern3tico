using Linux.Sys.IO;

namespace Linux.Sys.Input
{
    public interface ITtyDriver : IDeviceDriver
    {
        int Add(CharacterDevice ptm, CharacterDevice pts, string ptsFile);
    }
}