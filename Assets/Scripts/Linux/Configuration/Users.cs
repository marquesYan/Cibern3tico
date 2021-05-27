using System.Collections.Generic;
using Linux.FileSystem;
using Linux;

namespace Linux.Configuration
{    
    public class User {
        public User(string login,
                    int uid, 
                    int gid, 
                    string description, 
                    string homeDir, 
                    string shell) {
            Login = login;
            Uid = uid;
            Gid = gid;
            Description = description;
            HomeDir = homeDir;
            Shell = shell;
        }

        public string Login { get; protected set; }
        public int Uid { get; protected set; }
        public int Gid { get; protected set; }
        public string Description { get; protected set; }
        public string HomeDir { get; protected set; }
        public string Shell { get; protected set; }

        public string ToString() {
            return $"{Login}:x:{Uid}:{Gid}:{Description}:{HomeDir}:{Shell}";
        }
    }

    public class UserDatabase {
        List<User> Users { get; set; }

        FileTree Fs { get; set; }

        public UserDatabase(FileTree fs) { 
            Fs = fs;
            Users = new List<User>();

            LoadFromFs();
        }

        public void Add(User user) {
            if (LookupUid(user.Uid).Equals(null)) {
                throw new System.ArgumentException("uid already exists");
            }

            Users.Add(user);

            DataSource()?.Append(new string[] { user.ToString() });
        }

        public User LookupUid(int uid) {
            return Users.Find(u => u.Uid == uid);
        }

        void LoadFromFs() {
            string[] lines = ReadLines();

            foreach (string line in lines) {
                string[] tokens = line.Split(':');

                if (tokens.Length != 7) {
                    continue;
                }

                Add(UserFromTokens(tokens));
            }
        }

        User UserFromTokens(string[] tokens) {
            string login = tokens[0];

            if (login.Length == 0) {
                return null;
            }

            int uid;
            int gid;

            if (! int.TryParse(tokens[2], out uid)) {
                return null;
            }

            if (! int.TryParse(tokens[3], out gid)) {
                return null;
            }

            string description = tokens[4];
            string homeDir = tokens[5];
            string shell = tokens[6];

            return new User(login, uid, gid, description, homeDir, shell);
        }

        AbstractFile DataSource() {
            return Fs.Lookup("/etc/passwd");
        }

        string[] ReadLines() {
            return DataSource()?.Read().Split('\n');
        }
    }
}