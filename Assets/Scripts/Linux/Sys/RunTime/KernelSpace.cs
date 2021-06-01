using System.Collections.Generic;
using System.Threading;
using Linux.Configuration;
using Linux.FileSystem;
using Linux.IO;

namespace Linux.Sys.RunTime
{    
    public class KernelSpace {
        protected Linux.Kernel Kernel;

        public KernelSpace(Linux.Kernel kernel) {
            Kernel = kernel;
        }

        public Process GetCurrentProc() {
            return Kernel.ProcTable.LookupThread(Thread.CurrentThread);
        }

        public File LookupFile(string path) {
            return Kernel.Fs.Lookup(path);
        }

        public User GetCurrentUser() {
            Process proc = GetCurrentProc();

            return Kernel.UsersDb.LookupUid(proc.Uid);
        }

        public ITextIO GetControllingTty() {
            return Kernel.ControllingTty;
        }

        public List<Group> LookupUserGroups(User user) {
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

        public ITextIO Open(string filePath, int mode) {
            File file = LookupFile(filePath);

            if (file == null) {
                throw new System.IO.FileNotFoundException(
                    "No such file or directory: " + filePath
                );
            }

            if (IsFileModePermitted(file, mode)) {
                return Kernel.Fs.Open(file.Path, mode);
            }

            throw new System.AccessViolationException(
                "Permission denied" 
            );
        }

        protected bool IsFileModePermitted(File file, int mode) {
            User user = GetCurrentUser();

            int checkMode = FindUserCheckMode(user, file, mode);

            return (checkMode | file.Permission) != 0;
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