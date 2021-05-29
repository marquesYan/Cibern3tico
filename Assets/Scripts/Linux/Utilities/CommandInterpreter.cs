using System.Threading;
using Linux.Configuration;
using Linux.FileSystem;
using Linux.IO;

namespace Linux.Utilities
{    
    public class CommandInterpreter {
        protected Linux.Kernel Kernel;

        public CommandInterpreter(Linux.Kernel kernel) {
            Kernel = kernel;
        }

        public void Handle() {
            Process currentProcess = Kernel.ProcTable.LookupThread(Thread.CurrentThread);

            var execFile = Kernel.Fs.Lookup(currentProcess.Executable);

            if (execFile is IUtility) {
                ((IUtility) execFile).Execute(currentProcess);
            }
        }

        // public bool IsUtility(File executable) {
        //     return typeof(executable).GetInterfaces().Contains(typeof(IUtility));
        // }
    }
}