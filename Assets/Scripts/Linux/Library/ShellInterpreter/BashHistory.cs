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
            Index++;

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