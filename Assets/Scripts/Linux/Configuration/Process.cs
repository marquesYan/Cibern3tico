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
        public string[] Environ { get; protected set; }
        public Thread MainTask { get; protected set; }
        public List<Thread> BackgroundTasks { get; protected set; }
        public List<int> ChildPids { get; protected set; }
        public List<int> Fds { get; protected set; }
        public string Cwd { get; protected set; }
        public string Root { get; protected set; }

        public Process(
            int ppid,
            int pid,
            int uid,
            int gid,
            string[] cmdLine,
            string[] environ,
            string root,
            string cwd,
            Thread mainTask
        ) {
            PPid = ppid;
            Pid = pid;
            Uid = uid;
            Gid = gid;
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

        public Process CreateThread(Thread childTask) {
            Process task = new Process(
                PPid,
                Pid,
                Uid,
                Gid,
                CmdLine,
                Environ,
                Root,
                Cwd,
                MainTask
            );

            task.BackgroundTasks.Add(childTask);

            return task;
        }
    }

    public class ProcessesTable {
        readonly object procLock = new object();

        protected VirtualFileTree Fs;

        protected List<Process> Processes;

        public ProcessesTable(VirtualFileTree fs) {
            Fs = fs;
            Processes = new List<Process>();
        }

        public void Add(Process process) {
            lock(procLock) {
                if (LookupPid(process.Pid) != null) {
                    throw new System.ArgumentException("Process already exists");
                }

                Processes.Add(process);
                ProcessToFile(process);
            }
        }

        public Process Create(
            int ppid,
            int uid,
            int gid,
            string[] cmdLine,
            string[] environ,
            string root,
            string cwd,
            Thread mainTask
        ) {
            int pid;

            lock(procLock) {
                if (Processes.Count == 0) {
                    pid = 1;
                } else {
                    do {
                        pid = Processes[Processes.Count - 1].Pid + 1;
                    } while (LookupPid(pid) != null);
                }
            }

            Process process = new Process(
                ppid,
                pid,
                uid,
                gid,
                cmdLine,
                environ,
                root,
                cwd,
                mainTask
            );

            Add(process);

            return process;
        }

        public void AttachFileDescriptor(
            Process process,
            string filePath,
            int fd
        ) {
            File file = Fs.LookupOrFail(filePath);

            if (!Processes.Contains(process)) {
                throw new System.ArgumentException(
                    "Process not exists"
                );
            }

            Fs.CreateSymbolicLink(
                file,
                PathUtils.Combine(
                    ProcessDirectory(process), 
                    "fd",
                    fd.ToString()
                ),
                Perm.FromInt(7, 0, 0)
            );

            process.Fds.Add(fd);
        }

        public Process LookupPid(int pid) {
            return Processes.Find(p => p.Pid == pid);
        }

        public Process LookupThread(Thread thread) {
            return Processes.Find(p => p.MainTask == thread);
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
            WriteSpec(environFile, process.Environ);
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