using System.Collections.Generic;
using Linux.IO;
using Linux.PseudoTerminal;
using Linux.Sys.IO;
using UnityEngine;

namespace Linux.Sys.Input.Drivers.Tty {
    public class PtyLineDiscipline {
        protected ushort[] Flags = { 0 };

        protected BufferedStream Buffer;

        protected List<string> SpecialChars;

        public CharacterDevice Pts;

        public PtyLineDiscipline(CharacterDevice pts) {
            Pts = pts;
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
        }

        public string Receive(string input) {
            string output = input;

            if ((Flags[0] & PtyFlags.ECHO) == 0) {
                output = null;
            }

            if ((Flags[0] & PtyFlags.BUFFERED) != 0) {
                if (!IsSpecialChar(input)) {
                    Buffer.Write(input);
                }

                if (input == $"{AbstractTextIO.LINE_FEED}") {
                    string data = Buffer.Read();
                    WritePts(data);
                }
            } else {
                WritePts(input);
            }

            return output;
        }

        protected void WritePts(string key) {
            if ((Flags[0] & PtyFlags.SPECIAL_CHARS) == 0) {
                Pts.Write(key);
            } else if (!IsSpecialChar(key)) {
                Pts.Write(key);
            }
        }
        
        protected bool IsSpecialChar(string key) {
            return SpecialChars.Contains(key);
        }
    }
}