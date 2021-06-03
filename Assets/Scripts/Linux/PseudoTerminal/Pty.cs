using System;
using Linux.IO;
using Linux.Sys.IO;
using Linux.Sys.Input.Drivers.Tty;
using UnityEngine;

namespace Linux.PseudoTerminal
{
    public class SecondaryPty : CharacterDevice {
        protected ushort[] Flags;

        public SecondaryPty() : base(AccessMode.O_RDWR) { }

        public override void Ioctl(ushort signal, ref ushort[] args) {
            switch (signal) {
                case (ushort)PtyIoctl.TIO_SET_DRIVER_FLAGS: {
                    if (Flags == null) {
                        Flags = args;
                    } else {
                        throw new System.ArgumentException(
                            "Pty flags already set"
                        );
                    }
                    break;
                }

                case (ushort)PtyIoctl.TIO_SET_ATTR: {
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

        protected override int InternalAppend(string data) {
            Debug.Log("receiving on pts: " + data);
            return base.InternalAppend(data);
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