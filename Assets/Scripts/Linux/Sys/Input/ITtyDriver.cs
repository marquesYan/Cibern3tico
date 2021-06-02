using Linux.PseudoTerminal;
using Linux.Sys;

namespace Linux.Sys.Input
{
    public interface ITtyDriver : IDeviceDriver
    {
        void Add(PrimaryPty pty, SecondaryPty pts);
    }
}