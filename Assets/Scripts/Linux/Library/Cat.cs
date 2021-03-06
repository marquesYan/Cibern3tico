using System.Collections.Generic;
using Linux.Configuration;
using Linux.Sys.RunTime;
using Linux.IO;
using Linux.Sys.IO;
using Linux.FileSystem;

namespace Linux.Library
{    
    public class Cat : CompiledBin {
        public Cat(
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

            var parser = new ArgumentParser.GenericArgParser(
                userSpace,
                "Usage: {0} [FILE]...",
                "Concatenate FILEs and print on the standard output"
            );

            List<string> arguments = parser.Parse();

            if (arguments.Count < 1) {
                parser.ShowHelpInfo();
                return 1;
            }

            string path = userSpace.ResolvePath(arguments[0]);

            ITextIO stream = userSpace.Open(path, AccessMode.O_RDONLY);

            if (stream is CharacterDevice) {
                while (eventSet) {
                    userSpace.Print(stream.Read(1), "");
                }
            } else {
                userSpace.Print(stream.Read(), "");
            }

            return 0;
        }
    }
}