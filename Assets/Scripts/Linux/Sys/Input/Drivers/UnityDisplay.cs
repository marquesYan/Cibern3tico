using System.Text;
using UnityEngine;
using Linux.Devices.Input;
using Linux.IO;
using Linux.Sys.IO;

namespace Linux.Sys.Input.Drivers
{
    public class TerminalDevice : AbstractTextIO {
        protected UnityTerminal BackendTerminal;

        public TerminalDevice(
            UnityTerminal backendTerminal
        ) : base(AccessMode.O_WRONLY) {
            BackendTerminal = backendTerminal;
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
            return BackendTerminal.SendToSreen(data);
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
