using System;
using System.Collections;
using System.Collections.Generic;

namespace Linux.FileSystem
{
    public enum FileType {
        F_DIR,
        F_REG,
        F_BLK,
        F_CHR,
        F_PIP,
        F_SYL,
        S_SCK
    }

    public class File {
        public string Name { get; protected set; }

        public string Path { get; protected set; }
        public DateTime CreatedAt { get; protected set; }
        public DateTime UpdatedAt { get; protected set; }
        public FileType Type { get; protected set; }
        public int Permission { get; set; }
        public int Uid { get; set; }
        public int Gid { get; set; }

        public File Parent { get; set; }

        public File(
            string absolutePath, 
            int uid,
            int gid,
            int permission,
            FileType type
        ) {
            Path = absolutePath;
            Name = System.IO.Path.GetFileName(absolutePath);
            Uid = uid;
            Gid = gid;
            Permission = permission;
            Type = type;
            CreatedAt = UpdatedAt = DateTime.Now;
        }
        public File(
            string absolutePath,
            int uid,
            int gid,
            int permission
        ) : this(absolutePath, uid, gid, permission, FileType.F_REG) { }

        public bool IsHidden() {
            return Name.StartsWith(".");
        }

        public void Touch() {
            UpdatedAt = DateTime.Now;
        }
    }

    public class Directory : File {
        List<File> Childs = new List<File>();

        public Directory(
            string absolutePath,
            int uid,
            int gid,
            int permission
        ) : base(absolutePath, uid, gid, permission, FileType.F_DIR) { }

        public void Add(File file) {
            Childs.Add(file);
            Touch();
        }

        public int ChildsCount() {
            return Childs.Count;
        }

        public File[] GetChilds() {
            return Childs.ToArray();
        }

        public File Find(Predicate<File> action) {
            return Childs.Find(action);
        }

        public void Remove(File file) {
            Childs.Remove(file);
            Touch();
        }
    }

    public class SymbolicLink : File {
        public File SourceFile { get; protected set; }

        public SymbolicLink(
            File sourceFile,
            string absolutePath
        ) : this(sourceFile, absolutePath, Perm.FromInt(7, 7, 7)) { }

        public SymbolicLink(
            File sourceFile,
            string absolutePath,
            int permission
        ) : base(
                absolutePath,
                sourceFile.Uid,
                sourceFile.Gid,
                permission,
                FileType.F_SYL
            ) {
            SourceFile = sourceFile;
        }

        public SymbolicLink(
            string absolutePath, 
            int uid,
            int gid,
            int permission,
            FileType type
        ) : base(null, 0, 0, 0, FileType.F_REG) {
            throw new ArgumentException("Symbolic links requires a source file");
        }
    }
}