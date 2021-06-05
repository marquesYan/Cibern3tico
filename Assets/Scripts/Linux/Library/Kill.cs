using System.Collections.Generic;
using Linux.Configuration;
using Linux.Sys.RunTime;
using Linux.FileSystem;
using Linux.Library.ArgumentParser;
using UnityEngine;

namespace Linux.Library
{    
    public class Kill : CompiledBin {
        public Kill(
            string absolutePath,
            int uid,
            int gid,
            int permission,
            FileType type
        ) : base(absolutePath, uid, gid, permission, type) { }

        public override int Execute(UserSpace userSpace) {
            var parser = new GenericArgParser(
                userSpace,
                "Usage: {0} [-n SIGNAL] [PID,]",
                "Send signal to a process"
            );

            ProcessSignal signal = ProcessSignal.SIGTERM;
            parser.AddArgument<int>(
                "n|signal=",
                "The signal {SIGNAL} number to send. Default is '1' (SIGTERM)",
                (int signalNum) => signal = (ProcessSignal)signalNum
            );

            List<string> arguments = parser.Parse();

            if (arguments.Count < 1) {
                parser.ShowHelpInfo();
                return 1;
            }

            List<int> pids = new List<int>();

            foreach (string pidString in arguments) {
                int pid;
                if (!int.TryParse(pidString, out pid)) {
                    userSpace.Stderr.WriteLine($"kill: Not a valid PID number: {pidString}");
                    return 2;
                }

                pids.Add(pid);
            }

            bool success = true;

            foreach (int pid in pids) {
                try {
                    userSpace.Api.KillProcess(pid, signal);
                } catch (System.Exception e) {
                    userSpace.Stderr.WriteLine(e.Message);
                    userSpace.Stderr.WriteLine($"kill: Failed to kill process: {pid}");
                    success = false;
                }
            }

            if (success) {
                return 0;
            }
            return 64;
        }
    }
}