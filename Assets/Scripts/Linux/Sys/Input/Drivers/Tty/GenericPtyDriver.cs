using System.Threading;
using Linux.Configuration;
using Linux.IO;
using Linux.Sys.IO;
using Linux.Sys.RunTime;
using UnityEngine;

namespace Linux.Sys.Input.Drivers.Tty
{
    public class GenericPtyDriver : ITtyDriver
    {
        readonly object _ptyLock = new object();

        protected delegate bool Predicate();

        protected Linux.Kernel Kernel;

        protected UEvent KbdEvent;
        protected UEvent DisplayEvent;

        public GenericPtyDriver(Linux.Kernel kernel) {
            Kernel = kernel;

            KbdEvent = Kernel.EventTable.LookupByType(DevType.KEYBOARD);
            DisplayEvent = Kernel.EventTable.LookupByType(DevType.DISPLAY);
        }

        public bool IsSupported(GenericDevice device) {
            return false;
        }

        public void Handle(IRQCode code) {
            //
        }

        public int Add(
            CharacterDevice ptm,
            CharacterDevice pts,
            string ptsFile
        ) {
            // var ptyCtl = new PtyCtlModule(pty, pts);

            // lock(_ptyLock) {
            //     Ptys.Add(ptyCtl);
            // }

            KernelSpace kernelSpace = new KernelSpace(Kernel);

            int ptsFd = kernelSpace.Open(ptsFile, AccessMode.O_RDWR);

            int pid = kernelSpace.StartProcess(
                new string[] {
                    "/usr/sbin/ttyctl",
                    ptsFd.ToString(),
                    KbdEvent.FilePath,
                    DisplayEvent.FilePath
                },
                ptsFd
            );

            Thread.Sleep(1000);

            return ptsFd;
        }

        // protected void StartWorker(Predicate action) {
        //     Thread worker = new Thread(new ThreadStart(
        //         () => {
        //             bool predicate = true;
        //             while (!Kernel.IsShutdown && predicate) {
        //                 predicate = action();

        //                 Thread.Sleep(200);
        //             }
        //         }
        //     ));

        //     worker.Start();
        // }

        // protected void StartOutputListener() {
        //     ITextIO writer = Kernel.Fs.Open(
        //         DisplayEvent.FilePath,
        //         AccessMode.O_WRONLY
        //     );

        //     StartWorker(
        //         () => {
        //             string key;

        //             if (OutputQueue.TryDequeue(out key)) {
        //                 Debug.Log("recv output: " + key);
        //                 writer.Write(key);
        //             }
                    
        //             return true;
        //         }
        //     );
        // }

        // protected void ProcessInput(string key) {
        //     lock(_ptyLock) {
        //         Ptys.ForEach(ptyCtl => {
        //             string output = ptyCtl.Receive(key);

        //             if (output != null) {
        //                 OutputQueue.Enqueue(output);
        //             }
        //         });
        //     }
        // }
    }
}