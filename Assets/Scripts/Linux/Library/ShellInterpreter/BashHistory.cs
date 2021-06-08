using Linux.IO;
using Linux.FileSystem;
using Linux.Sys.RunTime;

namespace Linux.Library.ShellInterpreter
{
    public class BashHistory {
        protected UserSpace UserSpace;

        protected string FilePath;

        public BashHistory(UserSpace userSpace) {
            UserSpace = userSpace;
            FilePath = userSpace.ExpandUser("~/.bash_history");

            // Create history file
            using (ITextIO stream = UserSpace.Open(FilePath, AccessMode.O_APONLY)) {
                //
            }
        }

        public void Add(string command) {
            using (ITextIO stream = UserSpace.Open(FilePath, AccessMode.O_APONLY)) {
                stream.WriteLine(command);
            }
        }

        public string Last(int step) {
            using (ITextIO stream = UserSpace.Open(FilePath, AccessMode.O_RDONLY)) {
                string[] lines = stream.ReadLines();

                try {
                    return lines[lines.Length - step];
                } catch (System.IndexOutOfRangeException) {
                    return null;
                }
            }
        }
    }
}