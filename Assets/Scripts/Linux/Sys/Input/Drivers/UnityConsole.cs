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
        ) : base(AccessMode.O_RDWR) {
            BackendTerminal = backendTerminal;
        }

        protected override int InternalWrite(string data) {
            return BackendTerminal.SendToSreen(data);
        }
        protected override int InternalAppend(string data) {
            return 0;
        }
        protected override string InternalRead() {
            throw new System.ArgumentException(
                "Can not read from a raw terminal"
            );
        }

        protected override void InternalClose() {
            BackendTerminal.Close();
        }
    }

    public class UnityConsoleDriver : IUdevDriver
    {
        protected TerminalDevice BackendDevice;
        protected UnityTerminal VtTerminal;

        public UnityConsoleDriver(int bufferSize) {
            VtTerminal = new UnityTerminal(bufferSize);
            BackendDevice = new TerminalDevice(VtTerminal);
        }

        public bool IsSupported(GenericDevice device) {
            return device.VendorId == 255 &&
                    device.Type == DevType.CONSOLE;
        }

        public ITextIO CreateDevice() {
            return BackendDevice;
        }

        public void Handle(IRQCode code) {
            VtTerminal.DrawGUI();
        }
    }
}
