using System.Collections.Generic;
using Linux.FileSystem;
using Linux.IO;

namespace Linux.Configuration
{    
    public abstract class FileDatabase<T> {
        protected VirtualFileTree Fs { get; set; }

        public FileDatabase(VirtualFileTree fs) { 
            Fs = fs;
        }

        public abstract void Add(T item);

        public int Count() {
            return LoadFromFs().Count;
        }

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

        public List<T> ToList() {
            return LoadFromFs();
        }

        protected string[] ReadLines() {
            using (ITextIO stream = Fs.Open(DataSource().Path, AccessMode.O_RDONLY))
            {
                return stream.ReadLines();
            }
        }

        protected int AppendLine(string line) {
            using(ITextIO stream = Fs.Open(DataSource().Path, AccessMode.O_APONLY))
            {
                return stream.WriteLine(line);
            }
        }
    }
}