using System;
using Linux.IO;

namespace Linux.Sys.RunTime
{    
    public class UserSpace {
        protected KernelSpace KernelSpace;

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

        public ITextIO Open(string filePath, int mode) {
            return KernelSpace.Open(filePath, mode);
        }

        public void Print(string message, char end) {
            ITextIO tty = KernelSpace.GetControllingTty();

            if (tty == null) {
                throw new InvalidOperationException(
                    "No controlling terminal"
                );
            }

            tty.Write(message + end);
        }

        public int CreateProcess(string[] cmdLine) {
            return KernelSpace.CreateProcess(cmdLine).Pid;
        }

        public void Print(string message) {
            Print(message, AbstractTextIO.LINE_FEED);
        }

        public string Input(string message) {
            Print(message);
            return KernelSpace.GetControllingTty().ReadLine();
        }
    }
}