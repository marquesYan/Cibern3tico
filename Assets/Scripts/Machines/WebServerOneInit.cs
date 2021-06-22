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
            "renan",
            1000, 1000,
            "",
            "/home/user",
            "/usr/bin/bash"
        ));

        kernel.ShadowDb.Add(
            ShadowEntry.FromPlainText("renan", "dragon")
        );

        userSpace.Api.CreateDir("/home/renan");
        userSpace.Api.CreateDir("/srv");

        using (ITextIO stream = userSpace.Open("/srv/index.html", AccessMode.O_WRONLY)) {
            stream.WriteLine("tarefas concluídas:");
            stream.WriteLine("\t- [X] atualizar o sistema");
            stream.WriteLine("\t- [X] aplicar patches de segurança");
            stream.WriteLine("\t- [ ] comprar um disco novo");
            stream.WriteLine("\t- [ ] aplicar quota no diretório do Renan");
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

        using (ITextIO stream = userSpace.Open("/root/pista.txt", AccessMode.O_WRONLY)) {
            stream.WriteLine("Hmm...não é que o cara é bom mesmo?!");
            stream.WriteLine("");
            stream.WriteLine("Dê uma investigada no servidor 10.0.0.5, parece que os funcionários usam ele remotamente na empresa...");
        }

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