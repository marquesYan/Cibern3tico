using System.Collections.Generic;
using Linux.Configuration;
using Linux.FileSystem;
using Linux.Library.RunTime;
using Linux.IO;
using Linux.Sys.Input.Drivers.Tty;
using Linux.Sys.IO;
using UnityEngine;

namespace Linux.Sys.RunTime
{
    public class CommandHandler {
        protected Linux.Kernel Kernel;

        protected KernelSpace Api;

        protected List<AbstractRunTimeHandler> RunTimeHandlers;

        public CommandHandler(Linux.Kernel kernel) {
            Kernel = kernel;
            Api = new KernelSpace(Kernel);
            RunTimeHandlers = new List<AbstractRunTimeHandler>();

            AddDefaultHandlers();
        }

        public void Handle() {
            // Make sure executable receive a fresh kernel space instance
            var procSpace = new UserSpace(new KernelSpace(Kernel));

            int returnCode = 255;

            Process process = Kernel.ProcTable.LookupPid(
                procSpace.Api.GetPid()
            );

            try {
                returnCode = InternalHandle(procSpace);
            }

            catch (ExitProcessException exc) {
                returnCode = exc.ExitCode;
            }

            catch (System.Exception e) {
                Debug.Log($"cmdhandler: {e.Message}");
                Debug.Log(e.ToString());
            }

            // if (procSpace.Api.IsTtyControlled()) {
            //     int ppid = procSpace.Api.GetPPid();

            //     var pty = (IoctlDevice)procSpace.Stdin;

            //     int[] pidArray = new int[] { ppid };

            //     // Set controll back to parent process
            //     pty.Ioctl(
            //         PtyIoctl.TIO_SET_PID,
            //         ref pidArray
            //     );
            // }

            process.ReturnCode = returnCode;
        }

        protected int InternalHandle(UserSpace procSpace) {
            int pid = procSpace.Api.GetPid();

            if (procSpace.Api.IsTtyControlled()) {
                var pty = (IoctlDevice)procSpace.Stdin;

                int[] pidArray = new int[] { pid };

                // Set pty as controlling terminal for this process
                pty.Ioctl(
                    PtyIoctl.TIO_SET_PID,
                    ref pidArray
                );
            }

            Process proc = Kernel.ProcTable.LookupPid(pid);

            int SIGINTCount = 0;

            procSpace.Api.Trap(ProcessSignal.SIGINT, (int[] args) => {
                SIGINTCount++;

                ProcessSignal signal;

                if (SIGINTCount == 1) {
                    signal = ProcessSignal.SIGTERM;
                } else {
                    signal = ProcessSignal.SIGKILL;
                }

                procSpace.Stdout.WriteLine("^C");

                Kernel.KillProcess(proc, signal);
            });

            string executable = procSpace.Api.GetExecutable();

            File execFile = Kernel.Fs.LookupOrFail(executable);

            AbstractRunTimeHandler handler = FindAvailableHandler(execFile);

            if (handler == null) {
                procSpace.Stderr.WriteLine($"{executable}: invalid command");
                return 255;
            }
                
            return handler.Execute(procSpace, execFile);
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