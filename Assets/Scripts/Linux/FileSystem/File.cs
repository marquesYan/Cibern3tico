using System.Collections;
using System.Collections.Generic;

namespace Linux.FileSystem
{
    public abstract class AbstractFile {
        public AbstractFile(
            string absolutePath, 
            int uid, 
            int gid,
            int permission
        ) {
            Path = absolutePath;
            Name = System.IO.Path.GetFileName(absolutePath);
            Uid = uid;
            Gid = gid;
            Perm = permission;
        }

        public bool IsDirectory() {
            return false;
        }

        public bool IsHidden() {
            return Name.StartsWith(".");
        }

        public string Name { get; set; }

        public string Path { get; set; }

        public int Perm { get; set; }
        public int Uid { get; set; }
        public int Gid { get; set; }

        public List<AbstractFile> Childs { get; set; }

        public AbstractFile Parent { get; set; }

        abstract public int Write(string[] data);
        abstract public string Read();
        abstract public int Append(string[] data);
        abstract public int Execute(string[] arguments);
    }

    public class LinuxDirectory : AbstractFile {
        class FileIsADirectoryException : System.Exception {
            public FileIsADirectoryException(string message) : base(message) { }
        }

        public LinuxDirectory(
            string absolutePath, 
            int uid,
            int gid, 
            int permission
        ) : base(absolutePath, uid, gid, permission) {
            Childs = new List<AbstractFile>();
        }

        public override int Write(string[] data) {
            ThrowIsDir();
            return -1;
        }

        public override int Append(string[] data) {
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