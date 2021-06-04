using System;
using System.Threading;
using Linux.Sys.RunTime;
using Linux.Configuration;
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
                userSpace.Print($"Usage: {args[0]} ptsFd inputQueue outputQueue");
                return 1;
            }

            bool eventIsSet = true;

            userSpace.Api.Trap(
                ProcessSignal.SIGTERM,
                (int[] args) => {
                    eventIsSet = false;
                }
            );

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
            IoctlDevice wStream = (IoctlDevice)userSpace.Open(outputQueue, AccessMode.O_WRONLY);

            Debug.Log("getting pts file descriptor: " + ptsFd);
            CharacterDevice ptStream = (CharacterDevice)userSpace.Api.LookupByFD(ptsFd);

            new Thread(new ThreadStart(() => {
                while (eventIsSet) {
                    string data = ptStream.Read(1);
                    
                    try {
                        Debug.Log("recv from pts: " + TextUtils.ToByte(data));
                    } catch {
                        Debug.Log("recv from pts raw: " + data);
                    }

                    wStream.Write(data);
                }
            })).Start();

            Thread.Sleep(200);

            PtyLineDiscipline lineDiscipline = new PtyLineDiscipline(
                ptStream,
                wStream
            );

            while (eventIsSet) {
                string key = rStream.Read(1);

                lineDiscipline.Receive(key);
            }

            return 0;
        }

        // protected void RunTimeout(Action action, int timeout) {
        //     var timer = new Timer();
        //     timer.Interval = timeout;

        //     timer.Start();

        //     timer.Elapsed += (sender, e) => {
        //         action.EndInvoke(null);
        //         throw new TimeoutException();
        //     };

        //     action.BeginInvoke(null, null);
        // }
    }
}