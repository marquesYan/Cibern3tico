using System.Collections.Generic;
using Linux.IO;

namespace Linux.FileSystem
{
    public class FileExistsException : System.InvalidOperationException {
        public FileExistsException(string file) : base("File exists: " + file) {}
    }

    public class FsMountPoint {
        public readonly File MountPoint;
        public readonly AbstractFileTree Fs;

        public FsMountPoint(File mountPoint, AbstractFileTree fs) {
            MountPoint = mountPoint;
            Fs = fs;
        }
    }

    public abstract class AbstractFileTree {
        protected List<FsMountPoint> Mounts;

        public File Root { get; protected set; }

        public AbstractFileTree(File root) {
            EnsureIsDirectory(root);

            Root = root;

            Root.Parent = Root;

            Mounts = new List<FsMountPoint>();
        }

        protected abstract ITextIO OpenFileHandler(File file, int mode);
        protected abstract void OnAddedFile(File file, ITextIO stream);
        protected abstract void OnRemoveFile(File file);

        public void Mount(
            File mountPoint,
            AbstractFileTree fs
        ) {
            if (mountPoint.Type != FileType.F_MNT) {
                throw new System.ArgumentException(
                    "File not a mount point"
                );
            }

            if (Lookup(mountPoint.Path) != null) {
                throw new System.InvalidOperationException(
                    "already mounted"
                );
            }

            InternalAdd(Root, mountPoint, null);

            Mounts.Add(
                new FsMountPoint(mountPoint, fs)
            );
        }

        public ITextIO Open(string filePath, int mode) {
            File file = LookupOrFail(filePath);

            if ((file.Type == FileType.F_DIR) || 
                (file.Type == FileType.F_MNT)) {
                throw new System.ArgumentException(
                    "File is a directory"
                );
            }

            if (file.Type == FileType.F_SYL) {
                return OpenFileHandler(file.SourceFile, mode);
            }

            return OpenFileHandler(file, mode);
        }

        public void Add(File file) {
            AddFrom(Root, file, null);
        }

        public void Add(File file, ITextIO stream) {
            AddFrom(Root, file, stream);
        }

        public void Remove(File file) {
            RemoveFrom(Root, file);
        }

        public void AddFrom(
            File parent, 
            File file,
            ITextIO stream
        ) {
            if (Lookup(parent, file.Path) != null) {
                throw new FileExistsException(file.Path);
            }

            string absPath;
            FsMountPoint fsMountPoint = TryMountFs(
                parent.Path, 
                file.Path,
                out absPath
            );

            if (fsMountPoint == null) {
                InternalAdd(parent, file, stream);                
            } else {
                fsMountPoint.Fs.Add(file);
            }
        }

        public void AddFrom(
            File parent,
            File file
        ) {
            AddFrom(parent, file, null);
        }

        public void RemoveFrom(File parent, File file) {
            LookupOrFail(parent, file.Path);

            string absPath;
            FsMountPoint fsMountPoint = TryMountFs(
                parent.Path,
                file.Path,
                out absPath
            );

            if (fsMountPoint == null) {
                parent.RemoveChild(file.Name);
                file.Parent = null;
                OnRemoveFile(file);
            } else {
                fsMountPoint.Fs.Remove(file);
            }
        }

        public File Create(
            string file,
            int uid,
            int gid,
            int permission,
            FileType type,
            ITextIO stream
        ) {
            File baseDirectory = ParseBaseDirectory(file);

            var destFile = new File(
                file,
                uid,
                gid,
                permission,
                type
            );

            AddFrom(baseDirectory, destFile, stream);

            return destFile;
        }

        public File CreateSymbolicLink(
            File sourceFile,
            string symLinkPath,
            int permission,
            ITextIO stream
        ) {
            File baseDirectory = ParseBaseDirectory(symLinkPath);

            var destFile = new File(
                sourceFile,
                symLinkPath,
                permission
            );

            AddFrom(baseDirectory, destFile, stream);

            return destFile;
        }

        public File CreateSymbolicLink(
            File sourceFile,
            string symLinkPath,
            int permission
        ) {
            return CreateSymbolicLink(sourceFile, symLinkPath, permission, null);
        }

        public File Create(
            string file,
            int uid,
            int gid,
            int permission,
            FileType type
        ) {
            return Create(file, uid, gid, permission, type, null);
        }

        public File Create(
            string file,
            int uid,
            int gid,
            int permission
        ) {
            return Create(file, uid, gid, permission, FileType.F_REG);
        }

        public File CreateDir(
            string file,
            int uid,
            int gid,
            int permission
        ) {
            return Create(file, uid, gid, permission, FileType.F_DIR);
        }

        protected File ParseBaseDirectory(string file) {
            if (Lookup(file) != null) {
                throw new FileExistsException(file);
            }

            string fileName = PathUtils.BaseName(file);
            
            if (fileName == "") {
                throw new System.ArgumentException(
                    "File is not valid: " + file
                );
            }

            string pathName = PathUtils.PathName(file);

            File baseDirectory = LookupOrFail(pathName);

            EnsureIsDirectory(baseDirectory);

            return baseDirectory;
        }

        protected FsMountPoint TryMountFs(
            string parent,
            string child,
            out string absPath
        ) {
            absPath = PathUtils.Combine(parent, child);

            foreach(FsMountPoint fsMP in Mounts) {
                if (absPath.StartsWith(fsMP.MountPoint.Path)) {
                    return fsMP;
                }
            }

            return null;
        }

        protected string MaskMountedFile(string path, File mountPoint) {
            return PathUtils.ToAbsPath(
                path.Replace(mountPoint.Path, "")
            );
        }

        protected bool TryLookupFromMountPoint(
            string parent,
            string child,
            out File file
        ) {
            file = null;

            string absPath;
            FsMountPoint fsMountPoint = TryMountFs(parent, child, out absPath);

            if (fsMountPoint == null) {
                return false;            
            }

            string maskedPath = MaskMountedFile(
                absPath,
                fsMountPoint.MountPoint
            );

            file = fsMountPoint.Fs.Lookup(maskedPath);
            return true;
        }

        protected File Lookup(File root, string file) {
            EnsureIsDirectory(root);
            EnsureIsAbsolutePath(file);

            File fileFromMount;
            if (TryLookupFromMountPoint(root.Path, file, out fileFromMount)) {
                return fileFromMount;
            }

            string[] paths = PathUtils.Split(file);

            File needle = null;

            foreach (string path in paths) {
                if (path == "") {
                    continue;
                }

                needle = root.FindChild(path);

                if (needle == null) {
                    return null;
                }

                switch(needle.Type) {
                    case FileType.F_DIR: {
                        root = needle;
                        break;
                    }
                }
            }

            return needle;
        }

        protected void InternalAdd(File parent, File child, ITextIO stream) {
            child.Parent = parent;
            parent.AddChild(child);
            OnAddedFile(child, stream);
        }

        public File LookupOrFail(string path) {
            File file = Lookup(path);

            if (file == null) {
                throw new System.IO.FileNotFoundException(
                    "No such file or directory: " + path
                );
            }

            return file;
        }

        protected File LookupOrFail(File parent, string path) {
            File file = Lookup(parent, path);

            if (file == null) {
                throw new System.IO.FileNotFoundException(
                    "No such file or directory: " + path
                );
            }

            return file;
        }

        public File Lookup(string file) {
            EnsureIsAbsolutePath(file);

            if (file == Root.Path) {
                return Root;
            }

            return Lookup(Root, file);
        }

        protected void EnsureIsAbsolutePath(string file) {
            if (! PathUtils.IsAbsPath(file)) {
                throw new System.ArgumentException(
                    "File must be an absolute path"
                );
            }
        }

        protected void EnsureIsDirectory(File file) {
            if (file.Type != FileType.F_DIR) {
                throw new System.ArgumentException(
                    "Not a directory: " + file.Path
                );
            }
        }
    }
}