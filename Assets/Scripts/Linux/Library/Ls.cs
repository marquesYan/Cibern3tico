using System.Collections.Generic;
using System.Text;
using Linux.Configuration;
using Linux.Sys.RunTime;
using Linux.FileSystem;

namespace Linux.Library {
    public class Ls : CompiledBin {
        public Ls(
            string absolutePath,
            int uid,
            int gid,
            int permission,
            FileType type
        ) : base(absolutePath, uid, gid, permission, type) { }

        public override int Execute(UserSpace userSpace) {
            var parser = new ArgumentParser.GenericArgParser(
                userSpace,
                "Usage: {0} [OPTION]... [FILE]...", 
                "List information about the FILEs"
            );

            bool all = false;
            bool detailed = false;

            parser.AddArgument<string>(
                "a|all",
                "List all directories and hidden files",
                (v) => all = true
            );

            parser.AddArgument<string>(
                "l|list",
                "List detailed information about files and directories",
                (v) => detailed = true
            );

            List<string> arguments = parser.Parse();

            string path;

            if (arguments.Count == 0) {
                path = userSpace.Api.GetCwd();
            } else {
                path = userSpace.ResolvePath(arguments[0]);
            }

            List<ReadOnlyFile> files = userSpace.Api.ListDirectory(path);

            StringBuilder buffer = new StringBuilder();

            foreach(ReadOnlyFile file in files) {
                if (!all && file.IsHidden) {
                    continue;
                }

                if (detailed) {
                    buffer.Append(BuildFileType(file.Type));

                    buffer.Append(BuildPerm(file.Permission, 8));
                    buffer.Append(BuildPerm(file.Permission, 4));
                    buffer.Append(BuildPerm(file.Permission, 0));

                    buffer.Append("  ");
                    buffer.Append("2");
                    buffer.Append("  ");

                    string userName = userSpace.Api.LookupUserLogin(file.Uid);
                    buffer.Append(userName ?? file.Uid.ToString());
                    buffer.Append("  ");

                    string groupName = userSpace.Api.LookupGroupName(file.Gid);
                    buffer.Append(groupName ?? file.Gid.ToString());
                    buffer.Append("  ");

                    buffer.Append("4096");
                    buffer.Append("  ");

                    buffer.Append(file.UpdatedAt);
                    buffer.Append("  ");

                    buffer.Append(file.Name);

                    buffer.AppendLine();
                } else {
                    buffer.Append(file.Name);
                    buffer.Append("\t");
                }
            }

            userSpace.Print(buffer.ToString());

            return 0;
        }

        string BuildFileType(FileType type) {
            switch (type) {
                case FileType.F_MNT:
                case FileType.F_DIR: return "d";
                case FileType.F_REG: return "-";
                case FileType.F_BLK: return "b";
                case FileType.F_CHR: return "c";
                case FileType.F_PIP: return "p";
                case FileType.F_SYL: return "l";
                case FileType.F_SCK: return "s";
            }

            return "?";
        }
        string BuildPerm(int permission, int shift) {
            // Apply mask to AND operation discard positions not cared
            // E.g
            // permission = 0b0111_0101_0101 (755)
            // to compare permission on the owner side, use a shift of '8'
            // and we got this mask: 0b1111_0000_0000
            //
            // aplying the AND operation with permission and mask we ignore
            // everything but the most 4 significant bits:
            //  0b0111_0101_0101 & 0b1111_0000_0000 = 0b0111_0000_0000

            int basePermission = (permission & (0b1111 << shift));

            if (basePermission == 0) {
                return "---";
            }

            if ((basePermission - (7 << shift)) == 0) {
                return "rwx";
            }

            if ((basePermission - (6 << shift)) == 0) {
                return "rw-";
            }

            if ((basePermission - (5 << shift)) == 0) {
                return "r-x";
            }

            if ((basePermission - (4 << shift)) == 0) {
                return "r--";
            }

            if ((basePermission - (3 << shift)) == 0) {
                return "-wx";
            }

            if ((basePermission - (2 << shift)) == 0) {
                return "-w-";
            }

            if ((basePermission - (1 << shift)) == 0) {
                return "--x";
            }

            return "?";
        }
    }
}