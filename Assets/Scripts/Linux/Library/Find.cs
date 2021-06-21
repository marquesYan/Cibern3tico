using System.Collections.Generic;
using System.Text;
using Linux.Configuration;
using Linux.Sys.RunTime;
using Linux.FileSystem;

namespace Linux.Library {
    public class Find : CompiledBin {
        public Find(
            string absolutePath,
            int uid,
            int gid,
            int permission,
            FileType type
        ) : base(absolutePath, uid, gid, permission, type) { }

        public override int Execute(UserSpace userSpace) {
            var parser = new ArgumentParser.GenericArgParser(
                userSpace,
                "Usage: {0} PATH [OPTION]...", 
                "Find files recursively at PATH"
            );

            string permStr = null;

            parser.AddArgument<string>( 
                "perm=",
                "Filter files by this permission mask",
                (perm) => permStr = perm
            );

            List<string> arguments = parser.Parse();

            int permMask = -1;

            if (permStr != null) {
                permMask = Perm.FromString(permStr);

                if (permMask == -1) {
                    userSpace.Stderr.WriteLine("find: Permission must be a number");
                    return 1;
                }
            }

            if (arguments.Count < 1) {
                parser.ShowHelpInfo();
                return 2;
            }

            string path = userSpace.ResolvePath(arguments[0]);

            FindFiles(userSpace, path, permMask);

            return 0;
        }

        void FindFiles(UserSpace userSpace, string path, int permMask) {
            List<ReadOnlyFile> files;

            try {
                files = userSpace.Api.ListDirectory(path);
            } catch (System.Exception e) {
                return;
            }

            foreach(ReadOnlyFile file in files) {
                if (file.Type == FileType.F_DIR) {
                    FindFiles(userSpace, file.Path, permMask);
                } else {
                    if ((permMask == -1) || ((file.Permission & permMask) != 0)) {
                        userSpace.Print(file.Path);
                    }
                }
            }
        }
    }
}