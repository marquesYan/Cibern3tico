using System;
using System.Collections.Generic;
using System.Threading;
using Linux.Configuration;
using Linux.FileSystem;
using Linux.IO;
using UnityEngine;

namespace Linux.Sys.RunTime
{    
    public class KernelSpace {
        public Linux.Kernel Kernel;

        public bool IsShutdown { 
            get {
                return Kernel.IsShutdown;
            }
        }

        public KernelSpace(Linux.Kernel kernel) {
            Kernel = kernel;
        }

        // Public Calls

        public int Open(string filePath, int mode) {
            File file = LookupFile(filePath);

            if (file == null) {
                throw new System.IO.FileNotFoundException(
                    "No such file or directory: " + filePath
                );
            }

            if (IsFileModePermitted(file, mode)) {
                Process proc = GetCurrentProc();

                return Kernel.ProcTable.AttachIO(proc, filePath, mode);
            }

            ThrowPermissionDenied();
            return -1;
        }

        public string GetFdPath(int fd) {
            Process proc = GetCurrentProc();

            return Kernel.ProcTable.GetFdPath(proc, fd);
        }

        public string RealPath(string path) {
            File file = LookupFile(path);

            if (file.Type == FileType.F_SYL) {
                return file.SourceFile.Path;
            }

            return file.Path;
        }

        public void Trap(ProcessSignal signal, SignalHandler handler) {
            Process proc = GetCurrentProc();

            Kernel.ProcSigTable.Add(proc, signal, handler);
        }

        public void WaitPid(int pid) {
            Process proc = AccessOtherProcess(pid);

            proc.MainTask.Join();
        }

        public int OpenPty() {
            return Kernel.PtyTable.Add(GetCurrentUser());
        }

        public int GetPid() {
            return GetCurrentProc().Pid;
        }

        public int GetPPid() {
            return GetCurrentProc().PPid;
        }

        public string GetCwd() {
            return GetCurrentProc().Cwd;
        }

        public int GetUid() {
            return GetCurrentUser().Uid;
        }

        public string GetLogin() {
            return GetCurrentUser().Login;
        }

        public string[] GetArgs() {
            return GetCurrentProc().CmdLine;
        }

        public string GetExecutable() {
            return GetCurrentProc().Executable;
        }

        public void KillProcess(int pid) {
            KillProcess(pid, ProcessSignal.SIGTERM);
        }

        public void KillProcess(int pid, ProcessSignal signal) {
            Process process = AccessOtherProcess(pid);

            Kernel.KillProcess(process, signal);
        }

        public Dictionary<string, string> GetEnviron() {
            return new Dictionary<string, string>(GetCurrentProc().Environ);
        }

        public bool IsRootUser() {
            return GetUid() == 0;
        }

        public ITextIO LookupByFD(int fd) {
            Process proc = GetCurrentProc();

            return Kernel.ProcTable.LookupFd(proc, fd);
        }

        public void Dup2(int pid, int fd) {
            Process proc = GetCurrentProc();

            EnsureFdsExists(proc, new int[] { fd });

            Process destinationProc = AccessOtherProcess(pid);
            
            Kernel.ProcTable.DuplicateFd(
                proc,
                fd,
                destinationProc,
                fd
            );
        }

        public int StartProcess(
            string[] cmdLine,
            params int[] fds
        ) {
            Process currentProc = GetCurrentProc();

            EnsureFdsExists(currentProc, fds);

            Process proc = CreateProcess(
                cmdLine,
                0,
                1,
                2
            );

            foreach(int fd in fds) {
                Dup2(proc.Pid, fd);
            }

            proc.MainTask.Start();

            return proc.Pid;
        }

        public int StartProcess(string[] cmdLine) {
            return StartProcess(cmdLine, 0, 1, 2);
        }

        public int StartProcess(
            string[] cmdLine,
            int stdinFd,
            int stdoutFd,
            int stderrFd
        ) {
            Process proc = CreateProcess(
                cmdLine,
                stdinFd,
                stdoutFd,
                stderrFd
            );

            proc.MainTask.Start();

            return proc.Pid;
        }

        // Privileged Access Required

        public Kernel AccessKernel() {
            EnsureIsRoot();

            return Kernel;
        }

        public Process LookupProcessByPid(int pid) {
            EnsureIsRoot();

            return Kernel.ProcTable.LookupPid(pid);
        }

        public List<Group> LookupUserGroups(User user) {
            EnsureIsRoot();

            return Kernel.GroupsDb.ToList().FindAll(
                group => {
                    foreach (string login in group.Users) {
                        if (user.Login == login) {
                            return true;
                        }
                    }

                    return false;
                }
            );
        }

        protected File LookupFile(string path) {
            return Kernel.Fs.Lookup(path);
        }

        protected bool CanAccessProcess(Process otherProc) {
            if (IsRootUser()) {
                return true;
            }

            Process currentProc = GetCurrentProc();

            return currentProc.ChildPids.Contains(otherProc.Pid);
        }

        protected Process AccessOtherProcess(int pid) {
            Process process = Kernel.ProcTable.LookupPid(pid);

            if (process == null) {
                throw new ArgumentException(
                    "No such process: " + pid
                );
            }

            if (!CanAccessProcess(process)) {
                ThrowPermissionDenied();
            }

            return process;
        }

        protected Process CreateProcess(
            string[] cmdLine,
            int stdinFd,
            int stdoutFd,
            int stderrFd
        ) {
            if (cmdLine.Length == 0) {
                throw new System.ArgumentException("No command line");
            }

            File executable = LookupFile(cmdLine[0]);

            if (executable == null) {
                throw new System.ArgumentException(
                    "Command not found: " + cmdLine[0]
                );
            }

            int[] fds = new int[] {
                stdinFd,
                stdoutFd,
                stderrFd
            };

            Process proc = GetCurrentProc();

            EnsureFdsExists(proc, fds);

            User user = GetCurrentUser();

            return Kernel.CreateProcess(
                proc.Pid,
                user,
                cmdLine,
                stdinFd,
                stdoutFd,
                stderrFd
            );
        }

        protected User GetCurrentUser() {
            Process proc = GetCurrentProc();

            return Kernel.UsersDb.LookupUid(proc.Uid);
        }

        protected Process GetCurrentProc() {
            return Kernel.ProcTable.LookupThread(Thread.CurrentThread);
        }

        protected void EnsureFdsExists(Process process, int[] fds) {
            foreach (int fd in fds) {
                if (Kernel.ProcTable.LookupFd(process, fd) == null) {
                    throw new System.ArgumentException(
                        $"File descriptor not found: {fd}"  
                    );
                }
            }
        }

        protected void ThrowPermissionDenied() {
            throw new System.AccessViolationException(
                "Permission denied" 
            );
        }

        protected bool IsFileModePermitted(File file, int mode) {
            if (IsRootUser()) {
                return true;
            }

            User user = GetCurrentUser();

            int checkMode = FindUserCheckMode(user, file, mode);

            return (checkMode | file.Permission) != 0;
        }

        protected void EnsureIsRoot() {
            if (!IsRootUser()) {
                ThrowPermissionDenied();
            }
        }

        protected int FindUserCheckMode(User user, File file, int mode) {
            if (user.Uid == file.Uid) {
                switch (mode) {
                    case AccessMode.O_RDONLY: return PermModes.S_IRUSR;
                    case AccessMode.O_RDWR: return PermModes.S_IRUSR & PermModes.S_IWUSR;
                    case AccessMode.O_APONLY:
                    case AccessMode.O_WRONLY: return PermModes.S_IWUSR;
                }
            } else {
                List<Group> groups = LookupUserGroups(user);

                if (groups.Find(g => g.Gid == file.Gid) == null) {
                    switch (mode) {
                        case AccessMode.O_RDONLY: return PermModes.S_IROTH;
                        case AccessMode.O_RDWR: return PermModes.S_IROTH & PermModes.S_IWOTH;
                        case AccessMode.O_APONLY:
                        case AccessMode.O_WRONLY: return PermModes.S_IWOTH;
                    }
                } else {
                    switch (mode) {
                        case AccessMode.O_RDONLY: return PermModes.S_IRGRP;
                        case AccessMode.O_RDWR: return PermModes.S_IRGRP & PermModes.S_IWGRP;
                        case AccessMode.O_APONLY:
                        case AccessMode.O_WRONLY: return PermModes.S_IWGRP;
                    }
                }
            }

            throw new System.ArgumentException(
                "Invalid mode: " + mode
            );
        }
    }
}