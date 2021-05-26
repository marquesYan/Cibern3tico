using System.Collections;
using System.Collections.Generic;

namespace Linux.FileSystem
{
    public enum Perms {
        W = 0b_0000_0100,
        R = 0b_0000_0010,
        X = 0b_0000_0001,

        NONE = 0b_0000_0000,

        ALL = Perms.W | Perms.R | Perms.X,
        RX = Perms.R | Perms.X,
        RW = Perms.R | Perms.W,
    }

    public abstract class AbstractFile {
        protected AbstractFile(string absolute_path, Perms[] permissions) {
            Path = absolute_path;
            Name = System.IO.Path.GetFileName(absolute_path);
            Permissions = permissions;
        }

        public bool IsDirectory() {
            return false;
        }

        public bool IsHidden() {
            return Name.StartsWith(".");
        }

        public string Name { get; set; }

        public string Path { get; set; }

        public Perms[] Permissions { get; set; }

        public List<AbstractFile> Childs { get; set; }

        public AbstractFile Parent { get; set; }

        abstract public int Write(string[] data);
        abstract public string Read();
        abstract public int Execute(string[] arguments);
    }

    public class LinuxDirectory : AbstractFile {
        class FileIsADirectoryException : System.Exception {
            public FileIsADirectoryException(string message) : base(message) { }
        }

        public LinuxDirectory(string path, Perms[] permissions) : base(path, permissions) {
            Childs = new List<AbstractFile>();
        }

        public override int Write(string[] data) {
            ThrowIsDir();
            return -1;
        }

        public override string Read() {
            ThrowIsDir();
            return "";
        }

        public override int Execute(string[] arguments) {
            ThrowIsDir();
            return -1;
        }

        public bool IsDirectory() {
            return true;
        }

        void ThrowIsDir() {
            throw new FileIsADirectoryException("Is a directory");
        }
    }
}