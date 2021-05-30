using Linux.Sys;

namespace Linux.Sys.Drivers
{
    public abstract class PciDriver
    {
        protected Linux.Kernel Kernel;

        public PciDriver(Linux.Kernel kernel) {
            Kernel = kernel;
        }

        public abstract bool IsSupported(Pci pci);

        public abstract void Attach(Pci pci, GenericDevice input);
    }
}