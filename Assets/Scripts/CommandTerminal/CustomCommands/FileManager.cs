using System.Text;
using System.Collections;
using System.Collections.Generic;
using Linux.FileSystem;
using UnityEngine;

namespace CommandTerminal {
    public static class FileManager {
        public static AbstractFile current_dir;

        // [RegisterCommand(Help = "List all files in current directory", MaxArgCount = 2)]
        // static void CommandLs(CommandArg[] args) {
        //     bool all = false;
        //     bool detailed = false;

        //     foreach(CommandArg argument in args) {
        //         if (argument.String.Contains("l")) {
        //             detailed = true;
        //         }

        //         if (argument.String.Contains("a")) {
        //             all = true;
        //         }
        //     }

        //     AbstractFile current_file;
        //     current_file = current_dir;

        //     if (args.Length > 0) {
        //         string last_argument = args[args.Length - 1].String;

        //         if (! last_argument.StartsWith("-")) {
        //             current_file = Terminal.Fs.Lookup(last_argument);
        //             Debug.Log("directory: " + current_file);
        //         }
        //     }

        //     List<AbstractFile> files_to_display = new List<AbstractFile>();

        //     if (all) {
        //         files_to_display.Add(current_file.Parent);
        //         files_to_display.Add(current_file.Parent.Parent);
        //     }
        //     Debug.Log(current_file.Name);
        //     if (current_file.IsDirectory()) {
        //         foreach (AbstractFile file in current_file.Childs) {
        //             Debug.Log("adding file");
        //             files_to_display.Add(file);
        //         }
        //     } else {
        //         Debug.Log("file is not a directory");
        //         files_to_display.Add(current_file);
        //     }

        //     StringBuilder sb = new StringBuilder();

        //     foreach(AbstractFile file in files_to_display) {
        //         if (!all && file.IsHidden()) {
        //             continue;
        //         }
        //         Debug.Log(file);
        //         if (detailed) {
        //             if (file.IsDirectory()) {
        //                 sb.Append('d');
        //             } else {
        //                 sb.Append('-');
        //             }

        //             sb.Append(BuildPerm(file.Permissions[0]));
        //             sb.Append(BuildPerm(file.Permissions[1]));
        //             sb.Append(BuildPerm(file.Permissions[2]));

        //             sb.Append(" 2 user user 4096 May 25 16:58 ");

        //             sb.Append(file.Name);

        //             sb.Append("\n");
        //         } else {
        //             sb.Append(file.Name);
        //             sb.Append("\t");
        //         }
        //     }

        //     // if (detailed) {
        //     //     sb.Remove(sb.Length - 1, 1);
        //     // }

        //     Terminal.Log(sb.ToString());
        // }

        // [RegisterCommand(Help = "Show current working directory", MaxArgCount = 1)]
        // static void CommandPwd(CommandArg[] args) {
        //     Terminal.Log(current_dir.FullyQualifiedName());
        // }

        // static string BuildPerm(Perms permission) {
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