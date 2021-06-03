using System;
using System.Collections.Generic;

namespace Linux.IO
{
    public interface ITextIO : IDisposable {
        int WriteLine(string line);

        int WriteLines(string[] lines);

        string[] ReadLines();
        string ReadLine();

        int Write(string data);

        string Read();
        void Truncate();

        string Read(int length);
        void Seek(int position);

        void Close();

        int GetMode();
    }
}