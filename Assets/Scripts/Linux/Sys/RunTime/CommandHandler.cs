using Linux.Configuration;
using Linux.FileSystem;
using Linux.Library;
using UnityEngine;

namespace Linux.Sys.RunTime
{
    public class ExitProcessException : System.Exception {
        public int ExitCode { get; protected set; }

        public ExitProcessException(int exitCode) : base("Process exited")
        {
            ExitCode = exitCode;
        }
    }

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
                int returnCode = HandleCompiledBin((CompiledBin)execFile);
            }
        }

        protected int HandleCompiledBin(CompiledBin bin) {
            int returnCode;

            try {
                returnCode = bin.Execute(new UserSpace(Api));
            }
            
            catch (ExitProcessException exc) {
                returnCode = exc.ExitCode;
            }
            
            catch (System.Exception exception) {
                Debug.Log($"{bin.Name}: {exception.Message}");
                Debug.Log(exception.ToString());
                returnCode = 255;
            }

            return returnCode;
        }
    }
}