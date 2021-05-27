using System;
using System.Collections;
using System.Collections.Generic;

namespace Linux.FileSystem
{
    public static class PermModes {
        public const int S_IRUSR = 0b_0100_0000_0000;
        public const int S_IWUSR = 0b_0010_0000_0000;
        public const int S_IXUSR = 0b_0001_0000_0000;
        public const int S_IRGRP = 0b_0000_0100_0000;
        public const int S_IWGRP = 0b_0000_0010_0000;
        public const int S_IXGRP = 0b_0000_0001_0000;
        public const int S_IROTH = 0b_0000_0000_0100;
        public const int S_IWOTH = 0b_0000_0000_0010;
        public const int S_IXOTH = 0b_0000_0000_0001;
    }

    public static class Perm {
        public static int FromInt(int owner, int group, int other) {
            return (owner << 8) | (group << 4) | other;
        }
    }
}