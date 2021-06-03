using Linux.Configuration;
using Linux.FileSystem;
using Linux.Library;
using UnityEngine;

namespace Linux.Sys.RunTime
{    
    public class CommandHandler {
        protected KernelSpace Api;
        protected Linux.Kernel Kernel;

        public CommandHandler(Linux.Kernel kernel) {
            Kernel = kernel;
            Api = new KernelSpace(Kernel);
        }

        public void Handle() {
            string executable = Api.GetExecutable();

            File execFile = Kernel.Fs.LookupOrFail(executable);

            if (execFile is CompiledBin) {
                try {
                    ((CompiledBin) execFile).Execute(new UserSpace(Api));
                } catch (System.Exception exception) {
                    Debug.Log($"{execFile.Name}: {exception.Message}");
                }
            }
        }
    }
}