using System;
using System.Threading;
using Linux.Configuration;
using Linux.Devices;
using Linux.Devices.Input;
using Linux.IO;
using Linux.FileSystem;
using Linux.PseudoTerminal;
using Linux.Sys;
using Linux.Sys.Input.Drivers;
using Linux.Utilities.Sbin;
using Linux.Utilities;

namespace Linux.Boot
{    
    public class StartupStage {
        protected Linux.Kernel Kernel;
        protected VirtualTerminal Terminal;

        public StartupStage(Linux.Kernel kernel) {
            Kernel = kernel;
            Terminal = Kernel.Terminal;
            Start();
        }

        protected void Print(string message) {
            Terminal.Write($"[{DateTime.Now}] {message}");
            Thread.Sleep(500);
        }

        protected void Start() {
            // MakeFileSystem();
            // Print("virtual filesystem: created");

            // MakeLinuxDefaultDirectories();
            // int directoriesCount = Kernel.Fs.Root.ChildsCount();
            // Print($"filesystem structure has {directoriesCount} directories");

            MakeLinuxUtilities();
            Print("file utilities: created");

            // MakeLinuxDev();
            // Print("devices: created");

            MakeLinuxSys();
            Print("sys: created");

            // MakeLinuxConfigurations();
            // Print("configurations: created");


            Kernel.ProcTable = new ProcessesTable(Kernel.Fs);
            Print("process table: created");

            // Init();
            
            // Will handle unity inputs
            // InputDriver = new UnityInputDriver(this);

            // Terminal.SubscribeFirstDraw(
            //     () => {
            //         Debug.Log("terminal is ready");
            //         Init();
            //     }
            // );
        }

        void MakeLinuxUtilities() {
            int binPerm = Perm.FromInt(7, 5, 5);

            Kernel.Fs.CreateDir(
                "/usr/bin",
                0, 0, 
                binPerm
            );

            Kernel.Fs.CreateDir(
                "/usr/sbin",
                0, 0,
                binPerm
            );

            // File[] binaries = new File[] { 
            //     new LsUtility("/usr/bin/ls", 0, 0, binPerm),
            // };

            // foreach (File utility in binaries) {
            //     Fs.AddFrom(binDirectory, utility);
            // }

            // File[] sytemBinaries = new File[] { 
            //     new InitUtility("/usr/sbin/init", 0, 0, binPerm),
            // };

            // foreach (File utility in sytemBinaries) {
            //     Fs.AddFrom(sbinDirectory, utility);
            // }
        }

        void MakeLinuxSys() {
            int sysPerm = Perm.FromInt(7, 5, 5);

            var files = new string[] {
                "/sys/class",
                "/sys/devices",
            };

            foreach(string path in files) {
                Kernel.Fs.CreateDir(
                    path,
                    0, 0,
                    sysPerm
                );
            }
        }

        // void FindPeripheralComponents() {
        //     var usb1 = new Pci(
        //         "xHCI Host Controller",
        //         "SAD",
        //         "0000:00:04.0",
        //         189,
        //         122,
        //         PciClass.INPUT
        //     );

        //     Kernel.PciTable.Add(usb1);
        //     Print($"found {usb1.Product} at: {usb1.Slot}");

        //     var usb2 = new Pci(
        //         "xHCI Host Controller",
        //         "SAD",
        //         "0000:00:06.0",
        //         189,
        //         122,
        //         PciClass.INPUT
        //     );

        //     Kernel.PciTable.Add(usb2);
        //     Print($"found {usb2.Product} at: {usb2.Slot}");
        // }

         // IEnumerator Initialize() {
        //     Debug.Log("Initilizing Subsystem");
        //     yield return new WaitForSeconds(1);
        //     yield return StartCoroutine(LoadLinuxSystem());
        //     yield return StartCoroutine(LoadLoginSystem());
        // }

        // IEnumerator LoadLinuxSystem() {
        //     yield return new WaitForSeconds(1);

        //     Terminal.Write("[    0.000000] Linux version " + Version + 
        //               " (user@build-fedora4) (gcc version 6.4.1 20170727 (Red Hat 6.4.1-1) " +
        //               "(GCC)) #1 SMP Wed Feb 17 01:49:26 UTC 2021");

        //     yield return new WaitForSeconds(2);

        //     string[] lines = new string[1];
        //     // string[] lines = Fs.Lookup("/dev/console")?.Read().Split('\n');

        //     float lineDelay;

        //     for(int i = 0; i < 20; i++) {
        //         string line = lines[i];

        //         Terminal.Write(line);
        //         Terminal.RequestHighYAxis();

        //         lineDelay = i * _bootDelay;

        //         yield return new WaitForSeconds(Random.Range(0.01f - lineDelay, 0.1f - lineDelay));
        //     }
        // }
        
        // IEnumerator LoadLoginSystem() {
        //     Terminal.Write("Initializing login");
        //     yield return new WaitForSeconds(2);

        //     Terminal.ClearBuffer();

        //     Terminal.Write("");
        //     Terminal.Write("Fedora 32 (Thirty Two)");
        //     Terminal.Write("Kernel " + Version + " on an x86_64 (tty)");
        //     Terminal.Write("");
            
        //     string login = Terminal.Input("hacker1.localdomain login:");
        //     Debug.Log("Login: " + login);

        //     string password = Terminal.Input("Password:");
        //     Debug.Log("Password: " + password);
        // }
    }
}