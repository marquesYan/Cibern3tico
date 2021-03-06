using System.Threading;
using SysFile = System.IO.File;
using SysPath = System.IO.Path;
using System.Collections.Generic;
using Linux;
using Linux.Configuration;
using Linux.IO;
using Linux.Library;
using Linux.FileSystem;
using Linux.Sys.RunTime;
using UnityEngine;

public class ShadowInitBin : CompiledBin {
    public ShadowInitBin(
        string absolutePath,
        int uid,
        int gid,
        int permission,
        FileType type
    ) : base(absolutePath, uid, gid, permission, type) { }

    public override int Execute(UserSpace userSpace) {
        Kernel kernel = userSpace.Api.AccessKernel();

        int n = 49601;
        int e = 725;

        string publicKey = "/run/id_rsa.pub";

        using (ITextIO stream = userSpace.Open(publicKey, AccessMode.O_WRONLY)) {
            stream.WriteLine(n.ToString());
            stream.WriteLine(e.ToString());
        }

        userSpace.Api.CreateDir("/srv");

        var passwords = new List<ShadowEntry>();
        passwords.Add(ShadowEntry.FromPlainText("marco", "3af2a70bcf54d881"));
        passwords.Add(ShadowEntry.FromPlainText("anne", "Prinsengracht"));

        var buffer = new BufferedStream(AccessMode.O_RDWR);

        passwords.ForEach(
            entry => {
                buffer.WriteLine(entry.ToString());
            }
        );

        int bufferFd = userSpace.Api.OpenStream(buffer);

        int shadowFd = userSpace.Api.Open("/srv/shadow", AccessMode.O_WRONLY);

        int pid = userSpace.Api.StartProcess(
            new string[] {
                "/usr/bin/gpg",
                publicKey
            },
            bufferFd,
            shadowFd,
            2
        );

        userSpace.Api.WaitPid(pid);

        using (ITextIO stream = userSpace.Open("/srv/history", AccessMode.O_WRONLY)) {
            stream.Write(SysFile.ReadAllText(
                SysPath.Combine(
                    kernel.DataPath,
                    "Resources",
                    "Text",
                    "AnneFrank.txt"
                )
            ));
        }

        userSpace.Api.StartProcess(
            new string[] {
                "/usr/bin/httpd",
                "/srv"
            }
        );

        return 0;
    }
}

public class ShadowInit : KernelInit {
    public override CompiledBin Init() {
        return new ShadowInitBin(
            "/run/init",
            0,
            0,
            Perm.FromInt(7, 5, 5),
            FileType.F_REG
        );
    }
}