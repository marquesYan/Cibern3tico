using UnityEngine;

namespace Linux
{    
    public class Subsystem : MonoBehaviour {
        Linux.Kernel Kernel;

        void Start() {
            Kernel = new Linux.Kernel(this);
            Kernel.Bootstrap();
        }

        void OnGUI() {
            Kernel.Terminal.OnGUI();
        }
    }
}