using UnityEngine;
using System.IO;
using System.Collections;

namespace Linux.FileSystem
{
    public class FileTree {
        public LinuxDirectory Root { get; protected set; }

        char _separator = '/';

        public FileTree(LinuxDirectory root) {
            Root = root;

            Root.Parent = Root;
        }

        public void Add(AbstractFile file) {
            AddFrom(Root, file);
        }

        public void Remove(AbstractFile file) {
            RemoveFrom(Root, file);
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
            if (! file.StartsWith($"{_separator}")) {
                throw new System.ArgumentException("File must be an absolute path");
            }

            if (file == Root.Name) {
                return Root;
            }

            string[] paths = file.Split(_separator);

            AbstractFile needle = Root;

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

        public string Combine(params string[] paths) {
            return string.Join($"{_separator}", paths);
        }
    }
}