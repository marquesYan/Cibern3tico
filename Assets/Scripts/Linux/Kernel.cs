using System;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using Linux.Boot;
using Linux.Configuration;
using Linux.IO;
using Linux.Sys;
using Linux.Sys.Net;
using Linux.Sys.Drivers;
using Linux.Sys.Input;
using Linux.Sys.Input.Drivers.Tty;
using Linux.Sys.IO;
using Linux.FileSystem;
using Linux.PseudoTerminal;
using Linux.Utilities;
using Linux.Library;
using Linux.Sys.RunTime;

namespace Linux
{
    public class Kernel {
        readonly object _lock = new object();

        public const string Version = "0.0.1.x86_64"; 

        public UnityTerminal Terminal { get; protected set; }
        public VirtualMachine Machine { get; protected set; }

        // public bool IsShutdown { get; protected set; }
    
        public VirtualFileTree Fs;
        public ProcessesTable ProcTable;
        public UsersDatabase UsersDb;
        public GroupsDatabase GroupsDb;
        public PeripheralsTable PciTable;
        public UdevTable EventTable;

        public NetworkTable NetTable;

        public ArpTable ArpTable;

        public ShadowDatabase ShadowDb;

        public CommandHandler CmdHandler;

        public ConcurrentDictionary<string, Action<UEvent>> PostInterruptHooks;

        public PseudoTerminalTable PtyTable;

        public ProcessSignalsTable ProcSigTable;
        public string PersistentPath { get; protected set; }

        public string DataPath { get; protected set; }

        public string HostName { get; protected set; }

        protected Process InitProcess;

        protected bool IsShutdown;

        protected bool ShutdownScheduled;

        float _bootDelay = 0.0001f;

        public static bool IsRunning { get; protected set; }

        public Kernel(
            string persistentPath,
            string dataPath,
            VirtualMachine machine,
            string hostName
        ) {
            PersistentPath = persistentPath;
            DataPath = dataPath;
            Machine = machine;
            HostName = hostName;

            IsRunning = true;
            IsShutdown = false;
            ShutdownScheduled = false;

            PostInterruptHooks = new ConcurrentDictionary<string, Action<UEvent>>();
            new DecompressStage(this);
        }

        public void Bootstrap() {
            new StartupStage(this);

            NetTable = new NetworkTable(Fs);
            ArpTable = new ArpTable();

            PciTable = new PeripheralsTable(Fs);
            // Print("pci table: created");

            FindPeripheralComponents();
            // Print("found peripherals");

            MountDevFs();

            EventTable = new UdevTable(Fs);
            FindBiosDrivers();

            var ttyDriver = new GenericPtyDriver(this);

            PtyTable = new PseudoTerminalTable(Fs, ttyDriver);
            // FindControllingTty();

            ProcSigTable = new ProcessSignalsTable();

            CmdHandler = new CommandHandler(this);

            Init();
        }

        public void ScheduleShutdown() {
            ShutdownScheduled = true;
        }

        public void Shutdown() {
            lock(_lock) {
                if (!IsShutdown) {
                    InternalShutdown();
                    IsShutdown = true;
                }
            }
            
        }

        public void Interrupt(Pci pci, IRQCode code) {
            lock(_lock) {
                if (ShutdownScheduled) {
                    Shutdown();
                }

                if (IsShutdown) {
                    return;
                }
            }

            UEvent uEvent = EventTable.LookupByPci(pci);

            if (uEvent != null) {
                uEvent.Driver.Handle(code);

                foreach (Action<UEvent> hook in PostInterruptHooks.Values) {
                    hook(uEvent);
                }
            }
        }

        public void KillAllChildProcesses(ProcessSignal signal) {
            int[] childPids = InitProcess.ChildPids.ToArray();

            foreach (int pid in childPids) {
                Process process = ProcTable.LookupPid(pid);
                if (process != null) {
                    try {
                        KillProcess(process, signal);
                    } catch (System.TimeoutException exception) {
                        Debug.Log($"process {process.Pid}: {exception.Message}");
                    }
                }
            }
        }

        public void KillProcess(Process process) {
            KillProcess(process, ProcessSignal.SIGTERM);
        }

        public void KillProcess(Process process, ProcessSignal signal) {
            int[] childPids = process.ChildPids.ToArray();
            
            foreach (int pid in childPids) {
                Process child = ProcTable.LookupPid(pid);
                if (child != null) {
                    KillProcess(child, signal);
                }
            }

            switch(signal) {
                case ProcessSignal.SIGKILL: {
                    process.MainTask.Abort();
                    break;
                }

                default: {
                    ProcSigTable.Dispatch(process, signal);
                    break;
                }
            }

            ProcTable.Remove(process);
        }

        void InternalShutdown() {
            Debug.Log("Shutting down");
            IsRunning = false;

            EventTable.Close();

            Debug.Log("childs: " +string.Join(",", InitProcess.ChildPids));

            ProcSigTable.Dispatch(InitProcess, ProcessSignal.SIGHUP);

            while (InitProcess.ChildPids.Count > 0) {
                Debug.Log("waiting shutdown on kernel processes");
                Thread.Sleep(1000);
            }

            // Try gracefully
            KillProcess(InitProcess);

            if (!WaitProcessUntil(InitProcess, 15)) {
                // Go hard
                KillProcess(InitProcess, ProcessSignal.SIGKILL);
            }

            Thread.Sleep(1000);

            ProcTable.Close();
        }

        bool WaitProcessUntil(Process process, int seconds) {
            int maxAttempts = seconds;
            int attempt = 0;

            while (process.MainTask.IsAlive && attempt < maxAttempts) {
                Debug.Log("waiting process to finish");
                Thread.Sleep(1000);
                attempt++;
            }
            
            return !process.MainTask.IsAlive;
        }

        void MountDevFs() {
            // Create Input Devices directory
            Fs.CreateDir(
                "/dev",
                0, 0,
                Perm.FromInt(7, 5, 5)
            );

            Fs.Create(
                "/dev/null",
                0, 0,
                Perm.FromInt(6, 6, 6),
                FileType.F_CHR,
                new DevNull()
            );
        }

        void FindPeripheralComponents() {
            foreach(Pci pci in Machine.Chassis.Keys) {
                PciTable.Add(pci);
            }
        }

        void FindBiosDrivers() {
            foreach(KeyValuePair<Pci, GenericDevice> kvp in Machine.Chassis) {
                Pci pci = kvp.Key;
                GenericDevice device = kvp.Value;

                foreach(IPciDriver driver in Machine.BiosDrivers) {
                    if (driver.IsSupported(pci)) {
                        IDeviceDriver devDriver = driver.FindDevDriver(device);
                        
                        if (devDriver != null) {
                            if (devDriver is IUdevDriver) {
                                EventTable.Add(pci, device, (IUdevDriver)devDriver);
                            } else if (devDriver is INetDriver) {
                                NetTable.Add((INetDriver)devDriver);
                            }
                        }
                    }
                }
            }
        }

        public Process CreateProcess(
            int ppid,
            User user,
            string[] cmdLine,
            int umask,
            Dictionary<string, string> environ,
            int stdin,
            int stdout,
            int stderr
        ) {
            Process parent = ProcTable.LookupPid(ppid);

            Process process = BuildProcess(
                ppid,
                user,
                cmdLine,
                umask,
                environ
            );

            ProcTable.DuplicateFd(parent, stdin, process, 0);
            ProcTable.DuplicateFd(parent, stdout, process, 1);
            ProcTable.DuplicateFd(parent, stderr, process, 2);

            return process;
        }

        Process BuildProcess(
            int ppid,
            User user,
            string[] cmdLine,
            int umask,
            Dictionary<string, string> environ
        ) {
            Thread mainTask = new Thread(
                new ThreadStart(CmdHandler.Handle)
            );

            Dictionary<string, string> defaultEnviron = new Dictionary<string, string>();
            defaultEnviron.Add("USER", user.Login);
            defaultEnviron.Add("SHELL", user.Shell);
            defaultEnviron.Add("PWD", user.HomeDir);
            defaultEnviron.Add("HOME", user.HomeDir);

            foreach (KeyValuePair<string, string> kvp in environ) {
                defaultEnviron[kvp.Key] = kvp.Value;
            }

            Process process = ProcTable.Create(
                ppid,
                user.Uid,
                user.Gid,
                umask,
                cmdLine,
                defaultEnviron,
                Fs.Root.Path,
                defaultEnviron["PWD"],
                mainTask
            );

            return process;
        }

        void Init() {
            User root = UsersDb.LookupUid(0);

            InitProcess = BuildProcess(
                0,
                root,
                new string[] { "/usr/sbin/init" },
                Perm.FromInt(0, 2, 2),
                new Dictionary<string, string>()
            );

            var devNull = "/dev/null";

            ProcTable.AttachIO(InitProcess, devNull, AccessMode.O_RDONLY, 0);
            ProcTable.AttachIO(InitProcess, devNull, AccessMode.O_WRONLY, 1);
            ProcTable.AttachIO(InitProcess, devNull, AccessMode.O_WRONLY, 2);

            InitProcess.MainTask.Start();
        }

        string FakeBootFile() {
            return System.IO.Path.Combine(Application.dataPath, "Resources", "boot.txt"); 
        }
    }
}