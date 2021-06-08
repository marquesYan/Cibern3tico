using System.Collections.Generic;
using System.Threading;
using Linux.PseudoTerminal;
using Linux.FileSystem;
using Linux.IO;

namespace Linux.Sys.RunTime
{    
    public class UserSpace {
        public KernelSpace Api;

        // public bool IsShutdown {
        //     get { return Api.IsShutdown; }
        // }

        public ITextIO Tty {
            get {
                return Api.LookupByFD(255);
            }
        }

        public ITextIO Stdout { 
            get { 
                return Api.LookupByFD(1);
            }
        }
        public ITextIO Stdin { 
            get { 
                return Api.LookupByFD(0);
            }
        }
        public ITextIO Stderr { 
            get {
                return Api.LookupByFD(2);
            }
        }

        public UserSpace(KernelSpace kernelSpace) {
            Api = kernelSpace;
        }

        public string ResolvePath(string path) {
            if (PathUtils.IsAbsPath(path)) {
                return path;
            }

            Dictionary<string, string> environment = Api.GetEnviron();

            return PathUtils.Combine(environment["PWD"], path);
        }

        public string ExpandUser(string path) {
            if (path.StartsWith("~")) {
                return path.Replace("~", Api.GetHomeDir());
            }
            
            return path;
        }

        public void Exit(int exitCode) {
            throw new ExitProcessException(exitCode);
        }

        public ITextIO Open(string filePath, int mode) {
            int fd = Api.Open(filePath, mode);
            return Api.LookupByFD(fd);
        }

        public void Print(string message, string end) {
            Stdout.Write(message + end);
        }

        public void Print(string message) {
            Print(message, $"{AbstractTextIO.LINE_FEED}");
        }

        public string Input(string prompt, string end) {
            Print(prompt, end);

            Thread.Sleep(200);
            return Stdin.ReadLine();
        }

        public string Input(string prompt) {
            return Input(prompt, $"{AbstractTextIO.LINE_FEED}");
        }
    }
}