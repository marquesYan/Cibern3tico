using Linux.Sys.RunTime;
using Linux.IO;
using Linux.FileSystem;

namespace Linux.Library.RunTime
{
    public abstract class AbstractRunTimeHandler {
        protected UserSpace UserSpace;

        public AbstractRunTimeHandler(KernelSpace api) {
            UserSpace = new UserSpace(api);
        }

        public abstract bool IsFileSupported(File executable);

        public abstract int Execute(UserSpace procSpace, File executable);
    }
}