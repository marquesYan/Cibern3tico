using System.IO;
using System.Collections.Generic;
using Linux.FileSystem;

namespace Linux
{
    public class Shell {
        FileTree Fs { get; set; }

        public LinuxDirectory Cwd { get; private set; }

        public Dictionary<string, string> Env { get; set; }

        public AbstractFile Stdout { get; private set; }
        public AbstractFile Stderr { get; private set; }

        public Shell(FileTree fs) {
            Fs = fs;
            Cwd = (LinuxDirectory) Fs.Lookup("/");

            Stdout = Fs.Lookup("/dev/tty");
            Stderr = Fs.Lookup("/dev/tty");

            CreateDefaultEnvironment();
        }

        public int RunCommand(string command) {
            if (command.Length == 0) {
                throw new System.ArgumentException("Command must not be empty.");
            }

            Queue<string> input = new Queue<string>(command.Split(' '));

            string fileName = input.Dequeue();

            AbstractFile fileCmd = FindCommand(fileName);

            if (fileCmd == null) {
                Stdout.Write(new string[] { $"sh: {fileName}: command not found" });
                return 127;
            }

            return fileCmd.Execute(input.ToArray());
        }

        void CreateDefaultEnvironment() {
            Env = new Dictionary<string, string>(){
                { "PATH", "/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin" }
            };
        }

        public AbstractFile FindCommand(string fileName) {
            string path_env = Env["PATH"];
            
            foreach (string path in path_env?.Split(':')) {
                AbstractFile path_dir = Fs.Lookup(path);

                if (path_dir != null && path_dir.IsDirectory()) {
                    AbstractFile file_cmd = path_dir.Childs.Find(file => file.Name == fileName);

                    if (file_cmd != null) {
                        return file_cmd;
                    }
                }
            }

            return null;
        }
    }
}