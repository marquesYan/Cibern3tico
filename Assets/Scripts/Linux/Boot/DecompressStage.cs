using Linux.Configuration;
using Linux.FileSystem;
using Linux.Sys;

namespace Linux.Boot
{    
    public class DecompressStage {
        protected Linux.Kernel Kernel;

        public DecompressStage(Linux.Kernel kernel) {
            Kernel = kernel;
            Start();
        }

        void Start() {
            MakeFileSystem();

            MakeLinuxDefaultDirectories();

            MakeLinuxConfigurations();
            
            MakeLinuxDev();

            Kernel.UsersDb = new UsersDatabase(Kernel.Fs);
            // Print("users database: created");

            Kernel.GroupsDb = new GroupsDatabase(Kernel.Fs);
            // Print("groups database: created");

            MakeSystemUsers();
            int usersCount = Kernel.UsersDb.Count();
            // Print($"created system users: {usersCount}");

            MakeSystemGroups();
            int groupsCount = Kernel.GroupsDb.Count();
            // Print($"created system groups: {groupsCount}");
        }

        void MakeFileSystem() {
            Kernel.Fs = new VirtualFileTree(
                new File(
                    "/",
                    0,
                    0,
                    Perm.FromInt(7, 5, 5),
                    FileType.F_DIR
                )
            );
        }

        void MakeSystemUsers() {
            Kernel.UsersDb.Add(new User(
                "root",
                0, 0,
                "",
                "/root",
                "/usr/bin/bash"
            ));

            Kernel.UsersDb.Add(new User(
                "bin",
                1, 1,
                "",
                "/usr/bin",
                "/usr/sbin/nologin"
            ));
        }

        void MakeSystemGroups() {
            Kernel.GroupsDb.Add(new Group(
                "root",
                0,
                "root"
            ));

            Kernel.GroupsDb.Add(new Group(
                "bin",
                1,
                "bin"
            ));
        }

        void MakeLinuxDefaultDirectories() {
            string[] dir755 = new string[]{
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
                Kernel.Fs.CreateDir(
                    path,
                    0, 0,
                    perm755
                );
            }

            foreach (string path in dir555) {
                Kernel.Fs.CreateDir(
                    path, 0, 0, perm555
                );
            }

            Kernel.Fs.CreateDir(
                "/root", 0, 0, Perm.FromInt(5, 5, 0)
            );
        }

        void MakeLinuxDev() {
            // Create Input Devices directory
            Kernel.Fs.CreateDir(
                "/dev",
                0, 0,
                Perm.FromInt(7, 5, 5)
            );

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
            int configPerm = Perm.FromInt(7, 5, 5);

            var files = new string[] {
                "/etc/group",
                "/etc/passwd",
                "/etc/hosts"
            };

            foreach(string path in files) {
                Kernel.Fs.Create(
                    path,
                    0, 0,
                    configPerm
                );
            }
        }
    }
}