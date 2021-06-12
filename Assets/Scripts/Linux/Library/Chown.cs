using System.Collections.Generic;
using Linux.Sys.RunTime;
using Linux.IO;
using Linux.FileSystem;

namespace Linux.Library
{    
    public class Chown : CompiledBin {
        public Chown(
            string absolutePath,
            int uid,
            int gid,
            int permission,
            FileType type
        ) : base(absolutePath, uid, gid, permission, type) { }

        public override int Execute(UserSpace userSpace) {
            var parser = new ArgumentParser.GenericArgParser(
                userSpace,
                "Usage: {0} [OPTION]... USER[:GROUP] [FILE]...",
                "Change the owner and/or group of each FILE to OWNER and/or GROUP"
            );

            bool recursive = false;
            parser.AddArgument<string>(
                "R|recursive",
                "operate on file and directories recursively",
                (v) => recursive = true
            );

            List<string> arguments = parser.Parse();

            if (arguments.Count < 2) {
                parser.ShowHelpInfo();
                return 1;
            }

            string user;
            string group = null;

            string[] userNgroup = arguments[0].Split(':');

            if (!(userNgroup.Length == 1 || userNgroup.Length == 2)) {
                userSpace.Stderr.WriteLine("Invalid format: " + arguments[0]);
                return 2;
            }

            user = userNgroup[0];

            if (userNgroup.Length == 2) {
                group = userNgroup[1];
            }

            int uid;
            int gid = -1;

            if (!int.TryParse(user, out uid)) {
                uid = userSpace.Api.LookupUserUid(user);
            }

            if (group != null && !int.TryParse(group, out gid)) {
                gid = userSpace.Api.LookupGroupGid(group);
            }
            
            arguments.RemoveAt(0);

            arguments.ForEach(
                file => {
                    string path = userSpace.ResolvePath(file);

                    userSpace.Api.ChangeFileOwner(path, uid);

                    if (gid != -1) {
                        userSpace.Api.ChangeFileGroup(path, gid);
                    }
                }
            );

            return 0;
        }
    }
}