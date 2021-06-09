using System.Collections.Generic;
using Linux.Sys.RunTime;
using Linux.FileSystem;
using Linux.IO;
using UnityEngine;

namespace Linux.Library
{    
    public class Tee : CompiledBin {
        public Tee(
            string absolutePath,
            int uid,
            int gid,
            int permission,
            FileType type
        ) : base(absolutePath, uid, gid, permission, type) { }

        public override int Execute(UserSpace userSpace) {
            var parser = new ArgumentParser.GenericArgParser(
                userSpace,
                "Usage: {0} [OPTION]... [DATA] [FILE]", 
                "Copy standard input to each FILE, and also to standard output."
            );

            bool append = false;

            parser.AddArgument<string>(
                "a|append",
                "append to the given FILEs, do not overwrite",
                (v) => append = true
            );

            List<string> arguments = parser.Parse();

            if (arguments.Count < 2) {
                parser.ShowHelpInfo();
                return 1;
            }

            string data = arguments[0];
            string file = userSpace.ResolvePath(arguments[1]);

            int mode = append ? AccessMode.O_APONLY : AccessMode.O_WRONLY;

            using (ITextIO stream = userSpace.Open(file, mode)) {
                stream.WriteLine(data);
            }

            return 0;
        }
    }
}