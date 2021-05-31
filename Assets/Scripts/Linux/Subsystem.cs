using UnityEngine;
using Linux.Sys;
using Linux.Sys.IO;

namespace Linux
{    
    public class Subsystem : MonoBehaviour {
        public Linux.Kernel Kernel;

        public Pci ConsolePci;

        void Start() {
            var machine = new VirtualMachine("I4440FX", 4);

            ConsolePci = machine.AttachUSB(
                "Unity Game Console",
                255,
                DevType.CONSOLE
            );

            Kernel = new Linux.Kernel(machine);
            Kernel.Bootstrap();
        }

        void Update() {
            Kernel.Interrupt(ConsolePci, IRQCode.READ);
        }

        void OnGUI() {
            // Kernel.Terminal.DrawGUI();
        }
    }
}