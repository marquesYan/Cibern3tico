using System.Threading;
using System.Collections.Generic;
using Linux.FileSystem;
using Linux.IO;

namespace Linux.Configuration
{
    public class Process {
        public int Pid { get; protected set; }
        public int PPid { get; protected set; }
        public int Uid { get; protected set; }
        public int Gid { get; protected set; }
        public string Executable { get; protected set; }
        public string[] CmdLine { get; protected set; }
        public Dictionary<string, string> Environ { get; protected set; }
        public Thread MainTask { get; protected set; }
        public List<Thread> BackgroundTasks { get; protected set; }
        public List<int> ChildPids { get; protected set; }
        public List<int> Fds { get; protected set; }
        public string Root { get; protected set; }
        public string Cwd;
        public int Umask;

        public Process(
            int ppid,
            int pid,
            int uid,
            int gid,
            int umask,
            string[] cmdLine,
            Dictionary<string, string> environ,
            string root,
            string cwd,
            Thread mainTask
        ) {
            PPid = ppid;
            Pid = pid;
            Uid = uid;
            Gid = gid;
            Umask = umask;
            Executable = cmdLine[0];
            CmdLine = cmdLine;
            Environ = environ;
            Root = root;
            Cwd = cwd;

            MainTask = mainTask;
            if (MainTask.IsAlive) {
                throw new System.ArgumentException("Main task already exists");
            }

            BackgroundTasks = new List<Thread>();
            ChildPids = new List<int>();
            Fds = new List<int>();
        }

        // public Process CreateThread(Thread childTask) {
        //     Process task = new Process(
        //         PPid,
        //         Pid,
        //         Uid,
        //         Gid,
        //         CmdLine,
        //         Environ,
        //         Root,
        //         Cwd,
        //         MainTask
        //     );

        //     task.BackgroundTasks.Add(childTask);

        //     return task;
        // }
    }

    public class ProcessesTable {
        readonly object _procLock = new object();

        protected VirtualFileTree Fs;

        protected FileDescriptorsTable FdTable;

        public ProcessesTable(VirtualFileTree fs) {
            Fs = fs;
            FdTable = new FileDescriptorsTable();
        }

        public void Close() {
            FdTable = null;
        }

        public List<Process> GetProcesses() {
            return FdTable.GetProcesses();
        }

        public void ChangeDirectory(int pid, File directory) {
            lock(_procLock) {
                Process process = LookupPidOrFail(pid);
                process.Cwd = directory.Path;
            }
        }

        public void Remove(Process process) {
            lock(_procLock) {
                if (process.PPid != 0) {
                    Process parent = LookupPidOrFail(process.PPid);

                    parent.ChildPids.Remove(process.Pid);
                }
                
                FdTable.Remove(process);
            }
        }

        public Process LookupPidOrFail(int pid) {
            Process process = LookupPid(pid);

            if (process == null) {
                throw new System.InvalidOperationException(
                    $"No such process with PID: {pid}"
                );
            }

            return process;
        }

        public void Add(Process process) {
            lock(_procLock) {
                if (process.PPid != 0) {
                    Process parent = LookupPidOrFail(process.PPid);

                    parent.ChildPids.Add(process.Pid);
                }
                
                FdTable.Add(process);
                ProcessToFile(process);
            }
        }

        public Process Create(
            int ppid,
            int uid,
            int gid,
            int umask,
            string[] cmdLine,
            Dictionary<string, string> environ,
            string root,
            string cwd,
            Thread mainTask
        ) {
            lock(_procLock) {
                int pid = FdTable.HighestPid + 1;

                Process process = new Process(
                    ppid,
                    pid,
                    uid,
                    gid,
                    umask,
                    cmdLine,
                    environ,
                    root,
                    cwd,
                    mainTask
                );

                Add(process);

                return process;
            }
        }

        public void AttachIO(
            Process process,
            string filePath,
            int mode,
            int fd
        ) {
            lock(_procLock) {
                EnsureProcessExists(process);

                File file = Fs.LookupOrFail(filePath);

                Fs.CreateSymbolicLink(
                    file,
                    GetFdPath(process, fd),
                    Perm.FromInt(mode, 0, 0)
                );

                ITextIO stream = Fs.Open(filePath, mode);

                FdTable.Add(process, stream, fd);

                process.Fds.Add(fd);
            }
        }

        public int AttachIO(
            Process process,
            string filePath,
            int mode
        ) {
            int fd;

            lock(_procLock) {
                fd = FdTable.GetAvailableFd(process);    
            }

            AttachIO(process, filePath, mode, fd);

            return fd;
        }

        public void DuplicateFd(
            Process process,
            int fd,
            Process destProcess,
            int destFd
        ) {
            ITextIO stream = LookupFd(process, fd);

            string fdPath = GetFdPath(process, fd);

            File file = Fs.LookupOrFail(fdPath).SourceFile;

            AttachIO(
                destProcess,
                file.Path,
                stream.GetMode(),
                destFd
            );
        }

        public int DuplicateFd(
            Process process,
            int fd,
            Process destProcess
        ) {
            int destinationFd;

            lock(_procLock) {
                destinationFd = FdTable.GetAvailableFd(destProcess);
            }

            DuplicateFd(
                process, 
                fd, 
                destProcess,
                destinationFd
            );

            return destinationFd;
        }

        public string GetFdPath(Process process, int fd) {
            return PathUtils.Combine(
                ProcessDirectory(process),
                "fd",
                fd.ToString()
            );
        }

        public ITextIO LookupFd(Process process, int fd) {
            return FdTable.LookupFd(process, fd);
        }

        public Process LookupPid(int pid) {
            return FdTable.LookupPid(pid);
        }

        public Process LookupThread(Thread thread) {
            return FdTable.GetProcesses().Find(p => p.MainTask == thread);
        }

        // protected List<Process> LoadFromFs() {
        //     List<Process> processes = new List<Process>();

        //     foreach (File file in DataSource()?.Childs) {
        //         Process process = FileToProcess(file);
        //         if (process != null) {
        //             processes.Add(process);
        //         }
        //     }

        //     return processes;
        // }

        public string ProcessDirectory(Process process) {
            return PathUtils.Combine("/proc", $"{process.Pid}");
        }

        protected void EnsureProcessExists(Process process) {
            if (!FdTable.HasProcess(process)) {
                throw new System.ArgumentException(
                    $"Process with PID '{process.Pid}' does not exist"
                );
            }
        }

        void ProcessToFile(Process process) {
            string path = ProcessDirectory(process);

            File procDirectory = Fs.CreateDir(
                path,
                process.Uid,
                process.Gid,
                Perm.FromInt(5, 5, 5)
            );

            File fdDirectory = Fs.CreateDir(
                PathUtils.Combine(path, "fd"),
                process.Uid,
                process.Gid,
                Perm.FromInt(5, 0, 0)
            );

            File cmdLineFile = Fs.Create(
                PathUtils.Combine(path, "cmdline"),
                process.Uid,
                process.Gid,
                Perm.FromInt(4, 4, 4)
            );

            File environFile = Fs.Create(
                PathUtils.Combine(path, "environ"),
                process.Uid,
                process.Gid,
                Perm.FromInt(4, 0, 0)
            );

            WriteSpec(cmdLineFile, process.CmdLine);

            string[] environ = new string[process.Environ.Count];

            int i = 0;
            foreach(KeyValuePair<string, string> kvp in process.Environ) {
                environ[i] = $"{kvp.Key}={kvp.Value}";
                i++;
            }

            WriteSpec(environFile, environ);
        }

        // Process FileToProcess(File file) {
        //     int pid;

        //     if (file.IsDirectory() && int.TryParse(file.Name, out pid)) {
        //         Directory procDirectory = (Directory) file;
                
        //         File cmdLineFile = procDirectory.Childs.Find(c => c.Name == "cmdline");
        //         string[] cmdLine = cmdLineFile?.Read().Split('\0');
                
        //         File environFile = procDirectory.Childs.Find(c => c.Name == "environ");
        //         string[] environ = environFile?.Read().Split('\0');

        //         File cwdFile = procDirectory.Childs.Find(c => c.Name == "cwd");
        //         File rootFile = procDirectory.Childs.Find(c => c.Name == "root");

        //         int[] ids = ParseIdsFromProcessFile(procDirectory);                

        //         return new Process(
        //             ids[0],
        //             pid,
        //             ids[1],
        //             ids[2],
        //             cmdLine,
        //             environ,
        //             cwdFile?.Path,
        //             rootFile?.Path
        //         );
        //     }

        //     return null;
        // }

        // int[] ParseIdsFromProcessFile(Directory procDirectory) {
        //     File statusFile = Fs.Lookup(procDirectory, "status");
        //     string[] lines = statusFile?.Read().Split('\n');

        //     int[] ids = new int[3];

        //     int position = -1;

        //     foreach(string line in lines) {
        //         if (line.StartsWith("PPid")) {
        //             position = 0;
        //         } else if (line.StartsWith("Uid")) {
        //             position = 1;
        //         } else if (line.StartsWith("Gid")) {
        //             position = 2;
        //         }

        //         if (position != -1) {
        //             position = -1;
        //             ids[position] = System.Convert.ToInt32(line.Split('\t')[1]);
        //         }
        //     }

        //     return ids;
        // }

        protected void WriteSpec(File file, string[] contents) {
            using (ITextIO stream = Fs.Open(file.Path, AccessMode.O_WRONLY)) {
                stream.Write(string.Join("\0", contents));
            }
        }

        public File DataSource() {
            return Fs.Lookup("/proc");
        }
    }
}