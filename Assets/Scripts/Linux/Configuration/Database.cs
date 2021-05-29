using System.Collections.Generic;
using Linux.FileSystem;
using Linux.IO;
using Linux;

namespace Linux.Configuration
{    
    public abstract class FileDatabase<T> {
        protected VirtualFileTree Fs { get; set; }

        public FileDatabase(VirtualFileTree fs) { 
            Fs = fs;
        }

        public abstract void Add(T item);

        public abstract File DataSource();

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

        protected string[] ReadLines() {
            TextStreamWrapper stream = Fs.Open(DataSource(), AccessMode.O_WRONLY);
            return stream.ReadLines();
        }

        protected int AppendLine(string line) {
            TextStreamWrapper stream = Fs.Open(DataSource(), AccessMode.O_APONLY);
            return stream.AppendLine(line);
        }
    }
}