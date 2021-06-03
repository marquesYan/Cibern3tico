using System;
using System.Timers;
using Linux.Sys.RunTime;
using Linux.FileSystem;
using Linux.PseudoTerminal;
using Linux;
using Linux.IO;
using Linux.Sys.IO;
using Linux.Sys.Input.Drivers.Tty;
using UnityEngine;

namespace Linux.Library
{    
    public class TtyCtl : CompiledBin {
        public TtyCtl(
            string absolutePath,
            int uid,
            int gid,
            int permission,
            FileType type
        ) : base(absolutePath, uid, gid, permission, type) { }

        public override int Execute(UserSpace userSpace) {
            string[] args = userSpace.Api.GetArgs();

            if (args.Length < 4) {
                Debug.Log("invalid command");
                userSpace.Print($"Usage: {args[0]} ptsFd inputQueue outputQueue");
                return 1;
            }

            Debug.Log("ttyctl running");

            string inputQueue = args[2];
            string outputQueue = args[3];

            int ptsFd;

            if (!int.TryParse(args[1], out ptsFd)) {
                userSpace.Print($"ERROR: ptsFd must be an integer");
                return 2;
            }

            Debug.Log("opening input queue: " + inputQueue);
            ITextIO rStream = userSpace.Open(inputQueue, AccessMode.O_RDONLY);

            Debug.Log("opening output queue: " + outputQueue);
            ITextIO wStream = userSpace.Open(outputQueue, AccessMode.O_WRONLY);

            Debug.Log("getting pts file descriptor: " + ptsFd);
            CharacterDevice ptStream = (CharacterDevice)userSpace.Api.LookupByFD(ptsFd);

            PtyLineDiscipline lineDiscipline = new PtyLineDiscipline(ptStream);

            while (!userSpace.IsShutdown) {
                // Debug.Log("waiting user keyboard...");
                string key = rStream.Read(1);

                string output = lineDiscipline.Receive(key);

                if (output == null) {
                    Debug.Log("line discipline says no echo");
                } else {
                    // Debug.Log("recv input from queue: " + key);
                    wStream.Write(key);
                }
            }

            return 0;
        }

        protected void RunTimeout(Action action, int timeout) {
            var timer = new Timer();
            timer.Interval = timeout;

            timer.Start();

            timer.Elapsed += (sender, e) => {
                action.EndInvoke(null);
                throw new TimeoutException();
            };

            action.BeginInvoke(null, null);
        }
    }
}