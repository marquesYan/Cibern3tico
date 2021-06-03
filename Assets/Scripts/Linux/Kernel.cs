using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using Linux.Boot;
using Linux.Configuration;
using Linux.IO;
using Linux.Sys;
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
        public const string Version = "0.0.1.x86_64"; 

        public UnityTerminal Terminal { get; protected set; }
        public VirtualMachine Machine { get; protected set; }

        public bool IsShutdown { get; protected set; }
    
        public VirtualFileTree Fs;
        public ProcessesTable ProcTable;
        public UsersDatabase UsersDb;
        public GroupsDatabase GroupsDb;
        public PeripheralsTable PciTable;
        public UdevTable EventTable;
        public CommandHandler CmdHandler;
        
        public PseudoTerminalTable PtyTable;

        public ProcessSignalsTable ProcSigTable;

        protected Process InitProcess;

        float _bootDelay = 0.0001f;

        public Kernel(VirtualMachine machine) {
            Machine = machine;
            IsShutdown = false;
            new DecompressStage(this);
        }

        public void Bootstrap() {
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

            TriggerStartup();
        }

        public void Shutdown() {
            Debug.Log("Shutting down");
            IsShutdown = true;

            EventTable.Close();

            Debug.Log("childs: " +string.Join(",", InitProcess.ChildPids));

            ProcSigTable.Dispatch(InitProcess, ProcessSignal.SIGHUP);

            while (InitProcess.ChildPids.Count > 0) {
                Debug.Log("waiting shutdown on kernel processes");
                Thread.Sleep(1000);
            }

            try {
                KillProcess(InitProcess);
            } catch (System.TimeoutException) {
                try {
                    KillProcess(InitProcess, ProcessSignal.SIGKILL);
                } catch {
                    //
                }
            }

            ProcTable.Close();
        }

        public void Interrupt(Pci pci, IRQCode code) {
            UEvent uEvent = EventTable.LookupByPci(pci);

            if (uEvent != null) {
                uEvent.Driver.Handle(code);
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
            Debug.Log("will kill the process pid: " + process.Pid);

            process.ChildPids.ForEach(pid => {
                Process child = ProcTable.LookupPid(pid);
                if (child != null) {
                    KillProcess(child, signal);
                }
            });

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

            int maxAttempts = 10;
            int attempt = 0;

            while (process.MainTask.IsAlive && attempt < maxAttempts) {
                Debug.Log("waiting process to finish");
                Thread.Sleep(500);
                attempt++;
            }

            if (process.MainTask.IsAlive) {
                throw new System.TimeoutException(
                    "Failed to kill process, waited too much"
                );
            }

            ProcTable.Remove(process);
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
                        
                        if (devDriver != null && devDriver is IUdevDriver) {
                            EventTable.Add(pci, device, (IUdevDriver)devDriver);
                        }
                    }
                }
            }
        }

        void TriggerStartup() {
            CmdHandler = new CommandHandler(this);

            new StartupStage(this);

            Init();
        }

        public Process CreateProcess(
            int ppid,
            User user,
            string[] cmdLine,
            int stdin,
            int stdout,
            int stderr
        ) {
            Process parent = ProcTable.LookupPid(ppid);

            Process process = BuildProcess(
                ppid,
                user,
                cmdLine
            );

            ProcTable.DuplicateFd(parent, stdin, process, 0);
            ProcTable.DuplicateFd(parent, stdout, process, 1);
            ProcTable.DuplicateFd(parent, stderr, process, 2);

            return process;
        }

        Process BuildProcess(
            int ppid,
            User user,
            string[] cmdLine
        ) {
            Thread mainTask = new Thread(
                new ThreadStart(CmdHandler.Handle)
            );

            Process process = ProcTable.Create(
                ppid,
                user.Uid,
                user.Gid,
                cmdLine,
                new string[1],
                Fs.Root.Path,
                user.HomeDir,
                mainTask
            );

            return process;
        }

        void Init() {
            User root = UsersDb.LookupUid(0);

            InitProcess = BuildProcess(
                0,
                root,
                new string[] { "/usr/sbin/init" }
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