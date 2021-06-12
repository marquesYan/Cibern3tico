using System.Collections.Generic;
using SysFile = System.IO.File;
using SysDir = System.IO.Directory;
using SysPath = System.IO.Path;
using Linux.IO;
using UnityEngine;

namespace Linux.FileSystem
{
    public class LocalFileTree : AbstractFileTree {

        protected string LocalPath;

        public LocalFileTree(
            string localPath,
            File root,
            File MountPoint
        ) : base(root, MountPoint) {
            LocalPath = localPath;

            if (!SysDir.Exists(LocalPath)) {
                SysDir.CreateDirectory(LocalPath);
            }

            MapLocalToVirtualTree();
        }

        public LocalFileTree(
            string localPath,
            File root
        ) : this(localPath, root, null) {}

        protected void MapLocalToVirtualTree() {
            MapRootTree(Root);
        }

        protected void MapRootTree(File root) {
            string localRootFile = GetLocalPath(root);
            string rootPath = PathUtils.ToAbsPath(root.Path);

            if (MountPoint != null) {
                rootPath = PathUtils.Combine(
                    MountPoint.Path,
                    rootPath
                );
            }

            int filePerm = Perm.FromInt(6, 6, 4);
            int dirPerm = Perm.FromInt(7, 5, 5);

            foreach (string path in SysDir.GetFiles(localRootFile)) {
                string relPath = SysPath.GetFileName(path);

                File file = new File(
                    PathUtils.Combine(rootPath, relPath),
                    Root.Uid,
                    Root.Gid,
                    filePerm,
                    FileType.F_REG
                );

                AddFrom(root, file);
            }

            foreach (string dir in SysDir.GetDirectories(localRootFile)) {
                string relPath = SysPath.GetFileName(dir);

                File file = new File(
                    PathUtils.Combine(rootPath, relPath),
                    Root.Uid,
                    Root.Gid,
                    dirPerm,
                    FileType.F_DIR
                );

                AddFrom(root, file);

                MapRootTree(file);
            }
        }

        protected string GetLocalPath(File file) {
            string virtualPath = ResolvePathIfMounted(file);

            string normalizedPath = virtualPath.Replace(
                PathUtils.SEPARATOR,
                SysPath.DirectorySeparatorChar
            );

            return SysPath.Combine(
                LocalPath,
                PathUtils.ToRelPath(normalizedPath) // Ensure this is a relative path
            );
        }

        protected override ITextIO OpenFileHandler(File file, int mode) {
            string localPath = GetLocalPath(file);
            return new LocalFileStream(localPath, mode);
        }

        protected override void OnAddedFile(File file, ITextIO stream) {
            string localPath = GetLocalPath(file);

            switch (file.Type) {
                case FileType.F_DIR: {
                    SysDir.CreateDirectory(localPath);
                    break;
                }

                default: {
                    Debug.Log("creating file at: " + localPath);
                    if (!SysFile.Exists(localPath)) {
                        using (SysFile.Create(localPath)) {
                            //
                        }
                    }
                    break;
                }
            }
        }

        protected override void OnRemoveFile(File file) {
            string localPath = GetLocalPath(file);

            switch (file.Type) {
                case FileType.F_DIR: {
                    SysDir.Delete(localPath);
                    break;
                }

                default: {
                    SysFile.Delete(localPath);
                    break;
                }
            }
        }
    }
}