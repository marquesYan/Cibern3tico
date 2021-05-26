using UnityEngine;
using System.IO;
using System.Collections;

namespace Linux.FileSystem
{
    public class FileTree {
        LinuxDirectory root = new LinuxDirectory("/", 
                                      new Perms[3] { Perms.ALL, Perms.RX, Perms.RX });

        char separator = '/';

        public FileTree() {
            root.Parent = root;
        }

        public void Add(AbstractFile file) {
            AddFrom(root, file);
        }

        public void Remove(AbstractFile file) {
            RemoveFrom(root, file);
        }

        public void AddFrom(LinuxDirectory parent, AbstractFile file) {
            file.Parent = parent;
            parent.Childs.Add(file);
        }

        public void RemoveFrom(LinuxDirectory parent, AbstractFile file) {
            parent.Childs.Remove(file);
            file.Parent = null;
        }

        public AbstractFile Lookup(string file) {
            if (! file.StartsWith($"{separator}")) {
                throw new System.ArgumentException("File must be an absolute path");
            }

            if (file == root.Name) {
                return root;
            }

            string[] paths = file.Split(separator);

            AbstractFile needle = root;

            foreach (string path in paths) {
                if (path == "") {
                    continue;
                }

                needle = needle.Childs.Find(child => child.Name == path);

                if (needle == null) {
                    return null;
                }
            }

            return needle;
        }
    }
}