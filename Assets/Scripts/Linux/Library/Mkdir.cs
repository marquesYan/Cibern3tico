using System.Collections.Generic;
using Linux.Configuration;
using Linux.Sys.RunTime;
using Linux.FileSystem;
using Linux.Library.ArgumentParser;
using UnityEngine;

namespace Linux.Library
{    
    public class Mkdir : CompiledBin {
        public Mkdir(
            string absolutePath,
            int uid,
            int gid,
            int permission,
            FileType type
        ) : base(absolutePath, uid, gid, permission, type) { }

        public override int Execute(UserSpace userSpace) {
            var parser = new GenericArgParser(
                userSpace,
                "Usage: {0} [OPTION]... DIRECTORY...",
                "Create the DIRECTORY(ies), if they do not already exist."
            );

            bool parents = false;
            parser.AddArgument<string>(
                "p|parents",
                "no error if existing, make parent directories as needed",
                (v) => parents = true
            );

            List<string> arguments = parser.Parse();

            if (arguments.Count < 1) {
                parser.ShowHelpInfo();
                return 1;
            }

            foreach (string path in arguments) {
                userSpace.Api.CreateDir(
                    userSpace.ResolvePath(path)
                );
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