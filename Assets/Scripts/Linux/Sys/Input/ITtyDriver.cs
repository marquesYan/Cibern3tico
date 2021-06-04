using Linux.Sys.IO;

namespace Linux.Sys.Input
{
    public interface ITtyDriver : IDeviceDriver
    {
        CharacterDevice GetPt();

        int UnlockPt(string ptsFile);
    }
}