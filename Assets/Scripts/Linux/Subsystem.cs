using UnityEngine;
using Linux.Sys;
using Linux.Sys.IO;

namespace Linux
{    
    public class Subsystem : MonoBehaviour {
        public Linux.Kernel Kernel;

        public string ConsoleDeviceId;

        void Start() {
            var machine = new VirtualMachine("I4440FX", 4);

            ConsoleDeviceId = machine.AttachUSB(
                "Unity Game Console",
                255,
                DevType.CONSOLE
            );

            Kernel = new Linux.Kernel(machine);
            Kernel.Bootstrap();
        }

        void Update() {
            Kernel.Interrupt(ConsoleDeviceId, IRQCode.READ);
        }

        void OnGUI() {
            // Kernel.Terminal.DrawGUI();
        }
    }
}