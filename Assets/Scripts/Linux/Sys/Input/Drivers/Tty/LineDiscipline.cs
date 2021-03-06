using System.Collections.Generic;
using System.Text;
using System.Linq;
using Linux.Configuration;
using Linux.PseudoTerminal;
using Linux.Sys.IO;
using Linux.Sys.RunTime;
using Linux.Configuration;
using Linux.IO;
using Linux;
using UnityEngine;

namespace Linux.Sys.Input.Drivers.Tty {
    public class PtyLineDiscipline {
        readonly object _cursorLock = new object();

        int[] _nullArg = new int[0];

        protected int[] Flags = { 0 };

        protected int[] Pid = { -1 };

        protected StringBuilder Buffer;

        protected int Pointer;

        protected string[] SpecialChars;

        protected string[] UnbufferedChars;

        protected IoctlDevice Pts;

        protected IoctlDevice Output;

        protected Kernel Kernel;

        protected bool ControlPressed = false;

        public PtyLineDiscipline(
            Kernel kernel,
            IoctlDevice pts,
            IoctlDevice output
        ) {
            Kernel = kernel;
            Pts = pts;
            Output = output;
            SpecialChars = CharacterControl.GetConstants().ToArray();

            UnbufferedChars = new string[32];

            Pts.Ioctl(
                PtyIoctl.TIO_SET_DRIVER_FLAGS,
                ref Flags
            );

            Pts.Ioctl(
                PtyIoctl.TIO_SET_SPECIAL_CHARS,
                ref SpecialChars
            );

            Pts.Ioctl(
                PtyIoctl.TIO_SET_UNBUFFERED_CHARS,
                ref UnbufferedChars
            );

            Pts.Ioctl(
                PtyIoctl.TIO_SET_PID_FLAG,
                ref Pid
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
            lock(_cursorLock) {
                InternalReceive(input);
            }
        }

        protected void InternalReceive(string input) {
            string output = input;

            if ((Flags[0] & PtyFlags.ECHO) == 0) {
                output = null;
            }

            if (((Flags[0] & PtyFlags.AUTO_CONTROL) != 0) 
                    && IsSpecialChar(input)) {
                HandleCharControl(input);
                output = null;
            }

            if ((Flags[0] & PtyFlags.BUFFERED) == 0
                    && IsUnbufferedChar(input)) {
                // send through
                WritePts(input);
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
                if (ControlPressed) {
                    switch (output) {
                        case "l": {
                            Output.Ioctl(PtyIoctl.TIO_CLEAR, ref _nullArg);
                            RemoveAtBack();
                            break;
                        }

                        case "c": {
                            KillAttachedProcess();
                            break;
                        }
                    }
                } else {
                    var outputArray = new string[] { output };
                    Output.Ioctl(
                        PtyIoctl.TIO_SEND_KEY,
                        ref outputArray
                    );
                }
            }
        }

        protected void KillAttachedProcess() {
            if (Pid[0] == -1) {
                return;
            }

            Process proc = Kernel.ProcTable.LookupPid(Pid[0]);

            if (proc != null) {
                Kernel.ProcSigTable.Dispatch(
                    proc,
                    ProcessSignal.SIGINT
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
                var keyArray = new string[] { key };
                Pts.Ioctl(PtyIoctl.TIO_RCV_INPUT, ref keyArray);
            }
        }
        
        protected bool IsSpecialChar(string key) {
            return SpecialChars.Contains(key);
        }

        protected bool IsUnbufferedChar(string key) {
            return UnbufferedChars.Contains(key);
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

                case CharacterControl.C_CLEAR_BUFFER: {
                    Buffer.Clear();
                    Pointer = 0;
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
            if (data == "\n") {
                int index = Buffer.Length;
                Pointer = index;
            }

            Buffer.Insert(Pointer, data);
            Pointer += data.Length;
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