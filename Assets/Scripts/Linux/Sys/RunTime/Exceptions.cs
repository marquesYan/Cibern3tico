using System;

namespace Linux.Sys.RunTime
{
    public class ExitProcessException : Exception {
        public int ExitCode { get; protected set; }

        public ExitProcessException(int exitCode) : base("Process exited")
        {
            ExitCode = exitCode;
        }
    }

    public class KeyboardInterruptException : ExitProcessException {
        public KeyboardInterruptException() : base(120) { }
    }
}