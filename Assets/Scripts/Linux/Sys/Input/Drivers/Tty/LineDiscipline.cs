using System.Collections.Generic;
using System.Text;
using System.Linq;
using Linux.PseudoTerminal;
using Linux.Sys.IO;
using Linux.IO;
using UnityEngine;

namespace Linux.Sys.Input.Drivers.Tty {
    public class PtyLineDiscipline {
        int[] _nullArg = new int[0];

        protected int[] Flags = { 0 };

        protected StringBuilder Buffer;

        protected int Pointer;

        protected string[] SpecialChars;

        protected IoctlDevice Pts;

        protected IoctlDevice Output;

        protected bool ControlPressed = false;

        public PtyLineDiscipline(
            IoctlDevice pts,
            IoctlDevice output
        ) {
            Pts = pts;
            Output = output;
            SpecialChars = CharacterControl.GetConstants().ToArray();

            Pts.Ioctl(
                PtyIoctl.TIO_SET_DRIVER_FLAGS,
                ref Flags
            );

            Pts.Ioctl(
                PtyIoctl.TIO_SET_SPECIAL_CHARS,
                ref SpecialChars
            );

            Buffer = new StringBuilder();
            Pointer = 0;

            CookPty();
        }

        protected void CookPty() {
            Flags[0] |= PtyFlags.BUFFERED;
            Flags[0] |= PtyFlags.ECHO;
            Flags[0] |= PtyFlags.SPECIAL_CHARS;
            Flags[0] |= PtyFlags.AUTO_CONTROL;
        }

        public void Receive(string input) {
            string output = input;

            if ((Flags[0] & PtyFlags.ECHO) == 0) {
                Debug.Log("echo disabled");
                output = null;
            }
            Debug.Log($"flags: {Flags[0]}");
            if (((Flags[0] & PtyFlags.AUTO_CONTROL) != 0) 
                    && IsSpecialChar(input)) {
                Debug.Log("auto control enabled");

                HandleCharControl(input);
                output = null;
            }

            if ((Flags[0] & PtyFlags.BUFFERED) == 0) {
                Debug.Log("unbuffered line enabled");
                if (!IsSpecialChar(input)) {
                    WritePts(input);
                }
            } else {
                if (IsSpecialChar(input)) {
                    output = null;
                } else if (input == $"{AbstractTextIO.LINE_FEED}") {
                    Pointer = 0;

                    Buffer.Append(AbstractTextIO.LINE_FEED);

                    string data = Buffer.ToString();
                    Buffer.Clear();
                    WritePts(data);
                } else {
                    WriteBuffer(input);
                }
            }

            if (output != null) {
                if (ControlPressed && output == "l") {
                    Output.Ioctl(PtyIoctl.TIO_CLEAR, ref _nullArg);
                    RemoveAtBack();
                } else {
                    var outputArray = new string[] { output };
                    Output.Ioctl(
                        PtyIoctl.TIO_SEND_KEY,
                        ref outputArray
                    );
                }
            }
        }

        protected void WritePts(string key) {
            bool shouldWrite = false;

            if ((Flags[0] & PtyFlags.SPECIAL_CHARS) == 0) {
                shouldWrite = true;
            } else if (!IsSpecialChar(key)) {
                shouldWrite = true;
            }

            if (shouldWrite) {
                var keyArray = new string[] { key };
                Pts.Ioctl(PtyIoctl.TIO_RCV_INPUT, ref keyArray);
            }
        }
        
        protected bool IsSpecialChar(string key) {
            return SpecialChars.Contains(key);
        }

        protected void HandleCharControl(string key) {
            switch(key) {
                case CharacterControl.C_DCTRL: {
                    ControlPressed = true;
                    return;
                }

                case CharacterControl.C_UCTRL: {
                    ControlPressed = false;
                    return;
                }
            }

            ushort signal = GetIoctlFromControl(key);

            Output.Ioctl(signal, ref _nullArg);
        }

        protected ushort GetIoctlFromControl(string key) {
            switch(key) {
                case CharacterControl.C_DBACKSPACE: {
                    RemoveAtBack();
                    return PtyIoctl.TIO_REMOVE_BACK;
                }

                case CharacterControl.C_DDELETE: {
                    RemoveAtFront();
                    return PtyIoctl.TIO_REMOVE_FRONT;
                }

                case CharacterControl.C_DLEFT_ARROW: {
                    MovePointer(-1);
                    return PtyIoctl.TIO_LEFT_ARROW;
                }

                case CharacterControl.C_DRIGHT_ARROW: {
                    MovePointer(1);
                    return PtyIoctl.TIO_RIGHT_ARROW;
                }

                case CharacterControl.C_DUP_ARROW: {
                    return PtyIoctl.TIO_UP_ARROW;
                }

                case CharacterControl.C_DDOWN_ARROW: {
                    return PtyIoctl.TIO_DOWN_ARROW;
                }

                default: {
                    throw new System.ArgumentException(
                        "Unknow character control: " + key
                    );
                }
            }
        }

        protected void WriteBuffer(string data) {
            Buffer.Insert(Pointer, data);
            Pointer++;
        }

        protected void RemoveAtFront() {
            if (Pointer >= Buffer.Length || Buffer.Length == 0) {
                return;
            }

            Buffer.Remove(Pointer, 1);
        }

        protected void RemoveAtBack() {
            if (Pointer == 0 || Buffer.Length == 0) {
                return;
            }

            MovePointer(-1);

            Buffer.Remove(Pointer, 1);
        }

        protected void MovePointer(int step) {
            if (Buffer.Length == 0) {
                Pointer = 0;
                return;
            }

            int pointer = Pointer + step;

            if (pointer < 0) {
                pointer = 0;
            } else if (pointer >= Buffer.Length) {
                pointer = Buffer.Length - 1;
            }

            Pointer = pointer;
        }
    }
}