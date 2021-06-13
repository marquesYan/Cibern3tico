using System;
using Linux.IO;
using Linux.Sys.IO;

namespace Linux.Net {
    public class VirtualCable : CharacterDevice {
        protected Action<string> OnBroadcast;

        public VirtualCable(Action<string> onBroadcast) : base(AccessMode.O_RDWR) {
            OnBroadcast = onBroadcast;
        }

        public override void Ioctl(ushort signal, ref string[] args) {
            switch(signal) {
                case NetIO.SEND_BROADCAST: {
                    OnBroadcast(args[0]);
                    break;
                }

                default: {
                    throw new ArgumentException(
                        "Unknow signal: " + signal
                    );
                }
            }
        }
    }
}