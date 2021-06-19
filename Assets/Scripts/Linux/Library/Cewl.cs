using System;
using System.Collections.Generic;
using Linux.Sys.RunTime;
using Linux.IO;
using Linux.FileSystem;

namespace Linux.Library
{    
    public class Cewl : CompiledBin {
        public Cewl(
            string absolutePath,
            int uid,
            int gid,
            int permission,
            FileType type
        ) : base(absolutePath, uid, gid, permission, type) { }

        public override int Execute(UserSpace userSpace) {
            var parser = new ArgumentParser.GenericArgParser(
                userSpace,
                "Usage: {0} URL",
                "Fetch webpage and create a dictionary with it's contents"
            );

            List<string> arguments = parser.Parse();

            if (arguments.Count < 1) {
                parser.ShowHelpInfo();
                return 1;
            }

            string url = arguments[0];

            using (ITextIO stream = new BufferedStream(AccessMode.O_RDWR)) {
                int fd = userSpace.Api.OpenStream(stream);

                int pid = userSpace.Api.StartProcess(
                    new string[] {
                        "/usr/bin/curl",
                        url
                    },
                    0,
                    fd,
                    2
                );

                int exitCode = userSpace.Api.WaitPid(pid);

                if (exitCode != 0) {
                    userSpace.Stderr.WriteLine("cewl: Http request failed");
                    return exitCode;
                }

                string response = stream.Read();

                if (string.IsNullOrEmpty(response)) {
                    userSpace.Stderr.WriteLine("cewl: Empty http response");
                    return 2;
                }

                string[] lines = response.Split(
                    new char[] { ' ' },
                    StringSplitOptions.RemoveEmptyEntries
                );

                foreach (string line in lines) {
                    string parsedLine = line.Trim(
                        '\n',
                        '\t',
                        ' ',
                        '.',
                        ',',
                        ';',
                        '(', ')',
                        '/',
                        '?',
                        ':'
                    );

                    if (!string.IsNullOrEmpty(parsedLine)) {
                        userSpace.Print(parsedLine);
                    }
                }
            }

            return 0;
        }
    }
}