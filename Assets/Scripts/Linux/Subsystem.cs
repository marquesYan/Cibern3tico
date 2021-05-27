using System.Text;
using UnityEngine;

namespace Linux
{    
    public class Subsystem : MonoBehaviour {
        public Linux.Kernel Kernel;

        public Linux.FileSystem.AbstractFile KeyboardEvent;

        void Start() {
            Kernel = new Linux.Kernel(this);
            Kernel.Bootstrap();

            KeyboardEvent = Kernel.Fs.Lookup("/dev/input/event0");
        }

        void Update() {
            string inputStr = Input.inputString;
            if (! string.IsNullOrEmpty(inputStr)) {
                KeyboardEvent.Write(new string[] { inputStr });
            }
        }

        void OnGUI() {
            Kernel.Terminal.OnGUI();
        }
    }
}