using System;
using System.Collections.Generic;
using System.Threading;
using Linux.Configuration;
using Linux.FileSystem;
using Linux.IO;
using UnityEngine;

namespace Linux.Sys.RunTime
{
    public class ReadOnlyFile {
        public readonly string Path;

        public readonly string Name;

        public readonly string CreatedAt;
    
        public readonly string UpdatedAt;

        public readonly FileType Type;

        public readonly int Permission;

        public readonly int Uid;

        public readonly int Gid;

        public readonly ReadOnlyFile Parent;

        public readonly ReadOnlyFile SourceFile;

        public ReadOnlyFile(
            string path,
            string name,
            string createdAt,
            string updatedAt,
            FileType type,
            int permission,
            int uid,
            int gid,
            ReadOnlyFile parent,
            ReadOnlyFile sourceFile
        ) {
            Path = path;
            Name = name;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
            Type = type;
            Permission = permission;
            Uid = uid;
            Gid = gid;
            Parent = parent;
            SourceFile = sourceFile;
        }

        public static ReadOnlyFile FromFile(File file) {
            ReadOnlyFile parent = null;
            ReadOnlyFile sourceFile = null;

            if (file.Parent != null) {
                // The root file points Parent to himself
                if (file.Parent == file) {
                    parent = new ReadOnlyFile(
                        file.Parent.Path,
                        file.Parent.Name,
                        file.Parent.CreatedAt.ToString(),
                        file.Parent.UpdatedAt.ToString(),
                        file.Parent.Type,
                        file.Parent.Permission,
                        file.Parent.Uid,
                        file.Parent.Gid,
                        null,
                        null
                    );
                } else {
                    parent = ReadOnlyFile.FromFile(file.Parent);
                }
            }
            
            if (file.SourceFile != null) {
                sourceFile = ReadOnlyFile.FromFile(file.SourceFile);
            }

            return new ReadOnlyFile(
                file.Path,
                file.Name,
                file.CreatedAt.ToString(),
                file.UpdatedAt.ToString(),
                file.Type,
                file.Permission,
                file.Uid,
                file.Gid,
                parent,
                sourceFile
            );
        }
    }

    public class KernelSpace {
        public Linux.Kernel Kernel;

        public KernelSpace(Linux.Kernel kernel) {
            Kernel = kernel;
        }

        // Public Calls

        public int Open(string filePath, int mode) {
            File file = LookupFileOrFail(filePath);

            if (IsFileModePermitted(file, mode)) {
                Process proc = GetCurrentProc();

                return Kernel.ProcTable.AttachIO(proc, filePath, mode);
            }

            ThrowPermissionDenied();
            return -1;
        }

        public void ChangeDirectory(string path) {
            File directory = LookupDirectoryOrFail(path);

            if (!CanEnterDirectory(directory)) {
                ThrowPermissionDenied();
            }

            Kernel.ProcTable.ChangeDirectory(GetPid(), directory);
        }

        public string GetFdPath(int fd) {
            Process proc = GetCurrentProc();

            return Kernel.ProcTable.GetFdPath(proc, fd);
        }

        public bool FileExists(string path) {
            return LookupFile(path) != null;
        }

        public List<ReadOnlyFile> ListDirectory(string path) {
            File directory = LookupDirectoryOrFail(path);

            if (!CanListDirectory(directory)) {
                ThrowPermissionDenied();
            }

            List<ReadOnlyFile> files = new List<ReadOnlyFile>();

            foreach (File child in directory.ListChilds()) {
                ReadOnlyFile childFile = ReadOnlyFile.FromFile(child);
                files.Add(childFile);
            }

            return files;
        }

        public string RealPath(string path) {
            File file = LookupFileOrFail(path);

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

        protected File LookupFileOrFail(string path) {
            return Kernel.Fs.LookupOrFail(path);
        }

        protected File LookupDirectoryOrFail(string path) {
            File directory = LookupFileOrFail(path);

            if (!(directory.Type == FileType.F_DIR 
                    || directory.Type == FileType.F_MNT))
            {
                throw new InvalidOperationException(
                    "Not a directory: " + path
                );
            }

            return directory;
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

        protected bool CanEnterDirectory(File directory) {
            if (IsRootUser()) {
                return true;
            }

            int checkMode = FindRequiredEnterDirectoryPermission(directory);

            return (checkMode | directory.Permission) != 0;
        }

        protected bool CanListDirectory(File directory) {
            if (IsRootUser()) {
                return true;
            }

            int checkMode = FindRequiredListDirectoryPermission(directory);

            return (checkMode | directory.Permission) != 0;
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

        protected bool IsUserMemberOf(User user, int gid) {
            List<Group> groups = LookupUserGroups(user);

            return groups.Find(g => g.Gid == gid) != null;
        }

        protected int FindRequiredListDirectoryPermission(File directory) {
            User user = GetCurrentUser();

            if (user.Uid == directory.Uid) {
                return PermModes.S_IRUSR;
            }
            
            if (IsUserMemberOf(user, directory.Gid)) {
                return PermModes.S_IRGRP;
            }

            return PermModes.S_IROTH;
        }

        protected int FindRequiredEnterDirectoryPermission(File directory) {
            User user = GetCurrentUser();

            if (user.Uid == directory.Uid) {
                return PermModes.S_IRUSR & PermModes.S_IXUSR;
            }
            
            if (IsUserMemberOf(user, directory.Gid)) {
                return PermModes.S_IRGRP & PermModes.S_IXGRP;
            }

            return PermModes.S_IROTH & PermModes.S_IXOTH;
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
                if (IsUserMemberOf(user, file.Gid)) {
                    switch (mode) {
                        case AccessMode.O_RDONLY: return PermModes.S_IRGRP;
                        case AccessMode.O_RDWR: return PermModes.S_IRGRP & PermModes.S_IWGRP;
                        case AccessMode.O_APONLY:
                        case AccessMode.O_WRONLY: return PermModes.S_IWGRP;
                    }
                } else {
                    switch (mode) {
                        case AccessMode.O_RDONLY: return PermModes.S_IROTH;
                        case AccessMode.O_RDWR: return PermModes.S_IROTH & PermModes.S_IWOTH;
                        case AccessMode.O_APONLY:
                        case AccessMode.O_WRONLY: return PermModes.S_IWOTH;
                    }
                }
            }

            throw new System.ArgumentException(
                "Invalid mode: " + mode
            );
        }
    }
}