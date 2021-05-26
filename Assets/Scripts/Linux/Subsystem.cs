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
        [SerializeField] public float boot_delay = 0.001f; 
        
        [Range(32, 4096)]
        [SerializeField] public int BufferSize = 512;

        FileTree Fs { get; set; }
        UnityTerminal Terminal;

        bool _startedBoot = false;
    
        public Shell Sh { get; set; }
        string kernel_version = "5.4.98-1.fc25.x86_64";

        void OnGUI() {
            if (Terminal.IsFirstDraw && ! _startedBoot) {
                _startedBoot = true;
                StartCoroutine(Initialize());
            }
            Terminal.OnGUI();
        }

        void Start() {
            Terminal = new UnityTerminal(BufferSize);

            Fs = new FileTree();

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

            Terminal.Write("[    0.000000] Linux version " + kernel_version + 
                      " (user@build-fedora4) (gcc version 6.4.1 20170727 (Red Hat 6.4.1-1) " +
                      "(GCC)) #1 SMP Wed Feb 17 01:49:26 UTC 2021");

            yield return new WaitForSeconds(2);

            string[] lines = Fs.Lookup("/dev/console")?.Read().Split('\n');

            float line_delay;

            for(int i = 0; i < 20; i++) {
                string line = lines[i];

                Terminal.Write(line);
                Terminal.RequestHighYAxis();

                line_delay = i * boot_delay;

                yield return new WaitForSeconds(Random.Range(0.01f - line_delay, 0.1f - line_delay));
            }
        }
        
        IEnumerator LoadLoginSystem() {
            Terminal.Write("Initializing login");
            yield return new WaitForSeconds(2);

            Terminal.ClearBuffer();

            Terminal.Write("");
            Terminal.Write("Fedora 32 (Thirty Two)");
            Terminal.Write("Kernel " + kernel_version + " on an x86_64 (tty)");
            Terminal.Write("");
            Terminal.Write("hacker1.localdomain login: ");
        }

        void MakeLinuxDefaultDirectories() {
            string[] dir_755 = new string[]{
                "dev",
                "home",
                "mnt",
                "usr",
                "etc",
                "media",
                "opt",
                "var",
            };

            string[] dir_555 = new string[] { 
                "proc",
                "sys",
            };

            Perms[] perm_755 = new Perms[] { Perms.ALL, Perms.RX, Perms.RX };
            Perms[] perm_555 = new Perms[] { Perms.RX, Perms.RX, Perms.RX };
            
            foreach (string path in dir_755) {
                Fs.Add(new LinuxDirectory(path, perm_755));
            }

            foreach (string path in dir_555) {
                Fs.Add(new LinuxDirectory(path, perm_555));
            }

            Fs.Add(new LinuxDirectory("root", new Perms[] { Perms.RX, Perms.RX, Perms.NONE }));
        }

        void MakeLinuxUtilities() {
            LinuxDirectory usr_directory = (LinuxDirectory) Fs.Lookup("/usr");

            Perms[] bin_perms = new Perms[] { Perms.ALL, Perms.RX, Perms.RX };

            Fs.AddFrom(usr_directory, new LinuxDirectory("/usr/bin", bin_perms));

            LinuxDirectory bin_directory = (LinuxDirectory) Fs.Lookup("/usr/bin");

            AbstractFile[] utils = new AbstractFile[] { 
                new LsUtility($"{bin_directory.Path}/ls", bin_perms),
            };

            foreach (AbstractFile utility in utils) {
                Fs.AddFrom(bin_directory, utility);
            }
        }

        void MakeLinuxDev() {
            LinuxDirectory dev_directory = (LinuxDirectory) Fs.Lookup("/dev");

            Perms[] dev_perms = new Perms[] { Perms.RW, Perms.W, Perms.NONE };

            AbstractFile[] devices = new AbstractFile[] {
                new TtyDevice(Terminal, $"{dev_directory.Path}/tty", dev_perms),
                new ConsoleDevice(FakeBootFile(), $"{dev_directory.Path}/console", dev_perms),
            };

            foreach (AbstractFile dev in devices) {
                Fs.AddFrom(dev_directory, dev);
            }
        }

        string FakeBootFile() {
            return Path.Combine(Application.dataPath, "Resources", "boot.txt"); 
        }
    }
}