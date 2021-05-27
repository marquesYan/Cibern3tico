using System.Collections.Generic;
using Linux.FileSystem;
using Linux;

namespace Linux.Configuration
{    
    public abstract class FileDatabase<T> {
        protected FileTree Fs { get; set; }

        public FileDatabase(FileTree fs) { 
            Fs = fs;
        }

        public abstract void Add(T item);

        public abstract AbstractFile DataSource();

        protected abstract T ItemFromTokens(string[] tokens);

        protected List<T> LoadFromFs() {
            string[] lines = ReadLines();

            List<T> items = new List<T>();

            foreach (string line in lines) {
                string[] tokens = line.Split(':');

                T item = ItemFromTokens(tokens);

                if (item != null) {
                    items.Add(item);
                }
            }

            return items;
        }

        string[] ReadLines() {
            return DataSource()?.Read().Split('\n');
        }
    }
}