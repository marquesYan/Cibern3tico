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
            File root
        ) : base(root) {
            LocalPath = localPath;
            MapLocalToVirtualTree();
        }

        protected void MapLocalToVirtualTree() {
            MapRootTree(Root);
        }

        protected void MapRootTree(File root) {
            string rootFile = GetLocalPath(root);

            foreach (string path in SysDir.GetFiles(rootFile)) {
                string relPath = SysPath.GetFileName(path);

                bool isDir = SysDir.Exists(path);

                FileType type = isDir ? FileType.F_DIR : FileType.F_REG;

                int permission = isDir ? Perm.FromInt(7, 5, 5) : Perm.FromInt(6, 6, 4);

                File file = new File(
                    PathUtils.Combine(root.Name, relPath),
                    Root.Uid,
                    Root.Gid,
                    permission,
                    type
                );

                AddFrom(root, file);

                if (isDir) {
                    MapRootTree(file);
                }
            }
        }

        protected string GetLocalPath(File file) {
            string normalizedPath = file.Path.Replace(
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