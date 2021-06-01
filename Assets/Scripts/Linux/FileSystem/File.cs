using System;
using System.Collections;
using System.Collections.Generic;
using Linux.IO;

namespace Linux.FileSystem
{
    public enum FileType {
        F_DIR,
        F_REG,
        F_BLK,
        F_CHR,
        F_PIP,
        F_SYL,
        F_SCK,
        F_MNT
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
        public File SourceFile { get; protected set; }

        protected Dictionary<string, File> Childs;

        public File(
            string absolutePath, 
            int uid,
            int gid,
            int permission,
            FileType type
        ) {
            Path = absolutePath;
            Name = PathUtils.BaseName(absolutePath);
            Uid = uid;
            Gid = gid;
            Permission = permission;
            Type = type;
            CreatedAt = UpdatedAt = DateTime.Now;

            switch (Type) {
                case FileType.F_MNT:
                case FileType.F_DIR: {
                    Childs = new Dictionary<string, File>();
                    break;
                }
            }
        }
        public File(
            string absolutePath,
            int uid,
            int gid,
            int permission
        ) : this(absolutePath, uid, gid, permission, FileType.F_REG) { }

        public File(
            File sourceFile,
            string absolutePath
        ) : this(sourceFile, absolutePath, Perm.FromInt(7, 7, 7)) { }

        public File(
            File sourceFile,
            string absolutePath,
            int permission
        ) : this(
                absolutePath,
                sourceFile.Uid,
                sourceFile.Gid,
                permission,
                FileType.F_SYL
            )
        {
            SourceFile = sourceFile;
        }

        public bool IsHidden() {
            return Name.StartsWith(".");
        }

        public void Touch() {
            UpdatedAt = DateTime.Now;
        }

        public void AddChild(File file) {
            Childs.Add(file.Name, file);
            Touch();
        }

        public int ChildsCount() {
            return Childs.Count;
        }

        public File[] ListChilds() {
            File[] childs = new File[Childs.Count];
            Childs.Values.CopyTo(childs, 0);
            return childs;
        }

        public File FindChild(string fileName) {
            File result;

            if (Childs.TryGetValue(fileName, out result)) {
                return result;
            }

            return null;
        }

        public void RemoveChild(string fileName) {
            Childs.Remove(fileName);
            Touch();
        }
    }
}