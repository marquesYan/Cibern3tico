using System;
using System.Threading;
using Linux;
using Linux.Configuration;
using Linux.IO;
using Linux.Sys.IO;
using Linux.Sys.Input.Drivers.Tty;
using Linux.Library;
using Linux.Library.Crypto;
using Linux.FileSystem;
using Linux.Sys.RunTime;
using UnityEngine;

public class MarcoTypingBin : CompiledBin {
    public MarcoTypingBin(
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

        string password = "3af2a70bcf54d881";

        int ptsFd = userSpace.Api.OpenPty();

        // Permite gerente saber o que o marco está digitando
        string ptsPath = userSpace.Api.RealPath(
            userSpace.Api.GetFdPath(ptsFd)
        );

        userSpace.Api.ChangeFileGroup(
            ptsPath,
            1000
        );

        userSpace.Api.ChangeFilePermission(
            ptsPath,
            Perm.FromInt(6, 4, 0)
        );

        try {
            using (ITextIO ptStream = userSpace.Api.LookupByFD(ptsFd)) {
                IoctlDevice pts = (IoctlDevice)ptStream;

                while (eventSet) {
                    WriteText(pts, "https://gmail.com");
                    WriteText(pts, "marco@gmail.com");
                    WriteText(pts, password);

                    WriteText(pts, "Olá Fernando,");
                    WriteText(pts, "");
                    WriteText(pts, "Segundo os dados informados por Anne na reunião de ontem, devemos optar por agir pela ciência de forma estridente.");
                    WriteText(pts, "Entraremos em contato com mais informações.");
                    WriteText(pts, "");
                    WriteText(pts, "Atenciosamente");

                    Thread.Sleep(10000);
                }
            }
        } finally {
            userSpace.Api.RemovePty(ptsFd);
        }

        return 0;
    }

    protected void WriteText(IoctlDevice pts, string text) {
        string[] keyArray = new string[1];
        var rand = new System.Random();

        foreach (char key in text + "\n") {
            keyArray[0] = key.ToString();

            pts.Ioctl(
                PtyIoctl.TIO_RCV_INPUT,
                ref keyArray
            );

            Thread.Sleep(rand.Next(200, 600));
        }
    }
}

public class AccountingInitBin : CompiledBin {
    public AccountingInitBin(
        string absolutePath,
        int uid,
        int gid,
        int permission,
        FileType type
    ) : base(absolutePath, uid, gid, permission, type) { }

    public override int Execute(UserSpace userSpace) {
        Kernel kernel = userSpace.Api.AccessKernel();

        bool eventSet = true;

        userSpace.Api.Trap(
            ProcessSignal.SIGTERM,
            (int[] args) => {
                eventSet = false;
            }
        );

        int p = 193;
        int q = 257;
        int e = 725;
        int phi = 768;

        userSpace.Api.CreateDir("/srv");
        userSpace.Api.ChangeFilePermission("/srv", Perm.FromInt(7, 0, 0));

        using (ITextIO stream = userSpace.Open("/srv/keys.txt", AccessMode.O_WRONLY)) {
            stream.WriteLine("Verifique com o pessoal de segurança se os parâmetros para a criação da chave assimétrica estão dentro da nossa política:");
            stream.WriteLine("");
            stream.WriteLine($"P: {p}");
            stream.WriteLine($"Q: {q}");
            stream.WriteLine($"E: {e}");
            stream.WriteLine("IMPORTANTE: Não esquecer de excluir esse arquivo depois ;)");
        }

        RSA rsa = RSA.FromParameters(p, q, e, phi);
        string privateKey = "/root/id_rsa.key";

        using (ITextIO stream = userSpace.Open(privateKey, AccessMode.O_WRONLY)) {
            stream.WriteLine(rsa.N.ToString());
            stream.WriteLine(rsa.D.ToString());
        }

        userSpace.Api.StartProcess(
            new string[] {
                "/usr/bin/httpd",
                "/srv"
            }
        );

        var file = new MarcoTypingBin(
            "/run/marco-typing",
            1001,
            1001,
            Perm.FromInt(7, 0, 0),
            FileType.F_REG
        );

        File runFile = kernel.Fs.LookupOrFail("/run");
        kernel.Fs.AddFrom(runFile, file);

        userSpace.Api.StartProcess(
            new string[] { file.Path }
        );

        userSpace.Api.StartProcess(
            new string[] {
                "/usr/bin/sshd"
            }
        );

        kernel.UsersDb.Add(new User(
            "gerente",
            1000,
            1000,
            "",
            "/home/gerente",
            "/usr/bin/bash"
        ));

        kernel.GroupsDb.Add(new Group(
            "gerente",
            1000,
            "1000",
            "1001",
            "1002"
        ));

        kernel.UsersDb.Add(new User(
            "marco",
            1001,
            1001,
            "",
            "/home/marco",
            "/usr/bin/bash"
        ));

        kernel.GroupsDb.Add(new Group(
            "marco",
            1001,
            "1001"
        ));

        kernel.UsersDb.Add(new User(
            "anne",
            1002,
            1002,
            "",
            "/home/anne",
            "/usr/bin/bash"
        ));

        kernel.GroupsDb.Add(new Group(
            "anne",
            1002,
            "1002"
        ));

        userSpace.Api.CreateDir("/home/marco");
        userSpace.Api.CreateDir("/home/anne");

        using (ITextIO stream = userSpace.Open("/home/marco/root.txt", AccessMode.O_WRONLY)) {
            stream.WriteLine("");
        }

        userSpace.Api.ChangeFileGroup("/home/marco/root.txt", 1001);
        userSpace.Api.ChangeFileOwner("/home/marco/root.txt", 1001);
        userSpace.Api.ChangeFilePermission("/home/marco/root.txt", Perm.FromInt(6, 6, 0));

        int pid, shadowFd, bufferFd, exitCode;
        BufferedStream buffer;

        while (eventSet) {
            try {
                buffer = new BufferedStream(AccessMode.O_RDWR);
                bufferFd = userSpace.Api.OpenStream(buffer);

                pid = userSpace.Api.StartProcess(
                    new string[] {
                        "/usr/bin/curl",
                        "-t", "5",
                        "http://10.0.0.4/shadow"
                    },
                    0,
                    bufferFd,
                    2
                );

                exitCode = userSpace.Api.WaitPid(pid);

                if (exitCode == 5) {
                    // Timed out
                    Thread.Sleep(30000);
                    continue;
                }

                shadowFd = userSpace.Api.Open("/etc/shadow", AccessMode.O_WRONLY);

                // Decrypt data in buffer and send to shadow file
                pid = userSpace.Api.StartProcess(
                    new string[] {
                        "/usr/bin/gpg",
                        "-d",
                        privateKey
                    },
                    bufferFd,
                    shadowFd,
                    2
                );

                userSpace.Api.WaitPid(pid);

                using (ITextIO stream = userSpace.Api.LookupByFD(shadowFd)) {
                    stream.WriteLine(new ShadowEntry("root").ToString());
                }
            } catch (System.Exception exc) {
                userSpace.Stderr.WriteLine(exc.ToString());
            }

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