using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Linux.FileSystem;
using Linux.Utilities;
using Linux.Utilities.Sbin;
using Linux.Devices;
using Linux.Devices.Input;
using Linux.Sys.Input.Drivers;
using Linux.Configuration;
using Linux.IO;
using Linux;

namespace Linux
{    
    public class Kernel {
        public VirtualFileTree Fs { get; protected set; }
        public UnityTerminal Terminal { get; protected set; }
    
        public AbstractInputDriver InputDriver { get; protected set; }

        public ProcessesTable ProcTable { get; protected set; }
        public UsersDatabase UsersDb { get; protected set; }
        public GroupsDatabase GroupsDb { get; protected set; }

        public CommandInterpreter CmdInterpreter { get; protected set; }

        public readonly string Version = "5.4.98-1.fc25.x86_64";

        MonoBehaviour _gameObject;

        int _bufferSize = 512;

        float _bootDelay = 0.0001f;

        // public Kernel(MonoBehaviour gameObject) {
        //     _gameObject = gameObject;
        // }

        // public Coroutine StartCoroutine(IEnumerator coro) {
        //     return _gameObject.StartCoroutine(coro);
        // }

        public void Bootstrap() {
            Fs = new VirtualFileTree(
                new Directory(
                    "/",
                    0,
                    0,
                    Perm.FromInt(7, 5, 5)
                )
            );

            MakeLinuxDefaultDirectories();
            MakeLinuxUtilities();
            MakeLinuxDev();
            MakeLinuxConfigurations();

            ProcTable = new ProcessesTable(Fs);
            UsersDb = new UsersDatabase(Fs);
            GroupsDb = new GroupsDatabase(Fs);
            CmdInterpreter = new CommandInterpreter(this);

            MakeSystemUsers();
            Init();
            
            // Will handle unity inputs
            // InputDriver = new UnityInputDriver(this);

            // Terminal.SubscribeFirstDraw(
            //     () => {
            //         Debug.Log("terminal is ready");
            //         Init();
            //     }
            // );
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

            Thread mainTask = new Thread(new ThreadStart(CmdInterpreter.Handle));

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

            Fs.AddFrom((Directory)Fs.Lookup("/usr/sbin"), utility);

            User root = UsersDb.LookupUid(0);
            Debug.Log("Root: " + root);

            StartProcess(
                0,
                root,
                new string[] { "/usr/sbin/init" }
            );
        }

        void HandleTerm() {
            string login = Terminal.Input("Login:");
            Debug.Log("Login: " + login);
            string password = Terminal.Input("Password:");
            Debug.Log("Password: " + password);
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

        void MakeSystemUsers() {
            UsersDb.Add(new User(
                "root",
                0, 0,
                "",
                "/root",
                "/usr/bin/bash"
            ));

            UsersDb.Add(new User(
                "bin",
                1, 1,
                "",
                "/usr/bin",
                "/usr/sbin/nologin"
            ));
        }

        void MakeLinuxDefaultDirectories() {
            string[] dir755 = new string[]{
                "/dev",
                "/home",
                "/mnt",
                "/usr",
                "/etc",
                "/media",
                "/opt",
                "/var",
            };

            string[] dir555 = new string[] { 
                "/proc",
                "/sys",
            };

            int perm755 = Perm.FromInt(7, 5, 5);
            int perm555 = Perm.FromInt(5, 5, 5);
            
            foreach (string path in dir755) {
                Fs.Add(new Directory(path, 0, 0, perm755));
            }

            foreach (string path in dir555) {
                Fs.Add(new Directory(path, 0, 0, perm555));
            }

            Fs.Add(new Directory("/root", 0, 0, Perm.FromInt(5, 5, 0)));
        }

        void MakeLinuxUtilities() {
            Directory usrDirectory = (Directory) Fs.Lookup("/usr");

            int binPerm = Perm.FromInt(7, 5, 5);

            Directory binDirectory = new Directory(
                "/usr/bin", 
                0, 0, 
                binPerm
            );

            Directory sbinDirectory = new Directory(
                "/usr/sbin",
                0, 0,
                binPerm
            );

            Fs.AddFrom(usrDirectory, binDirectory);
            Fs.AddFrom(usrDirectory, sbinDirectory);

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

        void MakeLinuxDev() {
            Directory devDirectory = (Directory) Fs.Lookup("/dev");

            // Create Input Devices directory
            Directory inputDevDirectory = new Directory(
                "/dev/input",
                0, 0,
                Perm.FromInt(7, 5, 5)
            );

            Fs.AddFrom(devDirectory, inputDevDirectory);

            // EventFile kbEventFile = new EventFile(
            //     "/dev/input/event0",
            //     0, 0, 
            //     Perm.FromInt(6, 6, 0)
            // );

            // // Allocate terminal
            // Terminal = new UnityTerminal(_bufferSize, kbEventFile);

            // Fs.AddFrom(inputDevDirectory, kbEventFile);

            // Debug.Log("created kb input: " + Fs.Lookup("/dev/input/event0").Name);

            // // Create TTY
            // int devPerm = Perm.FromInt(5, 2, 0);

            // File[] devices = new File[] {
            //     new TtyDevice(Terminal, "/dev/tty", 0, 0, Perm.FromInt(6, 6, 6)),
            //     new ConsoleDevice(FakeBootFile(), "/dev/console", 0, 0, Perm.FromInt(6, 0, 0)),
            // };

            // foreach (File dev in devices) {
            //     Fs.AddFrom(devDirectory, dev);
            // }
        }

        void MakeLinuxConfigurations() {
            Directory configDirectory = (Directory) Fs.Lookup("/etc");

            int configPerm = Perm.FromInt(7, 5, 5);

            var files = new string[] {
                "/etc/group",
                "/etc/passwd",
                "/etc/hosts"
            };

            foreach(string path in files) {
                Fs.AddFrom(
                    configDirectory, 
                    new File(
                        path,
                        0, 0,
                        configPerm
                    )
                );
            }
        }

        string FakeBootFile() {
            return System.IO.Path.Combine(Application.dataPath, "Resources", "boot.txt"); 
        }
    }
}