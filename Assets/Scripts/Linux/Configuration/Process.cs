using System.IO;
using System.Threading;
using System.Collections.Generic;
using Linux.FileSystem;

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
        public List<string> ChildPids { get; protected set; }
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
            ChildPids = new List<string>();
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
        object procLock = new object();

        protected FileTree Fs;

        protected List<Process> Processes;

        public ProcessesTable(FileTree fs) {
            Fs = fs;
            Processes = new List<Process>();
        }

        public void Add(Process process) {
            lock(procLock) {
                if (LookupPid(process.Pid) != null) {
                    throw new System.ArgumentException("Process already exists");
                }

                Processes.Add(process);
                // ProcessToFile(process);
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
                        pid = Processes[Processes.Count - 1].Pid;
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

        public Process LookupPid(int pid) {
            return Processes.Find(p => p.Pid == pid);
        }

        // protected List<Process> LoadFromFs() {
        //     List<Process> processes = new List<Process>();

        //     foreach (AbstractFile file in DataSource()?.Childs) {
        //         Process process = FileToProcess(file);
        //         if (process != null) {
        //             processes.Add(process);
        //         }
        //     }

        //     return processes;
        // }

        void ProcessToFile(Process process, string directory) {
            string path = Fs.Combine(directory, $"{process.Pid}");

            LinuxDirectory procDirectory = new LinuxDirectory(
                path,                
                process.Uid,
                process.Gid,
                Perm.FromInt(5, 5, 5)
            );

            Fs.AddFrom(DataSource(), procDirectory);

            // AbstractFile cmdLineFile = new AbstractFile(
            //     Fs.Combine(path, "cmdline"),
            //     process.Uid,
            //     process.Gid,
            //     Perm.FromInt(4, 4, 4)
            // );

            // AbstractFile environFile = new AbstractFile(

            // );
        }

        // Process FileToProcess(AbstractFile file) {
        //     int pid;

        //     if (file.IsDirectory() && int.TryParse(file.Name, out pid)) {
        //         LinuxDirectory procDirectory = (LinuxDirectory) file;
                
        //         AbstractFile cmdLineFile = procDirectory.Childs.Find(c => c.Name == "cmdline");
        //         string[] cmdLine = cmdLineFile?.Read().Split('\0');
                
        //         AbstractFile environFile = procDirectory.Childs.Find(c => c.Name == "environ");
        //         string[] environ = environFile?.Read().Split('\0');

        //         AbstractFile cwdFile = procDirectory.Childs.Find(c => c.Name == "cwd");
        //         AbstractFile rootFile = procDirectory.Childs.Find(c => c.Name == "root");

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

        int[] ParseIdsFromProcessFile(LinuxDirectory procDirectory) {
            AbstractFile statusFile = procDirectory.Childs.Find(c => c.Name == "status");
            string[] lines = statusFile?.Read().Split('\n');

            int[] ids = new int[3];

            int position = -1;

            foreach(string line in lines) {
                if (line.StartsWith("PPid")) {
                    position = 0;
                } else if (line.StartsWith("Uid")) {
                    position = 1;
                } else if (line.StartsWith("Gid")) {
                    position = 2;
                }

                if (position != -1) {
                    position = -1;
                    ids[position] = System.Convert.ToInt32(line.Split('\t')[1]);
                }
            }

            return ids;
        }

        public LinuxDirectory DataSource() {
            return (LinuxDirectory) Fs.Lookup("/proc");
        }
    }
}