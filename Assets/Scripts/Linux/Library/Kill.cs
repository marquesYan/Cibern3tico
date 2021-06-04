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
            string[] args = userSpace.Api.GetArgs();

            ProcessSignal signal = ProcessSignal.SIGTERM;
            bool showHelp = false;

            var parser = new OptionSet() {
                {
                    "n|signal=", 
                    "The {SIGNAL} number to send.",
                    (int signalNum) => signal = (ProcessSignal)signalNum
                },
                {
                    "h|help", 
                    "Show help message.",
                    _ => showHelp = true
                }
            };
            
            List<string> arguments;

            System.Action showHelpInfo = () => {
                userSpace.Stderr.WriteLine("Try kill --help for more information");
            };

            try {
                arguments = parser.Parse(args);
            } catch (OptionException exc) {
                userSpace.Stderr.WriteLine($"kill: {exc.Message}");
                showHelpInfo();
                return 1;
            }

            if (showHelp) {
                ShowHelpMsg(userSpace, parser);
                return 1;
            }

            arguments.RemoveAt(0);

            if (arguments.Count < 1) {
                showHelpInfo();
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
            Debug.Log("using signal: " + (ushort)signal);
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

        protected void ShowHelpMsg(UserSpace userSpace, OptionSet options) {
            userSpace.Stderr.WriteLine("Usage: kill [-n] PID");
            userSpace.Stderr.WriteLine("\tSend a signal to a job");
            userSpace.Stderr.WriteLine("");
            options.WriteOptionDescriptions(userSpace.Stderr);
        }
    }
}