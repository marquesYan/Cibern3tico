using System.Collections.Generic;
using Linux.IO;

namespace Linux.Configuration
{
    public delegate void SignalHandler(params int[] args);

    public enum ProcessSignal: ushort {
        SIGTERM = 4,
        SIGKILL = 9,
        SIGHUP = 1,
    }

    public class ProcessSignalsTable {
        protected Dictionary<Process, Dictionary<ProcessSignal, SignalHandler>> Table;

        public ProcessSignalsTable() {
            Table = new Dictionary<Process, Dictionary<ProcessSignal, SignalHandler>>();
        }

        public bool HasProcess(Process process) {
            return Table.ContainsKey(process);
        }

        public void Add(
            Process process,
            ProcessSignal signal,
            SignalHandler handler
        ) {
            if (!HasProcess(process)) {
                Table[process] = new Dictionary<ProcessSignal, SignalHandler>();
            }

            Table[process][signal] = handler;
        }

        public void Dispatch(
            Process process,
            ProcessSignal signal,
            params int[] args
        ) {
            if (HasProcess(process) && Table[process].ContainsKey(signal)) {
                Table[process][signal](args);
            }
        }
    }
}