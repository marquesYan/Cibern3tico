using System.Threading;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Linux.FileSystem;
using Linux.Utilities;
using Linux.Devices;
using Linux.Devices.Input;
using Linux;

namespace Linux
{    
    public class Kernel {
        public FileTree Fs { get; protected set; }
        public UnityTerminal Terminal { get; protected set; }
    
        public readonly string Version = "5.4.98-1.fc25.x86_64";

        MonoBehaviour _gameObject;

        int _bufferSize = 512;

        float _bootDelay = 0.0001f;

        public Kernel(MonoBehaviour gameObject) {
            _gameObject = gameObject;
        }

        public Coroutine StartCoroutine(IEnumerator coro) {
            return _gameObject.StartCoroutine(coro);
        }

        public void Bootstrap() {
            Terminal = new UnityTerminal(_bufferSize);

            Terminal.SubscribeFirstDraw(
                () => {
                    Debug.Log("terminal is ready");
                    StartCoroutine(Initialize());
                    Init();
                }
            );

            Fs = new FileTree(
                new LinuxDirectory(
                    "/",
                    0,
                    0,
                    Perm.FromInt(7, 5, 5)
                )
            );

            MakeLinuxDefaultDirectories();
            MakeLinuxUtilities();
            MakeLinuxDev();
        }

        void Init() {
            Thread initThread = new Thread(new ThreadStart(Init2));
            initThread.Start();

            // Thread init3Thread = new Thread(new ThreadStart(Init3));
            // init3Thread.Start();
        }

        void Init2() {
            AbstractFile kbEvent = Fs.Lookup("/dev/input/event0");

            while (true) {
                Debug.Log("recv input from init2: " + kbEvent.Read());
            }
        }

        // void Init3() {
        //     EventFile kbEvent = Fs.Lookup("/dev/input/event0");



        //     while (true) {
        //         Debug.log("recv input from init3: " + kbEvent.Read());
        //     }
        // }

        IEnumerator Initialize() {
            Debug.Log("Initilizing Subsystem");
            yield return new WaitForSeconds(1);
            yield return StartCoroutine(LoadLinuxSystem());
            yield return StartCoroutine(LoadLoginSystem());
        }

        IEnumerator LoadLinuxSystem() {
            yield return new WaitForSeconds(1);

            Terminal.Write("[    0.000000] Linux version " + Version + 
                      " (user@build-fedora4) (gcc version 6.4.1 20170727 (Red Hat 6.4.1-1) " +
                      "(GCC)) #1 SMP Wed Feb 17 01:49:26 UTC 2021");

            yield return new WaitForSeconds(2);

            string[] lines = Fs.Lookup("/dev/console")?.Read().Split('\n');

            float lineDelay;

            for(int i = 0; i < 20; i++) {
                string line = lines[i];

                Terminal.Write(line);
                Terminal.RequestHighYAxis();

                lineDelay = i * _bootDelay;

                yield return new WaitForSeconds(Random.Range(0.01f - lineDelay, 0.1f - lineDelay));
            }
        }
        
        IEnumerator LoadLoginSystem() {
            Terminal.Write("Initializing login");
            yield return new WaitForSeconds(2);

            Terminal.ClearBuffer();

            Terminal.Write("");
            Terminal.Write("Fedora 32 (Thirty Two)");
            Terminal.Write("Kernel " + Version + " on an x86_64 (tty)");
            Terminal.Write("");
            
            Terminal.Input("hacker1.localdomain login:", OnLogin);
        }

        void OnLogin(string login) {
            Debug.Log("Login: " + login);

            Terminal.Input("Password:", OnPassword);
        }

        void OnPassword(string password) {
            Debug.Log("Password: " + password);
        }

        void MakeLinuxDefaultDirectories() {
            string[] dir755 = new string[]{
                "dev",
                "home",
                "mnt",
                "usr",
                "etc",
                "media",
                "opt",
                "var",
            };

            string[] dir555 = new string[] { 
                "proc",
                "sys",
            };

            int perm755 = Perm.FromInt(7, 5, 5);
            int perm555 = Perm.FromInt(5, 5, 5);
            
            foreach (string path in dir755) {
                Fs.Add(new LinuxDirectory(path, 0, 0, perm755));
            }

            foreach (string path in dir555) {
                Fs.Add(new LinuxDirectory(path, 0, 0, perm555));
            }

            Fs.Add(new LinuxDirectory("root", 0, 0, Perm.FromInt(5, 5, 0)));
        }

        void MakeLinuxUtilities() {
            LinuxDirectory usrDirectory = (LinuxDirectory) Fs.Lookup("/usr");

            int binPerm = Perm.FromInt(7, 5, 5);

            Fs.AddFrom(usrDirectory, new LinuxDirectory("/usr/bin", 0, 0, binPerm));

            LinuxDirectory binDirectory = (LinuxDirectory) Fs.Lookup("/usr/bin");

            AbstractFile[] utils = new AbstractFile[] { 
                new LsUtility("/usr/bin/ls", 0, 0, binPerm),
            };

            foreach (AbstractFile utility in utils) {
                Fs.AddFrom(binDirectory, utility);
            }
        }

        void MakeLinuxDev() {
            LinuxDirectory devDirectory = (LinuxDirectory) Fs.Lookup("/dev");

            // Create Input Devices directory
            LinuxDirectory inputDevDirectory = new LinuxDirectory(
                "/dev/input",
                0, 0,
                Perm.FromInt(7, 5, 5)
            );

            Fs.AddFrom(devDirectory, inputDevDirectory);

            EventFile kbEventFile = new EventFile(
                "/dev/input/event0",
                0, 0, 
                Perm.FromInt(6, 6, 0)
            );

            Fs.AddFrom(inputDevDirectory, kbEventFile);

            Debug.Log("created kb input: " + Fs.Lookup("/dev/input/event0").Name);

            // Create TTY
            int devPerm = Perm.FromInt(5, 2, 0);

            AbstractFile[] devices = new AbstractFile[] {
                new TtyDevice(Terminal, "/dev/tty", 0, 0, Perm.FromInt(6, 6, 6)),
                new ConsoleDevice(FakeBootFile(), "/dev/console", 0, 0, Perm.FromInt(6, 0, 0)),
                new ConsoleDevice(FakeBootFile(), "/dev/console", 0, 0, Perm.FromInt(6, 0, 0)),
            };

            foreach (AbstractFile dev in devices) {
                Fs.AddFrom(devDirectory, dev);
            }
        }

        string FakeBootFile() {
            return Path.Combine(Application.dataPath, "Resources", "boot.txt"); 
        }
    }
}