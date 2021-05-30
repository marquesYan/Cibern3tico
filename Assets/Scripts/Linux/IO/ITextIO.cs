using System.Collections.Generic;

namespace Linux.IO
{
    public interface ITextIO {
        int WriteLine(string line);

        int WriteLines(string[] lines);

        string[] ReadLines();

        int Write(string data);

        string Read();

        void Close();
    }
}