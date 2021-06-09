using System.Collections.Generic;
using Linux.Configuration;
using Linux.FileSystem;
using Linux.Library.RunTime;
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

        protected List<AbstractRunTimeHandler> RunTimeHandlers;

        public CommandHandler(Linux.Kernel kernel) {
            Kernel = kernel;
            Api = new KernelSpace(Kernel);
            RunTimeHandlers = new List<AbstractRunTimeHandler>();

            AddDefaultHandlers();
        }

        public void Handle() {
            string executable = Api.GetExecutable();

            File execFile = Kernel.Fs.LookupOrFail(executable);

            int returnCode = 255;

            AbstractRunTimeHandler handler = FindAvailableHandler(execFile);
            var userSpace = new UserSpace(Api);

            if (handler == null) {
                userSpace.Stderr.WriteLine($"{executable}: invalid command");
            } else {
                returnCode = handler.Execute(execFile);
            }

            Process process = Kernel.ProcTable.LookupPid(Api.GetPid());
            process.ReturnCode = returnCode;
        }

        protected AbstractRunTimeHandler FindAvailableHandler(File file) {
            return RunTimeHandlers.Find(
                h => h.IsFileSupported(file)
            );
        }

        protected void AddDefaultHandlers() {
            RunTimeHandlers.Add(new CompiledBinHandler(Api));
            RunTimeHandlers.Add(new BashCommandHandler(Api));
        }
    }
}