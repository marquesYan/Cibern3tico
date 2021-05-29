using System.Threading;
using UnityEngine;
using Linux.Boot;
using Linux.Configuration;
using Linux.FileSystem;
using Linux.PseudoTerminal;
using Linux.Utilities;

namespace Linux
{    
    public class Kernel {
        public UnityTerminal Terminal { get; protected set; }
        public string Version { get; protected set; }
    
        public VirtualFileTree Fs;
        public ProcessesTable ProcTable;
        public UsersDatabase UsersDb;
        public GroupsDatabase GroupsDb;
        public PeripheralsTable PciTable;

        int _bufferSize = 512;

        float _bootDelay = 0.0001f;

        public void Bootstrap() {
            Terminal = new UnityTerminal(_bufferSize);

            Terminal.SubscribeFirstDraw(TriggerStartup);
        }

        void TriggerStartup() {
            var startup = new Thread(new ThreadStart(() => {
                new StartupStage(this);

                Init(); 
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

            Fs.AddFrom((Directory)Fs.Lookup("/usr/sbin"), utility);

            User root = UsersDb.LookupUid(0);

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

        string FakeBootFile() {
            return System.IO.Path.Combine(Application.dataPath, "Resources", "boot.txt"); 
        }
    }
}