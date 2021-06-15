using SysPath = System.IO.Path;
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

            MountHomeFileSystem();

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

        void MountHomeFileSystem() {
            var mountPoint = new File(
                "/home",
                0,
                0,
                Perm.FromInt(7, 5, 5),
                FileType.F_MNT
            );

            string path = SysPath.Combine(
                Kernel.PersistentPath,
                "squashfs"
            );

            var homeFs = new LocalFileTree(
                path,
                new File(
                    "/",
                    0,
                    0,
                    Perm.FromInt(7, 5, 5),
                    FileType.F_DIR
                ),
                mountPoint
            );

            Kernel.Fs.Mount(mountPoint, homeFs);
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

            Kernel.UsersDb.Add(new User(
                "user",
                1000, 1000,
                "",
                "/home/user",
                "/usr/bin/bash"
            ));

            // Kernel.Fs.CreateDir(
            //     "/home/user",
            //     1000, 1000,
            //     Perm.FromInt(7, 0, 0)
            // );
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

            Kernel.GroupsDb.Add(new Group(
                "user",
                1000,
                "user"
            ));
        }

        void MakeLinuxDefaultDirectories() {
            string[] dir755 = new string[]{
                "/mnt",
                "/usr",
                "/etc",
                "/media",
                "/opt",
                "/var",
                "/run",

                "/usr/sbin",
                "/usr/bin",
                "/usr/share",
                "/usr/lib",
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