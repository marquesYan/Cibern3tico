using System;
using System.Text;
using UnityEngine;
using Linux.FileSystem;
using Linux.PseudoTerminal;

namespace Linux.Sys.Input.Drivers
{
    public class UnityInputDriver : AbstractInputDriver
    {
        AbstractFile _keyboardEvent;

        public UnityInputDriver(Linux.Kernel kernel) : base(kernel) {
            _keyboardEvent = Kernel.Fs.Lookup("/dev/input/event0");
        }

        public override void Handle(object input) {
            string inputStr = UnityEngine.Input.inputString;

            if (string.IsNullOrEmpty(inputStr)) {
                CheckKeyDown();
            } else {
                HandleStringKey(inputStr);
            }
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
            _keyboardEvent.Write(new string[] { key });
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