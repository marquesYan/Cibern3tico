using Linux.Sys.IO;

namespace Linux.Sys.Input
{
    public interface ITtyDriver : IDeviceDriver
    {
        int UnlockPt(
            string ptsFile,
            int uid,
            int gid,
            int permission
        );
    }
}