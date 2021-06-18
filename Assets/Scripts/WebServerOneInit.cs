using System.Net;
using Linux;
using Linux.Configuration;
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
        Kernel kernel = userSpace.Api.AccessKernel();

        kernel.UsersDb.Add(new User(
            "user",
            1000, 1000,
            "",
            "/home/user",
            "/usr/bin/bash"
        ));

        kernel.ShadowDb.Add(
            ShadowEntry.FromPlainText("user", "senha")
        );

        userSpace.Api.CreateDir("/home/user");
        userSpace.Api.CreateDir("/srv");

        using (ITextIO stream = userSpace.Open("/srv/index.html", AccessMode.O_WRONLY)) {
            stream.WriteLine("<html>");
            stream.WriteLine("  <head>");
            stream.WriteLine("  </head>");
            stream.WriteLine("</html>");
        }

        // userSpace.Api.StartProcess(
        //     new string[] {
        //         "/usr/bin/httpd",
        //         "/srv"
        //     }
        // );

        userSpace.Api.StartProcess(
            new string[] {
                "/usr/bin/sshd"
            }
        );

        using (ITextIO stream = userSpace.Open("/usr/bin/makemerich", AccessMode.O_WRONLY)) {
            stream.WriteLine("#!/usr/bin/bash");
            stream.WriteLine("###############################################################");
            stream.WriteLine("# Este script automaticamente faz dinheiro entrar na sua conta!");
            stream.WriteLine("###############################################################");
            stream.WriteLine("");
            stream.WriteLine("# Variaveis");
            stream.WriteLine("address=fonte.dinheiro.com.br");
            stream.WriteLine("output=/root/account.info");
            stream.WriteLine("");
            stream.WriteLine("curl http://$address > $output");
        }

        userSpace.Api.ChangeFilePermission(
            "/usr/bin/makemerich",
            Perm.FromInt(4, 7, 5, 5)
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