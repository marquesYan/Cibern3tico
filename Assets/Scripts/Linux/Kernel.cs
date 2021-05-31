using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using Linux.Boot;
using Linux.Configuration;
using Linux.Sys;
using Linux.Sys.Drivers;
using Linux.Sys.Input;
using Linux.Sys.IO;
using Linux.FileSystem;
using Linux.PseudoTerminal;
using Linux.Utilities;

namespace Linux
{
    public class Kernel {
        public const string Version = "0.0.1.x86_64"; 

        public UnityTerminal Terminal { get; protected set; }
        public VirtualMachine Machine { get; protected set; }
    
        public VirtualFileTree Fs;
        public ProcessesTable ProcTable;
        public UsersDatabase UsersDb;
        public GroupsDatabase GroupsDb;
        public PeripheralsTable PciTable;
        public UdevTable EventTable;
        public UEvent MasterKbdEvent;
        public UEvent MasterConsoleEvent;

        float _bootDelay = 0.0001f;

        public Kernel(VirtualMachine machine) {
            Machine = machine;
            new DecompressStage(this);
        }

        public void Bootstrap() {
            PciTable = new PeripheralsTable(Fs);
            // Print("pci table: created");

            FindPeripheralComponents();
            // Print("found peripherals");

            EventTable = new UdevTable(Fs);
            FindBiosDrivers();

            FindControllingTty();

            TriggerStartup();
        }

        public void Interrupt(Pci pci, IRQCode code) {
            UEvent uEvent = EventTable.LookupByPci(pci);

            if (uEvent != null) {
                uEvent.Driver.Handle(code);
            }
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
            UEvent kbdEvent = EventTable.LookupByType(DevType.KEYBOARD);
            if (kbdEvent != null) {
                MasterKbdEvent = kbdEvent;
            }

            UEvent consoleEvent = EventTable.LookupByType(DevType.CONSOLE);
            if (consoleEvent != null) {
                MasterConsoleEvent = consoleEvent;
            }

            if (MasterConsoleEvent != null && MasterKbdEvent != null) {
                //   
            }
        }

        void TriggerStartup() {
            var startup = new Thread(new ThreadStart(() => {
                // new StartupStage(this);
                while (true) {
                    string key = MasterKbdEvent.DevPointer.Read();
                    // Debug.Log("recv key: " + key);
                    string wKey = $"{CharacterControl.C_WRITE_KEY}{key}";
                    MasterConsoleEvent.DevPointer.Write(key);
                }
                // Init(); 
            }));

            startup.Start();
        }

        public Process StartProcess(
            int ppid,
            User user,
            string[] cmdLine
        ) {
            if (cmdLine.Length == 0) {
                throw new System.ArgumentException("No command line");
            }

            File executable = Fs.Lookup(cmdLine[0]);

            if (executable == null) {
                throw new System.ArgumentException("Command not found: " + cmdLine[0]);
            }

            Thread mainTask = new Thread(
                new ThreadStart(new CommandInterpreter(this).Handle)
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

            process.MainTask.Start();

            return process;
        }

        void Init() {
            var utility = new TestUtility();

            Fs.AddFrom(Fs.Lookup("/usr/sbin"), utility);

            User root = UsersDb.LookupUid(0);

            StartProcess(
                0,
                root,
                new string[] { "/usr/sbin/init" }
            );
        }

        // void HandleTerm() {
        //     string login = Terminal.Input("Login:");
        //     Debug.Log("Login: " + login);
        //     string password = Terminal.Input("Password:");
        //     Debug.Log("Password: " + password);
        // }

        string FakeBootFile() {
            return System.IO.Path.Combine(Application.dataPath, "Resources", "boot.txt"); 
        }
    }
}