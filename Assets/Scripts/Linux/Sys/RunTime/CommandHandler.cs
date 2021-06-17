using System.Collections.Generic;
using Linux.Configuration;
using Linux.FileSystem;
using Linux.Library.RunTime;
using Linux.PseudoTerminal;
using Linux.IO;
using Linux.Sys.Input.Drivers.Tty;
using UnityEngine;

namespace Linux.Sys.RunTime
{
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
            int returnCode = 255;

            try {
                returnCode = InternalHandle();
            }

            catch (ExitProcessException exc) {
                returnCode = exc.ExitCode;
            }

            catch (System.Exception e) {
                Debug.Log($"cmdhandler: {e.Message}");
                Debug.Log(e.ToString());
            }

            Process process = Kernel.ProcTable.LookupPid(Api.GetPid());
            process.ReturnCode = returnCode;
        }

        protected int InternalHandle() {
            ITextIO stream = Api.LookupByFD(0);

            int pid = Api.GetPid();

            if (stream != null && stream is SecondaryPty) {
                var pty = (SecondaryPty)stream;
                int[] pidArray = new int[] { pid };

                pty.Ioctl(
                    PtyIoctl.TIO_SET_PID,
                    ref pidArray
                );
            }

            Process proc = Kernel.ProcTable.LookupPid(pid);

            var userSpace = new UserSpace(Api);

            ITextIO stdout = userSpace.Stdout;

            int SIGINTCount = 0;

            Api.Trap(ProcessSignal.SIGINT, (int[] args) => {
                SIGINTCount++;

                ProcessSignal signal;

                if (SIGINTCount == 1) {
                    signal = ProcessSignal.SIGTERM;
                } else {
                    signal = ProcessSignal.SIGKILL;
                }

                stdout.WriteLine("^C");

                Kernel.KillProcess(proc, signal);
            });

            string executable = Api.GetExecutable();

            File execFile = Kernel.Fs.LookupOrFail(executable);

            AbstractRunTimeHandler handler = FindAvailableHandler(execFile);

            if (handler == null) {
                userSpace.Stderr.WriteLine($"{executable}: invalid command");
                return 255;
            }
                
            return handler.Execute(execFile);
        }

        protected void KeyboardInterrupt(int[] args) {
            throw new KeyboardInterruptException();
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