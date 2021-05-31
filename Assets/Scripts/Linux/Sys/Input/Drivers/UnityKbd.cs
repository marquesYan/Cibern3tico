using System.Text;
using Linux.IO;
using Linux.Sys;
using Linux.Sys.IO;
using Linux.PseudoTerminal;
using UnityEngine;

namespace Linux.Sys.Input.Drivers
{
    public class UnityKbdDriver : IUdevDriver
    {
        protected CharacterDevice BackendDevice;

        public UnityKbdDriver() {
            BackendDevice = new CharacterDevice(AccessMode.O_RDWR);
        }

        public bool IsSupported(GenericDevice device) {
            return device.VendorId == 255;
        }

        public void Handle(IRQCode code) {
            string inputStr = UnityEngine.Input.inputString;

            if (string.IsNullOrEmpty(inputStr)) {
                CheckKeyDown();
            } else {
                HandleStringKey(inputStr);
            }
        }

        public ITextIO CreateDevice() {
            return BackendDevice;
        }

        void CheckKeyDown() {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Backspace)) {
                WriteKeyboard(CharacterControl.C_DBACKSPACE);
            } else if (UnityEngine.Input.GetKeyDown(KeyCode.Delete)) {
                WriteKeyboard(CharacterControl.C_DDELETE);
            } else if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow)) {
                WriteKeyboard(CharacterControl.C_DLEFT_ARROW);
            } else if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow)) {
                WriteKeyboard(CharacterControl.C_DRIGHT_ARROW);
            }
        }

        void WriteKeyboard(string key) {
            Debug.Log("writing key: " + key);
            BackendDevice.Write(key);
        }

        void HandleStringKey(string key) {
            switch (StringToBytes(key)) {
                case 0x08: {
                    key = CharacterControl.C_DBACKSPACE;
                    break;
                }

                case 0x7f: {
                    key = CharacterControl.C_DDELETE;
                    break;
                }
            }

            WriteKeyboard(key);
        }

        byte StringToBytes(string token) {
            return Encoding.ASCII.GetBytes(token)[0];
        }
    }
}