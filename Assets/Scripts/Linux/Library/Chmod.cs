using System.Collections.Generic;
using Linux.Sys.RunTime;
using Linux.IO;
using Linux.FileSystem;

namespace Linux.Library
{    
    public class Chmod : CompiledBin {
        public Chmod(
            string absolutePath,
            int uid,
            int gid,
            int permission,
            FileType type
        ) : base(absolutePath, uid, gid, permission, type) { }

        public override int Execute(UserSpace userSpace) {
            var parser = new ArgumentParser.GenericArgParser(
                userSpace,
                "Usage: {0} [OPTION]... OCTAL-MODE [FILE]...",
                "Change the mode of each FILE to OCTAL-MODE"
            );

            List<string> arguments = parser.Parse();

            if (arguments.Count < 2) {
                parser.ShowHelpInfo();
                return 1;
            }

            string mode = arguments[0];
            if (mode.Length < 3) {
                userSpace.Stderr.WriteLine($"chmod: mode should have at least 3 digits: {mode}");
                return 1;
            }

            int stickyBit = 0;
            int owner, group, other;
            bool parsedMode = true;

            if (!int.TryParse($"{mode[mode.Length - 1]}", out other)) {
                parsedMode = false;
            }

            if (!int.TryParse($"{mode[mode.Length - 2]}", out group)) {
                parsedMode = false;
            }

            if (!int.TryParse($"{mode[mode.Length - 3]}", out owner)) {
                parsedMode = false;
            }

            if (mode.Length == 4) {
                if (!int.TryParse($"{mode[mode.Length - 4]}", out stickyBit)) {
                    parsedMode = false;
                }
            }

            if (!parsedMode) {
                userSpace.Stderr.WriteLine($"chmod: unknow octal mode: {mode}");
                return 2;
            }

            int permission = Perm.FromInt(stickyBit, owner, group, other);

            string file = userSpace.ResolvePath(arguments[1]);

            userSpace.Api.ChangeFilePermission(file, permission);

            return 0;
        }
    }
}