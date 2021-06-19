using System.Collections.Generic;
using Linux.Configuration;
using Linux.IO;
using Linux.Sys.RunTime;
using Linux.FileSystem;

namespace Linux.Library
{    
    public class PassCrack : CompiledBin {
        public PassCrack(
            string absolutePath,
            int uid,
            int gid,
            int permission,
            FileType type
        ) : base(absolutePath, uid, gid, permission, type) { }

        public override int Execute(UserSpace userSpace) {
            var parser = new ArgumentParser.GenericArgParser(
                userSpace,
                "Usage: {0} [-w wordlist] FILE",
                "Try to crack the SHA256 hashes in FILE using WORDLIST mode"
            );

            string wordlist = null;
            parser.AddArgument<string>(
                "w|wordlist=",
                "File with candidate password per line",
                (string path) => wordlist = path
            );

            List<string> arguments = parser.Parse();

            if (arguments.Count < 1) {
                parser.ShowHelpInfo();
                return 1;
            }

            if (wordlist == null) {
                userSpace.Stderr.WriteLine("pass-crack: Missing wordlist argument");
                return 2;
            }

            string hashFile = userSpace.ResolvePath(arguments[0]);
            string wordsFile = userSpace.ResolvePath(wordlist);

            using (ITextIO wordsStream = userSpace.Open(wordsFile, AccessMode.O_RDONLY)) {
                using (ITextIO hashesStream = userSpace.Open(hashFile, AccessMode.O_RDONLY)) {
                    string[] candidates = wordsStream.ReadLines();

                    foreach (string line in hashesStream.ReadLines()) {
                        string[] info = line.Split(
                            new char[] { ':' },
                            2,
                            0
                        );

                        if (info.Length == 2) {
                            string name = info[0];
                            string hash = info[1];

                            userSpace.Print($"[+] cracking hash '{name}'");
                            TryPasswordCracking(userSpace, name, hash, candidates);
                        }
                    }
                }
            }

            return 0;
        }

        protected void TryPasswordCracking(
            UserSpace userSpace,
            string name,
            string hash,
            string[] wordList
        ) {
            foreach (string word in wordList) {
                string computedHash = Hasher.HashPasswd(word);

                if (computedHash == hash) {
                    userSpace.Print($"[+] found password for '{name}': {word}");
                    break;
                }
            }
        }
    }
}