using System.Collections.Generic;
using Linux.FileSystem;

namespace Linux
{
    public class Shell {
        VirtualFileTree Fs { get; set; }

        public File Cwd { get; private set; }

        public Dictionary<string, string> Env { get; set; }

        public File Stdout { get; private set; }
        public File Stderr { get; private set; }

        public Shell(VirtualFileTree fs) {
            Fs = fs;
            Cwd = Fs.Root;

            Stdout = Fs.Lookup("/dev/tty");
            Stderr = Fs.Lookup("/dev/tty");

            CreateDefaultEnvironment();
        }

        public int RunCommand(string command) {
            // if (command.Length == 0) {
            //     throw new System.ArgumentException("Command must not be empty.");
            // }

            // Queue<string> input = new Queue<string>(command.Split(' '));

            // string fileName = input.Dequeue();

            // File fileCmd = FindCommand(fileName);

            // if (fileCmd == null) {
            //     Stdout.Write(new string[] { $"sh: {fileName}: command not found" });
            //     return 127;
            // }

            // return fileCmd.Execute();
            return 0;
        }

        void CreateDefaultEnvironment() {
            Env = new Dictionary<string, string>(){
                { "PATH", "/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin" }
            };
        }

        public File FindCommand(string fileName) {
            // string path_env = Env["PATH"];
            
            // foreach (string path in path_env?.Split(':')) {
            //     File path_dir = Fs.Lookup(path);

            //     if (path_dir != null && path_dir.IsDirectory()) {
            //         File file_cmd = path_dir.Childs.Find(file => file.Name == fileName);

            //         if (file_cmd != null) {
            //             return file_cmd;
            //         }
            //     }
            // }

            return null;
        }
    }
}