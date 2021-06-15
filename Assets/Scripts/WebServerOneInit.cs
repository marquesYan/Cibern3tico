using System.Threading;
using Linux;
using Linux.IO;
using Linux.FileSystem;
using Linux.Library;
using Linux.Sys.RunTime;
using UnityEngine;

public class WebServerOneBin : CompiledBin {
    public WebServerOneBin(
        string absolutePath,
        int uid,
        int gid,
        int permission,
        FileType type
    ) : base(absolutePath, uid, gid, permission, type) { }

    public override int Execute(UserSpace userSpace) {
        Debug.Log("calling webserver one");

        int fd = userSpace.Api.Open("/test", AccessMode.O_RDWR);

        ITextIO buffer = userSpace.Api.LookupByFD(fd);
        buffer.WriteLine("test 1");
        buffer.WriteLine("test 2");
        buffer.WriteLine("test 3");
        buffer.WriteLine("test 4");

        Debug.Log("waiting");

        Thread.Sleep(10000);

        Debug.Log("sending data to socket: " + fd); 

        int pid = userSpace.Api.StartProcess(
            new string[] {
                "/usr/bin/nc",
                "-s", "10.0.0.2",
                "10.0.0.1", "9999"
            },
            fd,
            1,
            2
        );

        int exitCode = userSpace.Api.WaitPid(pid);
        Debug.Log("nc: exit code:" + exitCode);

        return 0;
    }
}

public class WebServerOneInit : KernelInit {
    public override CompiledBin Init() {
        return new WebServerOneBin(
            "/run/init",
            0,
            0,
            Perm.FromInt(7, 5, 5),
            FileType.F_REG
        );
    }
}