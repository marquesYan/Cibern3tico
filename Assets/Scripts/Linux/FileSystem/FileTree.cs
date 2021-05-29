using System.Collections.Generic;
using UnityEngine;
using Linux.IO;

namespace Linux.FileSystem
{
    public class VirtualFileTree {
        protected Dictionary<File, TextStream> Streams;

        public Directory Root { get; protected set; }

        const char SEPARATOR = '/';

        public VirtualFileTree(Directory root) {
            Root = root;

            Root.Parent = Root;

            Streams = new Dictionary<File, TextStream>();
        }

        public TextStreamWrapper Open(File file, int mode) {
            if (!Streams.ContainsKey(file)) {
                if (mode == AccessMode.O_RDONLY) {
                    throw new System.ArgumentException(
                        "No such file or directory: " + file.Path
                    );
                } else {
                    Add(file);
                }
            }

            TextStream rootStream = Streams[file];

            return new TextStreamWrapper(rootStream, mode);
        }

        public void Add(File file) {
            AddFrom(Root, file);
        }

        public void Remove(File file) {
            RemoveFrom(Root, file);
        }

        public void AddFrom(Directory parent, File file) {
            if (Lookup(parent, file.Path) != null) {
                throw new System.InvalidOperationException(
                    "File exists: " + file.Path
                );
            }

            file.Parent = parent;
            parent.Add(file);

            var rootStream = new TextStream(AccessMode.O_RDWR);
            Streams.Add(file, rootStream);
        }

        public void RemoveFrom(Directory parent, File file) {
            parent.Remove(file);
            file.Parent = null;
        }

        public File Lookup(Directory root, string file) {
            string[] paths = file.Split(SEPARATOR);

            File needle = null;

            foreach (string path in paths) {
                if (path == "") {
                    continue;
                }

                needle = root.Find(child => child.Name == path);

                if (needle == null) {
                    return null;
                }

                if (needle.Type == FileType.F_DIR) {
                    root = (Directory) needle;
                }
            }

            return needle;
        }

        public File Lookup(string file) {
            if (! file.StartsWith(""+SEPARATOR)) {
                throw new System.ArgumentException(
                    "File must be an absolute path"
                );
            }

            if (file == Root.Name) {
                return Root;
            }

            return Lookup(Root, file);
        }

        public string Combine(params string[] paths) {
            return string.Join($"{SEPARATOR}", paths);
        }
    }
}