using System.Net;
using Linux;
using Linux.Net;
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
        userSpace.Api.CreateDir("/srv");

        using (ITextIO stream = userSpace.Open("/srv/index.html", AccessMode.O_WRONLY)) {
            stream.WriteLine("<html>");
            stream.WriteLine("  <head>");
            stream.WriteLine("  </head>");
            stream.WriteLine("</html>");
        }

        userSpace.Api.StartProcess(
            new string[] {
                "/usr/bin/httpd",
                "/srv"
            }
        );

        userSpace.Api.StartProcess(
            new string[] {
                "/usr/bin/sshd"
            }
        );

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