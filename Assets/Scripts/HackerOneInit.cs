using UnityEngine;
using Linux;
using Linux.Library;
using Linux.FileSystem;
using Linux.Sys.RunTime;

public class HackerOneInitBin : CompiledBin {
    public HackerOneInitBin(
        string absolutePath,
        int uid,
        int gid,
        int permission,
        FileType type
    ) : base(absolutePath, uid, gid, permission, type) { }

    public override int Execute(UserSpace userSpace) {
        Debug.Log("calling hacker one init");
        // var api = new KernelSpace(kernel);

        // api.StartProcess(
        //     new string[] {
        //         "/usr/bin/nc",
        //         "-l",
        //         "-s", "10.0.0.1",
        //         "9999"
        //     }
        // );
        return 0;
    }
}

public class HackerOneInit : KernelInit {
    public override CompiledBin Init() {
        return new HackerOneInitBin(
            "/run/init",
            0,
            0,
            Perm.FromInt(7, 5, 5),
            FileType.F_REG
        );
    }
}