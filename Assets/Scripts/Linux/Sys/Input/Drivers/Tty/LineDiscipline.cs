using System.Collections.Generic;
using Linux.IO;
using Linux.PseudoTerminal;
using Linux.Sys.IO;
using UnityEngine;

namespace Linux.Sys.Input.Drivers.Tty {
    public class PtyLineDiscipline {
        ushort[] _nullArg = new ushort[0];

        protected ushort[] Flags = { 0 };

        protected BufferedStream Buffer;

        protected List<string> SpecialChars;

        protected IoctlDevice Pts;

        protected IoctlDevice Output;

        public PtyLineDiscipline(
            IoctlDevice pts,
            IoctlDevice output
        ) {
            Pts = pts;
            Output = output;
            SpecialChars = CharacterControl.GetConstants();

            Pts.Ioctl(
                (ushort)PtyIoctl.TIO_SET_DRIVER_FLAGS,
                ref Flags
            );

            Buffer = new BufferedStream(AccessMode.O_RDWR);

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
                output = null;
            }

            if (((Flags[0] & PtyFlags.AUTO_CONTROL) != 0) 
                    && IsSpecialChar(input)) {
                HandleCharControl(input);
                output = null;
            }

            if ((Flags[0] & PtyFlags.BUFFERED) == 0) {
                WritePts(input);                
            } else {
                if (IsSpecialChar(input)) {
                    output = null;
                } else {
                    Buffer.Write(input);
                }

                if (input == $"{AbstractTextIO.LINE_FEED}") {
                    string data = Buffer.Read();
                    WritePts(data);
                }
            }

            if (output != null) {
                Output.Ioctl(
                    (ushort)PtyIoctl.TIO_SEND_KEY,
                    output
                );
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
                Pts.Ioctl((ushort)PtyIoctl.TIO_RCV_INPUT, key);
            }
        }
        
        protected bool IsSpecialChar(string key) {
            return SpecialChars.Contains(key);
        }

        protected void HandleCharControl(string key) {
            ushort signal = (ushort)GetIoctlFromControl(key);

            Output.Ioctl(signal, ref _nullArg);
        }

        protected PtyIoctl GetIoctlFromControl(string key) {
            switch(key) {
                case CharacterControl.C_DBACKSPACE: {
                    return PtyIoctl.TIO_REMOVE_BACK;
                }

                case CharacterControl.C_DDELETE: {
                    return PtyIoctl.TIO_REMOVE_FRONT;
                }

                case CharacterControl.C_DLEFT_ARROW: {
                    return PtyIoctl.TIO_LEFT_ARROW;
                }

                case CharacterControl.C_DRIGHT_ARROW: {
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
    }
}