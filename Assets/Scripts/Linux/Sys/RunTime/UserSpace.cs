using System;
using Linux.PseudoTerminal;
using Linux.IO;

namespace Linux.Sys.RunTime
{    
    public class UserSpace {
        protected KernelSpace KernelSpace;

        public ITextIO Tty {
            get { 
                return Open(
                    KernelSpace.LookupByFD(255).Path,
                    AccessMode.O_RDWR
                );
            }
        }

        public ITextIO Stdout { 
            get { 
                return Open(
                    KernelSpace.LookupByFD(1).Path,
                    AccessMode.O_APONLY
                );
            }
        }
        public ITextIO Stdin { 
            get { 
                return Open(
                    KernelSpace.LookupByFD(0).Path,
                    AccessMode.O_RDONLY
                );
            }
        }
        public ITextIO Stderr { 
            get {
                return Open(
                    KernelSpace.LookupByFD(2).Path,
                    AccessMode.O_APONLY
                );
            }
        }

        public UserSpace(KernelSpace kernelSpace) {
            KernelSpace = kernelSpace;
        }

        public KernelSpace AccessKernelSpace() {
            if (KernelSpace.IsRootUser()) {
                return KernelSpace;
            }

            throw new System.AccessViolationException(
                "Permission denied"
            );
        }

        public int GetPid() {
            return KernelSpace.GetCurrentProc().Pid;
        }

        public int GetPPid() {
            return KernelSpace.GetCurrentProc().PPid;
        }

        public string GetCwd() {
            return KernelSpace.GetCurrentProc().Cwd;
        }

        public string GetLogin() {
            return KernelSpace.GetCurrentUser().Login;
        }

        public void WaitPid(int pid) {
            KernelSpace.WaitPid(pid);
        }

        public ITextIO Open(string filePath, int mode) {
            return KernelSpace.Open(filePath, mode);
        }

        public void Print(string message, char end) {
            Stdout.Write(message + end);
        }

        public int CreateProcess(string[] cmdLine) {
            return KernelSpace.CreateProcess(cmdLine).Pid;
        }

        public void Print(string message) {
            Print(message, AbstractTextIO.LINE_FEED);
        }

        public string Input(string prompt, char end) {
            Print(prompt, end);
            Tty.Write(CharacterControl.C_BLOCK_REMOVE);
            return Stdin.ReadLine();
        }

        public string Input(string prompt) {
            return Input(prompt, AbstractTextIO.LINE_FEED);
        }
    }
}