using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Linux.Sys.RunTime;
using Linux.FileSystem;
using Linux.IO;
using Linux.Library.Crypto;
using Linux.Library.ArgumentParser;
using UnityEngine;

namespace Linux.Library
{
    public class Gpg : CompiledBin {
        public Gpg(
            string absolutePath,
            int uid,
            int gid,
            int permission,
            FileType type
        ) : base(absolutePath, uid, gid, permission, type) { }

        public override int Execute(UserSpace userSpace) {
            var parser = new GenericArgParser(
                userSpace,
                "Usage: {0} [-d] KEYFILE FILE",
                "Encrypt/decrypt streams using public key algorithms"
            );

            bool decrypt = false;
            parser.AddArgument<string>(
                "d|decrypt",
                "Perform encryption",
                v => decrypt = true
            );

            List<string> arguments = parser.Parse();

            if (arguments.Count < 2) {
                parser.ShowHelpInfo();
                return 1;
            }

            string keyFile = userSpace.ResolvePath(arguments[0]);
            string filePath = userSpace.ResolvePath(arguments[1]);

            RSA rsa;

            using (ITextIO stream = userSpace.Open(keyFile, AccessMode.O_RDONLY)) {
                string[] lines = stream.ReadLines();

                if (lines.Length < 2) {
                    throw new System.InvalidOperationException(
                        "Invalid format of key file"
                    );
                }

                BigInteger n = BigInteger.Parse(lines[0]);
                BigInteger x = BigInteger.Parse(lines[1]);

                if (decrypt) {
                    rsa = RSA.PrivKey(n, x);
                } else {
                    rsa = RSA.PubKey(n, x);
                }
            }

            string data;

            using (ITextIO stream = userSpace.Open(filePath, AccessMode.O_RDONLY)) {
                data = stream.Read();
            }

            List<BigInteger> numberMap = new List<BigInteger>();

            int chr;
            BigInteger number;

            if (decrypt) {
                foreach (string cipher in data.Split(',')) {
                    number = BigInteger.Parse(cipher);

                    numberMap.Add(
                        rsa.Decrypt(number)
                    );
                }
            } else {
                for (int i = 0; i < data.Length; i++) {
                    chr = TextUtils.ToInt(data[i]);

                    number = new BigInteger(chr);

                    numberMap.Add(
                        rsa.Encrypt(number)
                    );
                }
            }

            var result = new StringBuilder();

            if (decrypt) {
                foreach (BigInteger num in numberMap) {
                    chr = (int)num;
                    result.Append(TextUtils.FromInt(chr));
                }
            } else {
                result.Append(string.Join(",", numberMap));
            }

            userSpace.Print(result.ToString(), "");

            return 0;
       }
    }
}