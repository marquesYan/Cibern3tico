using UnityEngine;

namespace Linux
{    
    public class Subsystem : MonoBehaviour {
        public Linux.Kernel Kernel;

        void Start() {
            Kernel = new Linux.Kernel(this);
            Kernel.Bootstrap();
        }

        void Update() {
            Kernel.InputDriver.Handle(null);
        }

        void OnGUI() {
            Kernel.Terminal.DrawGUI();
        }
    }
}