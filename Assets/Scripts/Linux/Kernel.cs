using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using Linux.Boot;
using Linux.Configuration;
using Linux.IO;
using Linux.Sys;
using Linux.Sys.Drivers;
using Linux.Sys.Input;
using Linux.Sys.Input.Drivers;
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

            TriggerStartup();
        }

        public void Interrupt(Pci pci, IRQCode code) {
            UEvent uEvent = EventTable.LookupByPci(pci);

            if (uEvent != null) {
                uEvent.Driver.Handle(code);
            }
        }

        void MountDevFs() {
            // Create Input Devices directory
            Fs.CreateDir(
                "/dev",
                0, 0,
                Perm.FromInt(7, 5, 5)
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

        void FindControllingTty() {
            // UEvent consoleEvent = EventTable.LookupByType(DevType.CONSOLE);

            // if (kbdEvent != null && consoleEvent != null) {
            //     var controllingPty = new PrimaryPty(
            //         Fs.Open(kbdEvent.FilePath, AccessMode.O_RDONLY),
            //         Fs.Open(consoleEvent.FilePath, AccessMode.O_WRONLY)
            //     );

            //     PtyTable.SetControllingPty(controllingPty);
            // }
        }

        void TriggerStartup() {
            CmdHandler = new CommandHandler(this);

            new StartupStage(this);

            Init();
        }

        public Process StartProcess(
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

            File ptsFile = PtyTable.Add(user);

            ProcTable.AttachFileDescriptor(process, ptsFile.Path, 0);
            ProcTable.AttachFileDescriptor(process, ptsFile.Path, 1);
            ProcTable.AttachFileDescriptor(process, ptsFile.Path, 2);
            ProcTable.AttachFileDescriptor(process, ptsFile.Path, 255);

            process.MainTask.Start();

            return process;
        }

        void Init() {
            User root = UsersDb.LookupUid(0);

            StartProcess(
                0,
                root,
                new string[] { "/usr/sbin/init" }
            );
        }

        string FakeBootFile() {
            return System.IO.Path.Combine(Application.dataPath, "Resources", "boot.txt"); 
        }
    }
}