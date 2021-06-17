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
        protected int[] Pid;
        protected string[] SpecialChars;
        protected string[] UnbufferedChars;

        protected Action<string> OnWrite;
        protected Action<string> OnKeyboard;

        public SecondaryPty(
            Action<string> onWrite,
            Action<string> onKeyboard
        ) : base(AccessMode.O_RDWR) {
            OnWrite = onWrite;
            OnKeyboard = onKeyboard;
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

                case PtyIoctl.TIO_SET_PID_FLAG: {
                    if (Pid == null) {
                        Pid = args;
                    } else {
                        throw new System.ArgumentException(
                            "Pty pid flag already set"
                        );
                    }
                    break;
                }

                case PtyIoctl.TIO_SET_PID: {
                    Pid[0] = args[0];
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
                    if (SpecialChars == null) {
                        SpecialChars = data;
                    } else {
                        throw new System.ArgumentException(
                            "Pty special chars already set"
                        );
                    }
                    break;
                }

                case PtyIoctl.TIO_SET_UNBUFFERED_CHARS: {
                    if (UnbufferedChars == null) {
                        UnbufferedChars = data;
                    } else {
                        throw new System.ArgumentException(
                            "Pty unbuffered chars already set"
                        );
                    }
                    break;
                }

                case PtyIoctl.TIO_ADD_UNBUFFERED_CHARS: {
                    if (UnbufferedChars.Contains(data[0])) {
                        break;
                    }

                    int index = Array.IndexOf(UnbufferedChars, null);

                    if (index > (UnbufferedChars.Length - 1)) {
                        throw new System.InvalidOperationException(
                            "Reached maximum of unbuffered chars"
                        );
                    }

                    UnbufferedChars[index] = data[0];
                    break;
                }

                case PtyIoctl.TIO_DEL_SPECIAL_CHARS: {
                    string key = data[0];
                    
                    int index = Array.FindIndex(
                        SpecialChars,
                        s => s == key
                    );

                    if (index  != -1) {
                        SpecialChars[index] = null;
                    }

                    break;
                }

                case PtyIoctl.TIO_SEND_KEY: {
                    OnKeyboard(data[0]);
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