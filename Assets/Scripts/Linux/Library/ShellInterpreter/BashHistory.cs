using Linux.IO;
using Linux.FileSystem;
using Linux.Sys.RunTime;
using UnityEngine;

namespace Linux.Library.ShellInterpreter
{
    public class BashHistory {
        protected UserSpace UserSpace;

        protected string FilePath;

        protected int Index;
        protected int Length;

        public BashHistory(UserSpace userSpace) {
            UserSpace = userSpace;
            FilePath = userSpace.ExpandUser("~/.bash_history");
            Index = Length = 0;

            // Create history file
            using (ITextIO stream = UserSpace.Open(FilePath, AccessMode.O_APONLY)) {
                //
            }

            ReadHistoryLine();
        }

        public void Add(string command) {
            using (ITextIO stream = UserSpace.Open(FilePath, AccessMode.O_APONLY)) {
                stream.WriteLine(command);
            }
            Index = 0;
            Length++;
        }

        public string Last() {
            UpdateIndex(1);

            return ReadHistoryLine();
        }

        public string Next() {
            UpdateIndex(-1);
            
            return ReadHistoryLine();
        }

        protected void UpdateIndex(int step) {
            int index = Index + step;

            if (index < 0) {
                index = 0;
            } else if (index > Length) {
                index = Length;
            }

            Index = index;
        }

        protected string ReadHistoryLine() {
            using (ITextIO stream = UserSpace.Open(FilePath, AccessMode.O_RDONLY)) {
                string[] lines = stream.ReadLines();

                Length = lines.Length;

                try {
                    return lines[Length - (Index + 1)];
                } catch (System.IndexOutOfRangeException) {
                    return null;
                }
            }
        }
    }
}