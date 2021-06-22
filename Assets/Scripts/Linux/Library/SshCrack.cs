using System.Collections.Generic;
using Linux.Configuration;
using Linux.Sys.RunTime;
using Linux.FileSystem;
using Linux.IO;
using Linux.Library.ArgumentParser;
using UnityEngine;

namespace Linux.Library
{    
    public class SshCrack : CompiledBin {
        public SshCrack(
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
                "Usage: {0} [-w WORDLIST] URL",
                "Crack ssh passwords using a wordlist"
            );

            string wordlist = null;
            parser.AddArgument<string>(
                "w|wordlist=",
                "Dictionary of passwords. Required argument",
                (string path) => wordlist = path
            );

            List<string> arguments = parser.Parse();

            if (arguments.Count < 1) {
                parser.ShowHelpInfo();
                return 1;
            }

            if (wordlist == null) {
                userSpace.Stderr.WriteLine("sshcrack: Missing wordlist argument");
                return 2;
            }

            string wordlistPath = userSpace.ResolvePath(wordlist);

            string url = arguments[0];

            // Open the stream to pass in the ssh process 
            var buffer = new BufferedStream(AccessMode.O_RDWR);
            int fd = userSpace.Api.OpenStream(buffer);

            int pid, retCode;

            int devNull = userSpace.Api.Open("/dev/null", AccessMode.O_WRONLY);

            using (ITextIO stream = userSpace.Open(wordlistPath, AccessMode.O_RDONLY)) {
                foreach (string password in stream.ReadLines()) {
                    if (string.IsNullOrEmpty(password)) {
                        continue;
                    }

                    if (!eventSet) {
                        return 5;
                    }

                    buffer.WriteLine(password);

                    pid = userSpace.Api.StartProcess(
                        new string[] {
                            "/usr/bin/ssh",
                            url,
                            "true"
                        },
                        fd,
                        devNull,
                        devNull
                    );

                    retCode = userSpace.Api.WaitPid(pid);

                    if (retCode == 0) {
                        userSpace.Print($"[+] found password: {password}");
                        return 0;
                    }
                }
            }

            userSpace.Print("[-] password not in wordlist");
            
            return 3;
        }
    }
}