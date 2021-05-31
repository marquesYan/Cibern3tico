using UnityEngine;
using Linux.Sys;
using Linux.Sys.Drivers;
using Linux.Sys.Input.Drivers;
using Linux.Sys.IO;

namespace Linux
{    
    public class Subsystem : MonoBehaviour {
        public Linux.Kernel Kernel;

        public Pci ConsolePci;

        void Start() {
            var machine = new VirtualMachine("I4440FX", 4);

            machine.BiosDrivers.Add(GetUnityDriver());

            ConsolePci = machine.AttachUSB(
                "Unity Game Console",
                255,
                DevType.CONSOLE
            );

            Kernel = new Linux.Kernel(machine);
            Kernel.Bootstrap();
        }

        IPciDriver GetUnityDriver() {
            var usbDriver = new UsbControllerDriver();
            usbDriver.Register(new UnityKbdDriver());
            return usbDriver;
        }

        void Update() {
            Kernel.Interrupt(ConsolePci, IRQCode.READ);
        }

        void OnGUI() {
            // Kernel.Terminal.DrawGUI();
        }
    }
}