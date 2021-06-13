using System;
using System.Collections.Generic;
using System.Threading;
using System.Globalization;
using Linux.Configuration;
using Linux.FileSystem;
using Linux.IO;
using Linux.Net;
using Linux.Sys.Input.Drivers;
using UnityEngine;

namespace Linux.Sys.RunTime
{
    public class ReadOnlyFile {
        public const string DateTimeFormat = "MMMM dd HH:mm"; 

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

        public readonly bool IsHidden;

        public ReadOnlyFile(
            string path,
            string name,
            string createdAt,
            string updatedAt,
            FileType type,
            int permission,
            int uid,
            int gid,
            bool isHidden,
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
            IsHidden = isHidden;
            Parent = parent;
            SourceFile = sourceFile;
        }

        public static string FormatDate(DateTime dt) {
            return dt.ToString(DateTimeFormat);
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
                        FormatDate(file.Parent.CreatedAt),
                        FormatDate(file.Parent.UpdatedAt),
                        file.Parent.Type,
                        file.Parent.Permission,
                        file.Parent.Uid,
                        file.Parent.Gid,
                        file.Parent.IsHidden(),
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
                FormatDate(file.CreatedAt),
                FormatDate(file.UpdatedAt),
                file.Type,
                file.Permission,
                file.Uid,
                file.Gid,
                file.IsHidden(),
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
            if ((mode & (AccessMode.O_WRONLY | AccessMode.O_APONLY)) != 0) {
                CreateFileIfMissing(filePath, false);
            }

            File file = LookupFileOrFail(filePath);

            if (IsFileModePermitted(file, mode)) {
                Process proc = GetCurrentProc();

                return Kernel.ProcTable.AttachIO(proc, filePath, mode);
            }

            ThrowPermissionDenied();
            return -1;
        }

        public UdpSocket UdpSocket(string ipAddress, int port) {
            UEvent netEvent = Kernel.EventTable.LookupByType(DevType.NETWORK);

            if (netEvent == null) {
                throw new InvalidOperationException(
                    "network driver not available"
                );
            }

            VirtualNetDriver netDriver = (VirtualNetDriver)netEvent.Driver;

            return new UdpSocket(netDriver.GetNetInterface(), ipAddress, port);
        }

        public string LookupUserLogin(int uid) {
            User user = Kernel.UsersDb.LookupUid(uid);
            
            if (user == null) {
                return null;
            }

            return user.Login;
        }

        public int LookupUserUid(string login) {
            User user = Kernel.UsersDb.LookupLogin(login);
            
            if (user == null) {
                throw new System.InvalidOperationException(
                    "user not found: " + login
                );
            }

            return user.Uid;
        }

        public int LookupGroupGid(string name) {
            Group group = Kernel.GroupsDb.LookupName(name);
            
            if (group == null) {
                throw new System.InvalidOperationException(
                    "group not found: " + name
                );
            }

            return group.Gid;
        }

        public string LookupGroupName(int gid) {
            Group group = Kernel.GroupsDb.LookupGid(gid);
            
            if (group == null) {
                return null;
            }

            return group.Name;
        }

        public void RemoveFile(string path) {
            File file = LookupFileOrFail(path);

            if (!IsFileModePermitted(file, AccessMode.O_WRONLY)) {
                ThrowPermissionDenied();
            }

            Kernel.Fs.Delete(path);
        }

        public void CreateDir(string path) {
            string[] parts = PathUtils.Split(
                PathUtils.PathName(path)
            );

            string absPath = "/";

            foreach (string file in parts) {
                absPath = PathUtils.Combine(absPath, file);
                File dir = LookupDirectoryOrFail(absPath);

                if (!CanEnterDirectory(dir)) {
                    ThrowPermissionDenied();
                }
            }

            User user = GetCurrentUser();

            int umask = BuildUmask(user);

            Kernel.Fs.CreateDir(
                path,
                user.Uid,
                user.Gid,
                BuildPermissionFromUmask(true, umask)
            );
        }

        public ReadOnlyFile FindFile(string path) {
            File file = LookupFileOrFail(path);

            return ReadOnlyFile.FromFile(file);
        }

        public int RunAs(string login) {
            User user = Kernel.UsersDb.LookupLogin(login);
            
            if (user == null) {
                throw new ArgumentException(
                    $"User does not exist: {login}"
                );
            }

            if (IsRootUser()) {
                // int pty = OpenPty();

                Process process = CreateProcess(
                    user,
                    new string[] { user.Shell },
                    new Dictionary<string, string>(),
                    0,
                    1,
                    2
                );

                process.MainTask.Start();

                return process.Pid;
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

        public void ChangeFilePermission(string path, int newPermission) {
            File file = LookupFileOrFail(path);

            User user = GetCurrentUser();

            if (!IsRootUser() && user.Uid != file.Uid) {
                ThrowPermissionDenied();
            }

            file.Permission = newPermission;
        }

        public void ChangeFileOwner(string path, int newUid) {
            File file = LookupFileOrFail(path);

            User user = GetCurrentUser();

            if (!IsRootUser() && user.Uid != file.Uid) {
                ThrowPermissionDenied();
            }

            file.Uid = newUid;
        }

        public void ChangeFileGroup(string path, int newGid) {
            File file = LookupFileOrFail(path);

            User user = GetCurrentUser();

            if (!IsRootUser() && user.Uid != file.Uid) {
                ThrowPermissionDenied();
            }

            file.Gid = newGid;
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

        public int WaitPid(int pid) {
            Process proc = AccessOtherProcess(pid);

            proc.MainTask.Join();

            return proc.ReturnCode;
        }

        public int OpenPty() {
            return Kernel.PtyTable.Add(GetCurrentUser());
        }

        public int GetPid() {
            return GetCurrentProc().Pid;
        }

        public int GetUmask() {
            return GetCurrentProc().Umask;
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

        public string GetHomeDir() {
            return GetCurrentUser().HomeDir;
        }

        public void KillProcess(int pid) {
            KillProcess(pid, ProcessSignal.SIGTERM);
        }

        public void KillProcess(int pid, ProcessSignal signal) {
            Process process = AccessOtherProcess(pid);

            Kernel.KillProcess(process, signal);
        }

        public Dictionary<string, string> GetEnviron() {
            return GetCurrentProc().Environ;
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

        protected List<Group> LookupUserGroups(User user) {
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
            return CreateProcess(
                GetCurrentUser(),
                cmdLine,
                new Dictionary<string, string>(GetEnviron()),
                stdinFd,
                stdoutFd,
                stderrFd
            );
        }

        protected Process CreateProcess(
            User user,
            string[] cmdLine,
            Dictionary<string, string> environ,
            int stdinFd,
            int stdoutFd,
            int stderrFd,
            int umask
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

            return Kernel.CreateProcess(
                proc.Pid,
                user,
                cmdLine,
                umask,
                environ,
                stdinFd,
                stdoutFd,
                stderrFd
            );
        }

        protected int BuildUmask(User user) {
            return user.Uid == 0 ? Perm.FromInt(0, 2, 2) : Perm.FromInt(0, 0, 2);
        }

        protected Process CreateProcess(
            User user,
            string[] cmdLine,
            Dictionary<string, string> environ,
            int stdinFd,
            int stdoutFd,
            int stderrFd
        ) {
            return CreateProcess(
                user,
                cmdLine,
                environ,
                stdinFd,
                stdoutFd,
                stderrFd,
                BuildUmask(user)
            );
        }

        protected User GetCurrentUser() {
            Process proc = GetCurrentProc();

            return Kernel.UsersDb.LookupUid(proc.Uid);
        }

        protected Process GetCurrentProc() {
            return Kernel.ProcTable.LookupThread(Thread.CurrentThread);
        }

        protected int BuildPermissionFromUmask(bool asDirectory, int umask) {
            int basePermission;

            if (asDirectory) {
                basePermission = Perm.FromInt(7, 7, 7);
            } else {
                basePermission = Perm.FromInt(6, 6, 6);
            }

            return basePermission - umask;
        }

        protected void CreateFileIfMissing(string path, bool asDirectory) {
            if (FileExists(path)) {
                return;
            }

            int permission = BuildPermissionFromUmask(asDirectory, GetUmask());

            FileType type = asDirectory ? FileType.F_DIR : FileType.F_REG;

            User user = GetCurrentUser();

            Kernel.Fs.Create(
                path,
                user.Uid,
                user.Gid,
                permission,
                type
            );
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

            return (checkMode & directory.Permission) != 0;
        }

        protected bool CanListDirectory(File directory) {
            if (IsRootUser()) {
                return true;
            }

            int checkMode = FindRequiredListDirectoryPermission(directory);

            return (checkMode & directory.Permission) != 0;
        }

        protected bool IsFileModePermitted(File file, int mode) {
            if (IsRootUser()) {
                return true;
            }

            User user = GetCurrentUser();

            int checkMode = FindUserCheckMode(user, file, mode);

            return (checkMode & file.Permission) != 0;
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
                return PermModes.S_IRUSR | PermModes.S_IXUSR;
            }
            
            if (IsUserMemberOf(user, directory.Gid)) {
                return PermModes.S_IRGRP | PermModes.S_IXGRP;
            }

            return PermModes.S_IROTH | PermModes.S_IXOTH;
        }

        protected int FindUserCheckMode(User user, File file, int mode) {
            if (user.Uid == file.Uid) {
                switch (mode) {
                    case AccessMode.O_RDONLY: return PermModes.S_IRUSR;
                    case AccessMode.O_RDWR: return PermModes.S_IRUSR | PermModes.S_IWUSR;
                    case AccessMode.O_APONLY:
                    case AccessMode.O_WRONLY: return PermModes.S_IWUSR;
                }
            } else {
                if (IsUserMemberOf(user, file.Gid)) {
                    switch (mode) {
                        case AccessMode.O_RDONLY: return PermModes.S_IRGRP;
                        case AccessMode.O_RDWR: return PermModes.S_IRGRP | PermModes.S_IWGRP;
                        case AccessMode.O_APONLY:
                        case AccessMode.O_WRONLY: return PermModes.S_IWGRP;
                    }
                } else {
                    switch (mode) {
                        case AccessMode.O_RDONLY: return PermModes.S_IROTH;
                        case AccessMode.O_RDWR: return PermModes.S_IROTH | PermModes.S_IWOTH;
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