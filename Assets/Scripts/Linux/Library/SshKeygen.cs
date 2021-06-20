using System.Collections.Generic;
using Linux.Sys.RunTime;
using Linux.FileSystem;
using Linux.IO;
using Linux.Library.Crypto;
using Linux.Library.ArgumentParser;
using UnityEngine;

namespace Linux.Library
{
    public class SshKeygen : CompiledBin {
        public SshKeygen(
            string absolutePath,
            int uid,
            int gid,
            int permission,
            FileType type
        ) : base(absolutePath, uid, gid, permission, type) { }

        public override int Execute(UserSpace userSpace) {
            var parser = new GenericArgParser(
                userSpace,
                "Usage: {0} FILE",
                "Generate a RSA private and public keys"
            );

            List<string> arguments = parser.Parse();

            if (arguments.Count < 1) {
                parser.ShowHelpInfo();
                return 1;
            }

            RSA rsa = RSA.New();

            string path = userSpace.ResolvePath(arguments[0]);

            string privateKey = $"{path}.key";
            string publicKey = $"{path}.pub";

            using (ITextIO stream = userSpace.Open(privateKey, AccessMode.O_WRONLY)) {
                stream.WriteLine(rsa.N.ToString());
                stream.WriteLine(rsa.D.ToString());
            }

            using (ITextIO stream = userSpace.Open(publicKey, AccessMode.O_WRONLY)) {
                stream.WriteLine(rsa.N.ToString());
                stream.WriteLine(rsa.E.ToString());
            }

            return 0;
       }
    }
}