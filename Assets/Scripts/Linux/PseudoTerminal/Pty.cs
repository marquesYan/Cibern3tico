using System;
using Linux.IO;
using Linux.Sys.IO;
using Linux.Sys.Input.Drivers.Tty;
using UnityEngine;

namespace Linux.PseudoTerminal
{
    public class SecondaryPty : CharacterDevice {
        protected ushort[] Flags;

        protected Action<string> OnWrite;

        public SecondaryPty(Action<string> onWrite) : base(AccessMode.O_RDWR) {
            OnWrite = onWrite;
        }

        public override void Ioctl(ushort signal, ref ushort[] args) {
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

                case PtyIoctl.TIO_SET_ATTR: {
                    Flags[0] |= args[0];
                    break;
                }

                default: {
                    throw new System.ArgumentException(
                        "Unknow ioctl signal: " + signal
                    );
                }
            }
        }

        public override void Ioctl(ushort signal, string data) {
            switch (signal) {
                case PtyIoctl.TIO_RCV_INPUT: {
                    DigestInput(data);
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

        public override void Ioctl(ushort signal, ref ushort[] args) {
            //
        }
    }
}