using UnityEngine;
using Linux.Sys;
using Linux.Sys.Drivers;
using Linux.Sys.Input.Drivers;
using Linux.Sys.IO;

namespace Linux
{    
    public class Subsystem : MonoBehaviour {
        const int UnityVendorId = 255;

        public Linux.Kernel Kernel;

        public Pci DisplayPci;
        public Pci KeyboardPci;

        void Start() {
            var machine = new VirtualMachine("I4440FX", 4);

            machine.BiosDrivers.Add(GetUnityDriver());

            DisplayPci = machine.AttachUSB(
                "Unity Game Display",
                UnityVendorId,
                DevType.DISPLAY,
                "0000:00:04.0"
            );

            KeyboardPci = machine.AttachUSB(
                "Unity Game Keyboard",
                UnityVendorId,
                DevType.KEYBOARD,
                "0000:00:05.0"
            );

            Kernel = new Linux.Kernel(machine);
            Kernel.Bootstrap();
        }

        IPciDriver GetUnityDriver() {
            var usbDriver = new UsbControllerDriver();
            usbDriver.Register(new UnityKbdDriver());
            usbDriver.Register(new UnityDisplayDriver(1024 ^ 2));
            return usbDriver;
        }

        void OnApplicationQuit() {
            Kernel.Shutdown();
        }

        void Update() {
            Kernel.Interrupt(KeyboardPci, IRQCode.READ);
        }

        void OnGUI() {
            Kernel.Interrupt(DisplayPci, IRQCode.WRITE);
        }
    }
}