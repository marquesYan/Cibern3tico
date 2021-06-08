using System.Collections.Concurrent;
using Linux.Configuration;
using Linux.PseudoTerminal;
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

        public CharacterDevice GetPt() {
            SecondaryPty secondaryPty;

            if (DisplayEvent == null) {
                secondaryPty = new SecondaryPty(_ => { });
            } else {
                ITextIO wStream = UserSpace.Open(
                    DisplayEvent.FilePath,
                    AccessMode.O_WRONLY
                );

                secondaryPty = new SecondaryPty(data => {
                    wStream.Write(data);
                });
            }

            return (CharacterDevice)secondaryPty;
        }

        public int UnlockPt(string ptsFile) {
            string postHooKey = BuildHooKey(ptsFile);
            
            int ptsFd = UserSpace.Api.Open(ptsFile, AccessMode.O_RDWR);

            if (Kernel.PostInterruptHooks.ContainsKey(postHooKey)) {
                throw new System.ArgumentException(
                    $"Pseudo-terminal '{ptsFile}' already unlocked"
                );
            }

            IoctlDevice ptStream = (IoctlDevice)UserSpace.Api.LookupByFD(ptsFd);

            IoctlDevice wStream;

            if (DisplayEvent == null) {
                wStream = new CharacterDevice(AccessMode.O_WRONLY);
            } else {
                wStream = (IoctlDevice)UserSpace.Open(
                    DisplayEvent.FilePath,
                    AccessMode.O_WRONLY
                );
            }

            IoctlDevice rStream;

            if (KbdEvent == null) {
                rStream = new CharacterDevice(AccessMode.O_RDONLY);
            } else {
                rStream = (IoctlDevice)UserSpace.Open(
                    KbdEvent.FilePath,
                    AccessMode.O_RDONLY
                );
            }

            var lineDiscipline = new PtyLineDiscipline(ptStream, wStream);

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