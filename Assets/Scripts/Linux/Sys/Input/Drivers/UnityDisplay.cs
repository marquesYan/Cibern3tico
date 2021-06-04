using System.Text;
using UnityEngine;
using Linux.Devices.Input;
using Linux.IO;
using Linux.Sys.IO;
using Linux.Sys.Input.Drivers.Tty;

namespace Linux.Sys.Input.Drivers
{
    public class TerminalDevice : AbstractTextIO, IoctlDevice {
        protected UnityTerminal BackendTerminal;

        public TerminalDevice(
            UnityTerminal backendTerminal
        ) : base(AccessMode.O_WRONLY) {
            BackendTerminal = backendTerminal;
        }

        public void Ioctl(ushort signal, ref ushort[] args) {
            switch (signal) {
                case (ushort)PtyIoctl.TIO_LEFT_ARROW: {
                    Debug.Log("moving cursor LEFT");
                    BackendTerminal.MoveCursorLeft();
                    break;
                }

                case (ushort)PtyIoctl.TIO_RIGHT_ARROW: {
                    Debug.Log("moving cursor RIGHT");
                    BackendTerminal.MoveCursorRight();
                    break;
                }
            }
        }

        public void Ioctl(ushort signal, string arg) {
            switch (signal) {
                case (ushort)PtyIoctl.TIO_SEND_KEY: {
                    BackendTerminal.ReceiveKey(arg);
                    break;
                }

                default: {
                    throw new System.ArgumentException(
                        "Unknow ioctl signal: " + signal
                    );
                }
            }
        }

        protected override void InternalTruncate() {
            if (BackendTerminal != null) {
                BackendTerminal.ClearBuffer(); 
            }
        }

        protected override bool CanMovePointer(int newPosition) {
            return false;
        }

        protected override int InternalAppend(string data) {
            return BackendTerminal.WriteToScreen(data);
        }

        protected override string InternalRead(int length) {
            return "";
        }

        protected override void InternalClose() {
            BackendTerminal.Close();
        }
    }

    public class UnityDisplayDriver : IUdevDriver
    {
        protected TerminalDevice BackendDevice;
        protected UnityTerminal VtTerminal;

        public UnityDisplayDriver(int bufferSize) {
            VtTerminal = new UnityTerminal(bufferSize);
            BackendDevice = new TerminalDevice(VtTerminal);
        }

        public bool IsSupported(GenericDevice device) {
            return device.VendorId == 255 &&
                    device.Type == DevType.DISPLAY;
        }

        public ITextIO CreateDevice() {
            return BackendDevice;
        }

        public void Handle(IRQCode code) {
            VtTerminal.DrawGUI();
        }
    }
}
