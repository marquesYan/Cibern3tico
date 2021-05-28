using System.Threading;
using Linux.Devices.Input;
using UnityEngine;

namespace Linux.PseudoTerminal
{
    public class EventController {
        public VirtualTerminal Terminal { get; protected set; }
        public EventFile EventFile { get; protected set; }

        public EventController(VirtualTerminal terminal, EventFile eventFile) {
            Terminal = terminal;
            EventFile = eventFile;

            Thread ctlThread = new Thread(new ThreadStart(Start));
            ctlThread.Start();
        }

        void Start() {
            while (!Terminal.IsClosed) {
                Terminal.WriteKey(EventFile.Read());
            }
        }
    }
}