using System.Collections.Generic;
using Linux.FileSystem;
using Linux.IO;
using Linux;

namespace Linux.Configuration
{    
    public class Group {
        public Group(string name,
                    int gid,
                    params string[] users) {
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

    public class GroupsDatabase : FileDatabase<Group> {
        public GroupsDatabase(VirtualFileTree fs) : base(fs) { }

        public override void Add(Group group) {
            if (LookupGid(group.Gid) != null) {
                throw new System.ArgumentException("gid already exists");
            }

            AppendLine(group.ToString());
        }

        public override File DataSource() {
            return Fs.Lookup("/etc/group");
        }

        public Group LookupGid(int gid) {
            return LoadFromFs().Find(g => g.Gid == gid);
        }

        public Group LookupName(string name) {
            return LoadFromFs().Find(g => g.Name == name);
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

            string[] users = tokens[3].Split(',');

            return new Group(name, gid, users);
        }
    }
}