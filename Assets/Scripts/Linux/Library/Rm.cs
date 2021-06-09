using System.Collections.Generic;
using Linux.Configuration;
using Linux.Sys.RunTime;
using Linux.FileSystem;
using Linux.Library.ArgumentParser;
using UnityEngine;

namespace Linux.Library
{    
    public class Rm : CompiledBin {
        public Rm(
            string absolutePath,
            int uid,
            int gid,
            int permission,
            FileType type
        ) : base(absolutePath, uid, gid, permission, type) { }

        public override int Execute(UserSpace userSpace) {
            var parser = new GenericArgParser(
                userSpace,
                "Usage: {0} [OPTION]... [FILE]...",
                "Remove (unlink) the FILEs"
            );

            bool recursive = false;
            parser.AddArgument<string>(
                "r|recursive",
                "remove directories and their contents recursively",
                (v) => recursive = true
            );

            List<string> arguments = parser.Parse();

            if (arguments.Count < 1) {
                parser.ShowHelpInfo();
                return 1;
            }

            foreach (string path in arguments) {
                string absPath = userSpace.ResolvePath(path);

                if (recursive) {
                    RemoveChilds(userSpace, absPath);
                }

                userSpace.Api.RemoveFile(absPath);
            }
            
            return 0;
        }

        protected void RemoveChilds(UserSpace userSpace, string path) {
            ReadOnlyFile file = userSpace.Api.FindFile(path);

            if (file.Type != FileType.F_DIR) {
                return;
            }

            List<ReadOnlyFile> childs = userSpace.Api.ListDirectory(path);

            foreach (ReadOnlyFile child in childs) {
                if (child.Type == FileType.F_DIR) {
                    RemoveChilds(userSpace, child.Path);
                } else {
                    userSpace.Api.RemoveFile(child.Path);
                }
            }
        }
    }
}