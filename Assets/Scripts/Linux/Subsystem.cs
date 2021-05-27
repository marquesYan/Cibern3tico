using System.IO;
using System.Collections;
using UnityEngine;
using Linux.FileSystem;
using Linux.Utilities;
using Linux.Devices;
using Linux;

namespace Linux
{    
    public class Subsystem : MonoBehaviour {
        [Range(0.001f, 0.00001f)]
        [SerializeField] public float BootDelay = 0.001f; 
        
        [Range(32, 4096)]
        [SerializeField] public int BufferSize = 512;

        FileTree Fs { get; set; }
        UnityTerminal Terminal;

        bool _startedBoot = false;
    
        public Shell Sh { get; set; }
        string KernelVersion = "5.4.98-1.fc25.x86_64";

        void OnGUI() {
            if (Terminal.IsFirstDraw && ! _startedBoot) {
                _startedBoot = true;
                StartCoroutine(Initialize());
            }
            Terminal.OnGUI();
        }

        void Start() {
            Terminal = new UnityTerminal(BufferSize);

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

            Sh = new Linux.Shell(Fs);
        }

        public IEnumerator Initialize() {
            Debug.Log("Initilizing Subsystem");
            yield return StartCoroutine(LoadLinuxSystem());
            yield return StartCoroutine(LoadLoginSystem());
        }

        IEnumerator LoadLinuxSystem() {
            yield return new WaitForSeconds(1);

            Terminal.Write("[    0.000000] Linux version " + KernelVersion + 
                      " (user@build-fedora4) (gcc version 6.4.1 20170727 (Red Hat 6.4.1-1) " +
                      "(GCC)) #1 SMP Wed Feb 17 01:49:26 UTC 2021");

            yield return new WaitForSeconds(2);

            string[] lines = Fs.Lookup("/dev/console")?.Read().Split('\n');

            float lineDelay;

            for(int i = 0; i < 20; i++) {
                string line = lines[i];

                Terminal.Write(line);
                Terminal.RequestHighYAxis();

                lineDelay = i * BootDelay;

                yield return new WaitForSeconds(Random.Range(0.01f - lineDelay, 0.1f - lineDelay));
            }
        }
        
        IEnumerator LoadLoginSystem() {
            Terminal.Write("Initializing login");
            yield return new WaitForSeconds(2);

            Terminal.ClearBuffer();

            Terminal.Write("");
            Terminal.Write("Fedora 32 (Thirty Two)");
            Terminal.Write("Kernel " + KernelVersion + " on an x86_64 (tty)");
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

            int devPerm = Perm.FromInt(5, 2, 0);

            AbstractFile[] devices = new AbstractFile[] {
                new TtyDevice(Terminal, "/dev/tty", 0, 0, Perm.FromInt(6, 6, 6)),
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