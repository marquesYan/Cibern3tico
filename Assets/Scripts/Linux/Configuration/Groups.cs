using System.Collections.Generic;
using Linux.FileSystem;
using Linux;

namespace Linux.Configuration
{    
    public class Group {
        public Group(string name,
                    int gid,
                    string[] users) {
            Name = name;
            Gid = gid;
            Users = users;
        }

        public string Name { get; protected set; }
        public int Gid { get; protected set; }
        public string[] Users { get; protected set; }
        
        public string ToString() {
            return $"{Name}:x:{Gid}:{string.Join(",", Users)}";
        }
    }

    public class GroupDatabase : FileDatabase<Group> {
        public GroupDatabase(FileTree fs) : base(fs) { }

        public override void Add(Group group) {
            if (LookupGid(group.Gid) == null) {
                throw new System.ArgumentException("gid already exists");
            }

            DataSource()?.Append(new string[] { group.ToString() });
        }

        public override AbstractFile DataSource() {
            return Fs.Lookup("/etc/group");
        }

        public Group LookupGid(int gid) {
            return LoadFromFs().Find(g => g.Gid == gid);
        }

        protected override Group ItemFromTokens(string[] tokens) {
            if (tokens.Length != 4) {
                return null;
            }

            string name = tokens[0];

            if (name.Length == 0) {
                return null;
            }

            int gid;

            if (! int.TryParse(tokens[2], out gid)) {
                return null;
            }

            string[] users = tokens[3].Split(':');

            return new Group(name, gid, users);
        }
    }
}