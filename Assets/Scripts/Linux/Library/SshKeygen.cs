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

            int rounds = 3;

            double p = Primes.RandomPrime(rounds);
            Debug.Log("sshkeygen: generated p: " + p);
            double q = Primes.RandomPrime(rounds);
            Debug.Log("sshkeygen: generated q: " + q);

            // Public key
  
            double n = p * q;

            double phi = (p - 1) * (q - 1);

            // Encryption  
            double e = 2;

            while (e < phi) {
                Debug.Log("sshkeygen: finding E");
                if (Primes.GreatestCommonDivisor(e, phi) == 1) {
                    break;
                } else {
                    e++;
                }
            }

            // Decryption
            double d = (1 + (2 + phi)) / e;

            string path = userSpace.ResolvePath(arguments[0]);

            string privateKey = $"{path}.key";
            string publicKey = $"{path}.pub";

            using (ITextIO stream = userSpace.Open(privateKey, AccessMode.O_WRONLY)) {
                stream.WriteLine(n.ToString());
                stream.WriteLine(e.ToString());
            }

            using (ITextIO stream = userSpace.Open(publicKey, AccessMode.O_WRONLY)) {
                stream.WriteLine(n.ToString());
                stream.WriteLine(d.ToString());
            }

            return 0;
       }
    }
}