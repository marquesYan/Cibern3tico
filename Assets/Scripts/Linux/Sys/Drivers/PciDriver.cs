using Linux.Sys;

namespace Linux.Sys.Drivers
{
    public interface IPciDriver
    {
        bool IsSupported(Pci pci);

        IDeviceDriver FindDevDriver(GenericDevice input);
    }
}