using System;
using System.Linq;
using Linux.IO;
using Linux.Sys.IO;
using Linux.Sys.Input.Drivers.Tty;
using UnityEngine;

namespace Linux.PseudoTerminal
{
    public class SecondaryPty : CharacterDevice {
        protected int[] Flags;
        protected string[] SpecialChars;

        protected Action<string> OnWrite;

        public SecondaryPty(Action<string> onWrite) : base(AccessMode.O_RDWR) {
            OnWrite = onWrite;
        }

        public override void Ioctl(ushort signal, ref int[] args) {
            switch (signal) {
                case PtyIoctl.TIO_SET_DRIVER_FLAGS: {
                    if (Flags == null) {
                        Flags = args;
                    } else {
                        throw new System.ArgumentException(
                            "Pty flags already set"
                        );
                    }
                    break;
                }

                case PtyIoctl.TIO_SET_FLAG: {
                    Flags[0] |= args[0];
                    break;
                }

                case PtyIoctl.TIO_UNSET_FLAG: {
                    Flags[0] &= (~args[0]) & 0b1111_1111_1111;
                    break;
                }

                default: {
                    throw new System.ArgumentException(
                        "Unknow ioctl signal: " + signal
                    );
                }
            }
        }

        public override void Ioctl(ushort signal, ref string[] data) {
            switch (signal) {
                case PtyIoctl.TIO_RCV_INPUT: {
                    DigestInput(data[0]);
                    break;
                }

                case PtyIoctl.TIO_SET_SPECIAL_CHARS: {
                    SpecialChars = data;
                    break;
                }

                case PtyIoctl.TIO_DEL_SPECIAL_CHARS: {
                    string key = data[0];
                    
                    int index = Array.FindIndex(
                        SpecialChars,
                        s => s == key
                    );

                    SpecialChars[index] = null;
                    break;
                }

                default: {
                    throw new System.ArgumentException(
                        "Unknow ioctl signal: " + signal
                    );
                }
            }
        }

        protected override int InternalAppend(string data) {
            OnWrite(data);

            return 0;
        }

        protected void DigestInput(string data) {
            lock(StreamLock) {
                foreach (char inputChar in data) {
                    Buffer.Enqueue(inputChar.ToString());
                }

                Length += data.Length;
            }
        }
    }

    public class PrimaryPty : CharacterDevice
    {
        public PrimaryPty() : base(AccessMode.O_RDWR) { }

        public override void Ioctl(ushort signal, ref int[] args) {
            //
        }
    }
}