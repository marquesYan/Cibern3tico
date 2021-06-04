using System.Threading;
using Linux.Configuration;
using Linux.PseudoTerminal;
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

        protected KernelSpace KernelSpace;

        protected UEvent KbdEvent;
        protected UEvent DisplayEvent;

        public GenericPtyDriver(Linux.Kernel kernel) {
            KernelSpace = new KernelSpace(kernel);

            KbdEvent = kernel.EventTable.LookupByType(DevType.KEYBOARD);
            DisplayEvent = kernel.EventTable.LookupByType(DevType.DISPLAY);
        }

        public bool IsSupported(GenericDevice device) {
            return false;
        }

        public void Handle(IRQCode code) {
            //
        }

        public CharacterDevice GetPt() {
            SecondaryPty secondaryPty;

            if (DisplayEvent == null) {
                secondaryPty = new SecondaryPty(_ => { });
            } else {
                int displayFd = KernelSpace.Open(
                    DisplayEvent.FilePath,
                    AccessMode.O_WRONLY
                );

                ITextIO wStream = KernelSpace.LookupByFD(displayFd);

                secondaryPty = new SecondaryPty(data => {
                    wStream.Write(data);
                });
            }

            return (CharacterDevice)secondaryPty;
        }

        public int UnlockPt(string ptsFile) {
            int ptsFd = KernelSpace.Open(ptsFile, AccessMode.O_RDWR);

            int pid = KernelSpace.StartProcess(
                new string[] {
                    "/usr/sbin/ttyctl",
                    ptsFd.ToString(),
                    KbdEvent.FilePath,
                    DisplayEvent.FilePath
                },
                ptsFd
            );

            Thread.Sleep(500);

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