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

    public class UsersDatabase : FileDatabase<User> {

        public UsersDatabase(VirtualFileTree fs) : base(fs) { }

        public override void Add(User user) {
            if (LookupUid(user.Uid) != null) {
                throw new System.ArgumentException("uid already exists");
            }

            AppendLine(user.ToString());
        }

        public User LookupUid(int uid) {
            return LoadFromFs().Find(u => u.Uid == uid);
        }

        public override File DataSource() {
            return Fs.Lookup("/etc/passwd");
        }

        protected override User ItemFromTokens(string[] tokens) {
            if (tokens.Length != 7) {
                return null;
            }

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
    }
}