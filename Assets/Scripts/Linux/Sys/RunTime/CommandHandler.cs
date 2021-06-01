using Linux.Configuration;
using Linux.FileSystem;
using Linux.Library;

namespace Linux.Sys.RunTime
{    
    public class CommandHandler {
        protected KernelSpace Kernel;

        public CommandHandler(Linux.Kernel kernel) {
            Kernel = new KernelSpace(kernel);
        }

        public void Handle() {
            Process process = Kernel.GetCurrentProc();

            File execFile = Kernel.LookupFile(process.Executable);

            if (execFile is CompiledBin) {
                ((CompiledBin) execFile).Execute(new UserSpace(Kernel));
            }
        }
    }
}