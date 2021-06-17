using System.Collections.Concurrent;
using System.Collections.Generic;
using Linux.Configuration;
using Linux.PseudoTerminal;
using Linux.FileSystem;
using Linux.IO;
using Linux.Sys.IO;
using Linux.Sys.RunTime;

namespace Linux.Sys.Input.Drivers.Tty
{
    public class GenericPtyDriver : ITtyDriver
    {
        protected UserSpace UserSpace;

        protected Kernel Kernel;

        protected UEvent KbdEvent;

        protected UEvent DisplayEvent;

        public GenericPtyDriver(Linux.Kernel kernel) {
            Kernel = kernel;
            UserSpace = new UserSpace(new KernelSpace(kernel));

            KbdEvent = kernel.EventTable.LookupByType(DevType.KEYBOARD);
            DisplayEvent = kernel.EventTable.LookupByType(DevType.DISPLAY);
        }

        public bool IsSupported(GenericDevice device) {
            return false;
        }

        public void Handle(IRQCode code) {
            //
        }

        public void RemovePt(string ptsFile) {
            string postHooKey = BuildHooKey(ptsFile);

            System.Action<UEvent> action;

            if (!Kernel.PostInterruptHooks.TryRemove(postHooKey, out action)) {
                throw new System.ArgumentException(
                    $"Pseudo-terminal '{ptsFile}' does not exist"
                );
            }

            Kernel.Fs.Delete(ptsFile);
        }

        public int UnlockPt(
            string ptsFile,
            int uid,
            int gid,
            int permission
        ) {
            string postHooKey = BuildHooKey(ptsFile);

            if (Kernel.PostInterruptHooks.ContainsKey(postHooKey)) {
                throw new System.ArgumentException(
                    $"Pseudo-terminal '{ptsFile}' already unlocked"
                );
            }

            IoctlDevice rStream;

            if (KbdEvent == null) {
                rStream = new CharacterDevice(AccessMode.O_RDWR);
            } else {
                rStream = (IoctlDevice)UserSpace.Open(
                    KbdEvent.FilePath,
                    AccessMode.O_RDWR
                );
            }

            SecondaryPty secondaryPty;
            IoctlDevice wStream;

            if (DisplayEvent == null) {
                wStream = new CharacterDevice(AccessMode.O_WRONLY);
                secondaryPty = new SecondaryPty(_ => { }, _ => { });
            } else {
                wStream = (IoctlDevice)UserSpace.Open(
                    DisplayEvent.FilePath,
                    AccessMode.O_WRONLY
                );

                secondaryPty = new SecondaryPty(
                    data => {
                        wStream.Write(data);
                    },
                    data => {
                        // Send fake keyboard input
                        rStream.Write(data);

                        // Inform kernel LineDiscipline about data
                        Kernel.PostInterruptHooks[postHooKey](KbdEvent);
                    }
                );
            }

            Kernel.Fs.Create(
                ptsFile,
                uid,
                gid,
                permission,
                FileType.F_CHR,
                secondaryPty
            );
            
            int ptsFd = UserSpace.Api.Open(ptsFile, AccessMode.O_RDWR);

            var lineDiscipline = new PtyLineDiscipline(
                Kernel,
                secondaryPty,
                wStream
            );

            System.Action<UEvent> hookAction = (UEvent evt) => {
                if (KbdEvent == evt && rStream.GetLength() > 0) {
                    string data = rStream.Read(1);
                    lineDiscipline.Receive(data);
                }
            };

            if (!Kernel.PostInterruptHooks.TryAdd(postHooKey, hookAction)) {
                throw new System.InvalidOperationException(
                    "Failed to register kernel interrup hook for PTY"
                );
            }

            return ptsFd;
        }

        protected string BuildHooKey(string ptsFile) {
            return $"keyboard-pts-{ptsFile}";
        }
    }
}