using System.Collections.Generic;
using System.Text;
using Linux.Configuration;
using Linux.Sys.RunTime;
using Linux.FileSystem;
using UnityEngine;

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

            List<string> arguments = parser.Parse();

            string path;

            if (arguments.Count == 0) {
                path = userSpace.Api.GetCwd();
            } else {
                path = arguments[0];
            }

            List<ReadOnlyFile> files = userSpace.Api.ListDirectory(path);

            StringBuilder buffer = new StringBuilder();

            foreach (ReadOnlyFile file in files) {
                buffer.AppendFormat("{0}\t", file.Name);
            }

            userSpace.Print(buffer.ToString());

            // File current_file;
            // current_file = current_dir;

            // if (args.Length > 0) {
            //     string last_argument = args[args.Length - 1].String;

            //     if (! last_argument.StartsWith("-")) {
            //         current_file = Terminal.Fs.Lookup(last_argument);
            //         Debug.Log("directory: " + current_file);
            //     }
            // }

            // List<File> files_to_display = new List<File>();

            // if (all) {
            //     files_to_display.Add(current_file.Parent);
            //     files_to_display.Add(current_file.Parent.Parent);
            // }
            // Debug.Log(current_file.Name);
            // if (current_file.IsDirectory()) {
            //     foreach (File file in current_file.Childs) {
            //         Debug.Log("adding file");
            //         files_to_display.Add(file);
            //     }
            // } else {
            //     Debug.Log("file is not a directory");
            //     files_to_display.Add(current_file);
            // }

            // StringBuilder sb = new StringBuilder();

            // foreach(File file in files_to_display) {
            //     if (!all && file.IsHidden()) {
            //         continue;
            //     }
            //     Debug.Log(file);
            //     if (detailed) {
            //         if (file.IsDirectory()) {
            //             sb.Append('d');
            //         } else {
            //             sb.Append('-');
            //         }

            //         sb.Append(BuildPerm(file.Permissions[0]));
            //         sb.Append(BuildPerm(file.Permissions[1]));
            //         sb.Append(BuildPerm(file.Permissions[2]));

            //         sb.Append(" 2 user user 4096 May 25 16:58 ");

            //         sb.Append(file.Name);

            //         sb.Append("\n");
            //     } else {
            //         sb.Append(file.Name);
            //         sb.Append("\t");
            //     }
            // }

            // // if (detailed) {
            // //     sb.Remove(sb.Length - 1, 1);
            // // }

            // Terminal.Log(sb.ToString());
            return 0;
        }

        // string BuildPerm(Perms permission) {
        //     switch(permission) {
        //         case Perms.NONE: return "---";
        //         case Perms.ALL: return "rwx";
        //         case Perms.R: return "r--";
        //         case Perms.RX: return "r-x";
        //         case Perms.RW: return "rw-";
        //         case Perms.W | Perms.X: return "-wx";
        //         case Perms.W: return "-w-";
        //         case Perms.X: return "--x";
        //     }

        //     return null;
        // }
    }
}