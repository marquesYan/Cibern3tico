using System;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using Linux.FileSystem;
using Linux;
using UnityEngine;

namespace Linux.Configuration
{
    public class Hasher {
        public static string HashPasswd(string password) {
            byte[] data = Encoding.UTF8.GetBytes(password);
            byte[] messageDigest;

            using (SHA512 shaM = new SHA512Managed()) {
                messageDigest = shaM.ComputeHash(data);
            }

            return Convert.ToBase64String(messageDigest);
        }
    }

    public class ShadowEntry {
        public const string EMPTY_HASH = "!!";

        public ShadowEntry(
            string login,
            string hash
        ) {
            Login = login;
            Hash = hash;
            HasPasswd = Hash !=  EMPTY_HASH;
        }

        public ShadowEntry(string login) : this(login, EMPTY_HASH) { }

        public static ShadowEntry FromPlainText(
            string login,
            string password
        ) {
            return new ShadowEntry(login, Hasher.HashPasswd(password));
        }

        public readonly string Login;

        public readonly bool HasPasswd;

        public readonly string Hash;

        public string ToString() {
            return $"{Login}:{Hash}";
        }
    }

    public class ShadowDatabase : FileDatabase<ShadowEntry> {

        public ShadowDatabase(VirtualFileTree fs) : base(fs) { }

        public override void Add(ShadowEntry shadow) {
            if (LookupLogin(shadow.Login) != null) {
                throw new System.ArgumentException("Login already exists");
            }

            AppendLine(shadow.ToString());
        }

        public ShadowEntry LookupLogin(string login) {
            return LoadFromFs().Find(u => u.Login == login);
        }

        public bool CheckLoginPassword(string login, string password) {
            ShadowEntry shadow = LookupLogin(login);

            if (shadow == null) {
                return false;
            }

            Debug.Log("has passwd: " + shadow.HasPasswd);

            if (!shadow.HasPasswd) {
                return true;
            }

            string hash = Hasher.HashPasswd(password);

            return shadow.Hash == hash;
        }

        public override File DataSource() {
            return Fs.Lookup("/etc/shadow");
        }

        protected override ShadowEntry ItemFromTokens(string[] tokens) {
            if (tokens.Length != 2) {
                return null;
            }

            string login = tokens[0];

            if (login.Length == 0) {
                return null;
            }

            string hash = tokens[1];

            if (hash.Length == 0) {
                return null;
            }

            return new ShadowEntry(login, hash);
        }
    }
}