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

        public BashHistory(UserSpace userSpace) {
            UserSpace = userSpace;
            FilePath = userSpace.ExpandUser("~/.bash_history");
            Index = 1;

            // Create history file
            using (ITextIO stream = UserSpace.Open(FilePath, AccessMode.O_APONLY)) {
                //
            }
        }

        public bool IsHistoryOn() {
            return Index != 1;
        }

        public void Add(string command) {
            Index = 1;
            using (ITextIO stream = UserSpace.Open(FilePath, AccessMode.O_APONLY)) {
                stream.WriteLine(command);
            }
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

            if (index < 1) {
                index = 1;
            }

            Index = index;
        }

        protected string ReadHistoryLine() {
            using (ITextIO stream = UserSpace.Open(FilePath, AccessMode.O_RDONLY)) {
                string[] lines = stream.ReadLines();

                try {
                    return lines[lines.Length - Index];
                } catch (System.IndexOutOfRangeException) {
                    return null;
                }
            }
        }
    }
}