using SysPath = System.IO.Path;
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
        Kernel kernel = userSpace.Api.AccessKernel();

        kernel.Fs.RecursivelyDeleteDir("/root");

        var mountPoint = new File(
            "/root",
            0,
            0,
            Perm.FromInt(7, 5, 5),
            FileType.F_MNT
        );

        string path = SysPath.Combine(
            kernel.PersistentPath,
            "squashfs"
        );

        var rootFs = new LocalFileTree(
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

        kernel.Fs.Mount(mountPoint, rootFs);

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