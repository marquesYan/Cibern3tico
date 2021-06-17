using System.Collections.Generic;
using Linux.Configuration;
using Linux.Sys.RunTime;
using Linux.Sys.IO;
using Linux.PseudoTerminal;
using Linux.Sys.Input.Drivers.Tty;
using Linux.FileSystem;
using Linux.Library.ArgumentParser;
using UnityEngine;

namespace Linux.Library
{    
    public class Login : CompiledBin {
        public Login(
            string absolutePath,
            int uid,
            int gid,
            int permission,
            FileType type
        ) : base(absolutePath, uid, gid, permission, type) { }

        public override int Execute(UserSpace userSpace) {
            bool eventSet = true;

            userSpace.Api.Trap(
                ProcessSignal.SIGTERM,
                (int[] args) => {
                    eventSet = false;
                }
            );

            var parser = new GenericArgParser(
                userSpace,
                "Usage: {0}",
                "Begin session on the system"
            );

            parser.Parse();

            string login = null;
            string password = null;
            bool loggedOut = true;

            while (eventSet) {
                userSpace.Print(userSpace.Api.Name());

                userSpace.Print("");

                while (eventSet && loggedOut) {
                    login = userSpace.Input("Login: ", "");

                    password = userSpace.Input("Password: ", "");

                    loggedOut = !userSpace.Api.CheckLogin(login, password);
                }

                if (loggedOut || login == null || password == null) {
                    continue;
                }

                int pid = userSpace.Api.RunLogin(login, password);
                userSpace.Api.WaitPid(pid);

                // Ensure process is killed
                userSpace.Api.KillProcess(pid);

                if (userSpace.Api.IsTtyControlled()) {
                    var pts = (IoctlDevice)userSpace.Stdin;

                    // Above operations clear the screen

                    string[] clearArray = new string[] { 
                        CharacterControl.C_DCTRL
                    };
                    
                    // Press Ctrl
                    pts.Ioctl(
                        PtyIoctl.TIO_SEND_KEY,
                        ref clearArray
                    );

                    clearArray = new string[] { "l" };
                    
                    // Send L
                    pts.Ioctl(
                        PtyIoctl.TIO_SEND_KEY,
                        ref clearArray
                    );

                    clearArray = new string[] {
                        CharacterControl.C_UCTRL
                    };
                    
                    // Unpress Ctrl
                    pts.Ioctl(
                        PtyIoctl.TIO_SEND_KEY,
                        ref clearArray
                    );
                }
            }

            return 0;
        }
    }
}