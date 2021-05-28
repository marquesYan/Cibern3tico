
namespace Linux.Sys.Input.Drivers
{
    public abstract class AbstractInputDriver
    {
        protected Linux.Kernel Kernel;

        public AbstractInputDriver(Linux.Kernel kernel) {
            Kernel = kernel;
        }

        public abstract void Handle(object input);
    }
}