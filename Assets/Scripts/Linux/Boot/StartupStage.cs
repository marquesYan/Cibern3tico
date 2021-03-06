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
using Linux.Library;

namespace Linux.Boot
{    
    public class StartupStage {
        protected Linux.Kernel Kernel;

        public StartupStage(Linux.Kernel kernel) {
            Kernel = kernel;
            Start();
        }

        protected void Print(string message) {
            // Terminal.Write($"[{DateTime.Now}] {message}");
            Thread.Sleep(500);
        }

        protected void Start() {
            // MakeFileSystem();
            // Print("virtual filesystem: created");

            // MakeLinuxDefaultDirectories();
            // int directoriesCount = Kernel.Fs.Root.ChildsCount();
            // Print($"filesystem structure has {directoriesCount} directories");

            MakeLinuxLibrary();
            // Print("file utilities: created");

            // MakeLinuxDev();
            // Print("devices: created");

            // MakeLinuxSys();
            // Print("sys: created");

            // MakeLinuxConfigurations();
            // Print("configurations: created");


            Kernel.ProcTable = new ProcessesTable(Kernel.Fs);
            // Print("process table: created");

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

        void MakeLinuxLibrary() {
            File systemBinDir = Kernel.Fs.LookupOrFail("/usr/sbin");
            File binDir = Kernel.Fs.LookupOrFail("/usr/bin");

            Kernel.Fs.AddFrom(
                binDir,
                new True(
                    "/usr/bin/true",
                    0, 0,
                    Perm.FromInt(7, 5, 5),
                    FileType.F_REG
                )
            );

            Kernel.Fs.AddFrom(
                binDir,
                new Rm(
                    "/usr/bin/rm",
                    0, 0,
                    Perm.FromInt(7, 5, 5),
                    FileType.F_REG
                )
            );

            Kernel.Fs.AddFrom(
                binDir,
                new Chown(
                    "/usr/bin/chown",
                    0, 0,
                    Perm.FromInt(7, 5, 5),
                    FileType.F_REG
                )
            );

            Kernel.Fs.AddFrom(
                binDir,
                new Mkdir(
                    "/usr/bin/mkdir",
                    0, 0,
                    Perm.FromInt(7, 5, 5),
                    FileType.F_REG
                )
            );

            Kernel.Fs.AddFrom(
                binDir,
                new Bash(
                    "/usr/bin/bash",
                    0, 0,
                    Perm.FromInt(7, 5, 5),
                    FileType.F_REG
                )
            );

            Kernel.Fs.AddFrom(
                binDir,
                new Kill(
                    "/usr/bin/kill",
                    0, 0,
                    Perm.FromInt(7, 5, 5),
                    FileType.F_REG
                )
            );

            Kernel.Fs.AddFrom(
                binDir,
                new Cat(
                    "/usr/bin/cat",
                    0, 0,
                    Perm.FromInt(7, 5, 5),
                    FileType.F_REG
                )
            );

            Kernel.Fs.AddFrom(
                binDir,
                new Ls(
                    "/usr/bin/ls",
                    0, 0,
                    Perm.FromInt(7, 5, 5),
                    FileType.F_REG
                )
            );

            Kernel.Fs.AddFrom(
                binDir,
                new Sshd(
                    "/usr/bin/sshd",
                    0, 0,
                    Perm.FromInt(7, 5, 5),
                    FileType.F_REG
                )
            );

            Kernel.Fs.AddFrom(
                binDir,
                new Ssh(
                    "/usr/bin/ssh",
                    0, 0,
                    Perm.FromInt(7, 5, 5),
                    FileType.F_REG
                )
            );

            Kernel.Fs.AddFrom(
                binDir,
                new SshCrack(
                    "/usr/bin/ssh-crack",
                    0, 0,
                    Perm.FromInt(7, 5, 5),
                    FileType.F_REG
                )
            );

            Kernel.Fs.AddFrom(
                binDir,
                new Cewl(
                    "/usr/bin/cewl",
                    0, 0,
                    Perm.FromInt(7, 5, 5),
                    FileType.F_REG
                )
            );

            Kernel.Fs.AddFrom(
                binDir,
                new PassCrack(
                    "/usr/bin/pass-crack",
                    0, 0,
                    Perm.FromInt(7, 5, 5),
                    FileType.F_REG
                )
            );

            Kernel.Fs.AddFrom(
                binDir,
                new SshKeygen(
                    "/usr/bin/ssh-keygen",
                    0, 0,
                    Perm.FromInt(7, 5, 5),
                    FileType.F_REG
                )
            );

            Kernel.Fs.AddFrom(
                binDir,
                new Tcpdump(
                    "/usr/bin/tcpdump",
                    0, 0,
                    Perm.FromInt(7, 5, 5),
                    FileType.F_REG
                )
            );

            Kernel.Fs.AddFrom(
                binDir,
                new Gpg(
                    "/usr/bin/gpg",
                    0, 0,
                    Perm.FromInt(7, 5, 5),
                    FileType.F_REG
                )
            );

            Kernel.Fs.AddFrom(
                binDir,
                new Curl(
                    "/usr/bin/curl",
                    0, 0,
                    Perm.FromInt(7, 5, 5),
                    FileType.F_REG
                )
            );

            Kernel.Fs.AddFrom(
                binDir,
                new Httpd(
                    "/usr/bin/httpd",
                    0, 0,
                    Perm.FromInt(7, 5, 5),
                    FileType.F_REG
                )
            );

            Kernel.Fs.AddFrom(
                binDir,
                new Nc(
                    "/usr/bin/nc",
                    0, 0,
                    Perm.FromInt(7, 5, 5),
                    FileType.F_REG
                )
            );

            Kernel.Fs.AddFrom(
                binDir,
                new Echo(
                    "/usr/bin/echo",
                    0, 0,
                    Perm.FromInt(7, 5, 5),
                    FileType.F_REG
                )
            );

            Kernel.Fs.AddFrom(
                binDir,
                new Find(
                    "/usr/bin/find",
                    0, 0,
                    Perm.FromInt(7, 5, 5),
                    FileType.F_REG
                )
            );

            Kernel.Fs.AddFrom(
                binDir,
                new Id(
                    "/usr/bin/id",
                    0, 0,
                    Perm.FromInt(7, 5, 5),
                    FileType.F_REG
                )
            );

            Kernel.Fs.AddFrom(
                binDir,
                new Tee(
                    "/usr/bin/tee",
                    0, 0,
                    Perm.FromInt(7, 5, 5),
                    FileType.F_REG
                )
            );

            Kernel.Fs.AddFrom(
                binDir,
                new Chmod(
                    "/usr/bin/chmod",
                    0, 0,
                    Perm.FromInt(7, 5, 5),
                    FileType.F_REG
                )
            );

            Kernel.Fs.AddFrom(
                systemBinDir,
                new Su(
                    "/usr/sbin/su",
                    0, 0,
                    Perm.FromInt(7, 5, 5),
                    FileType.F_REG
                )
            );

            Kernel.Fs.AddFrom(
                systemBinDir,
                new Login(
                    "/usr/sbin/login",
                    0, 0,
                    Perm.FromInt(7, 5, 5),
                    FileType.F_REG
                )
            );

            Kernel.Fs.AddFrom(
                systemBinDir,
                new Poweroff(
                    "/usr/sbin/poweroff",
                    0, 0,
                    Perm.FromInt(7, 5, 5),
                    FileType.F_REG
                )
            );

            Kernel.Fs.AddFrom(
                systemBinDir,
                new Init(
                    "/usr/sbin/init",
                    0, 0,
                    Perm.FromInt(7, 5, 5),
                    FileType.F_REG
                )
            );
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