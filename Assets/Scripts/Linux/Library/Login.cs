using System.Collections.Generic;
using Linux.Configuration;
using Linux.Sys.RunTime;
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

            userSpace.Print(userSpace.Api.Name());

            userSpace.Print("");

            while (eventSet && loggedOut) {
                userSpace.Print("Login: ", "");
                login = userSpace.Stdin.ReadLine();

                userSpace.Print("Password: ", "");
                password = userSpace.Stdin.ReadLine();

                loggedOut = !userSpace.Api.CheckLogin(login, password);
            }

            if (login == null || password == null) {
                return 128;
            }

            if (loggedOut) {
                return 1;
            }

            int pid = userSpace.Api.RunLogin(login, password);
            return userSpace.Api.WaitPid(pid);
        }
    }
}