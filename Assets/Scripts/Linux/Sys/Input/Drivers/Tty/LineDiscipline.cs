using Linux.IO;
using Linux.Sys.IO;
using UnityEngine;

namespace Linux.Sys.Input.Drivers.Tty {
    public class PtyLineDiscipline {
        protected ushort[] Flags = { 0 };

        protected BufferedStream Buffer;

        
        public CharacterDevice Pts;

        public PtyLineDiscipline(CharacterDevice pts) {
            Pts = pts;

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
        }

        public string Receive(string input) {
            string output = input;

            if ((Flags[0] & PtyFlags.ECHO) == 0) {
                output = null;
            }

            if ((Flags[0] & PtyFlags.BUFFERED) != 0) {
                Buffer.Write(input);

                if (input == $"{AbstractTextIO.LINE_FEED}") {
                    string data = Buffer.Read();
                    Debug.Log("sending buffer to Pts: " + data);
                    Pts.Write(data);
                }
            } else {
                Debug.Log("sending unbuffered data to Pts");
                Pts.Write(input);
            }

            return output;
        }
    }
}