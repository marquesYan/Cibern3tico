using System.Collections.Generic;

namespace Linux.FileSystem
{
    public static class PathUtils {
        const char SEPARATOR = '/';

        public static string[] Split(string path) {
            return path.Split(SEPARATOR);
        }

        public static string Normalize(string path) {
            return path.TrimEnd(SEPARATOR);
        }

        public static string ToAbsPath(string path) {
            return $"{SEPARATOR}{path.TrimStart(SEPARATOR)}";
        }

        public static bool IsAbsPath(string path) {
            return path.StartsWith($"{SEPARATOR}");
        }

        public static string Combine(params string[] paths) {
            if (paths.Length == 0) {
                return "";
            }

            bool isAbsPath = IsAbsPath(paths[0]);
            string[] normalizedPaths = new string[paths.Length];

            int i = 0;
            foreach(string path in paths) {
                normalizedPaths[i] = path.Trim(SEPARATOR);
                i++;
            }
 
            string newPath = string.Join($"{SEPARATOR}", normalizedPaths);

            if (isAbsPath) {
                return ToAbsPath(newPath);
            }

            return newPath;
        }

        public static bool Compare(string path1, string path2) {
            return Normalize(path1) == Normalize(path2);
        }

        public static string BaseName(string path) {
            if (path == null || path.Length == 0) {
                return "";
            }

            string[] parts = Split(path);
            return parts[parts.Length - 1];
        }

        public static string PathName(string path) {
            if (path == null || path.Length == 0) {
                return "";
            }

            bool isAbsolute = IsAbsPath(path);

            List<string> parts = new List<string>(Split(path));

            // Remove file name
            parts.RemoveAt(parts.Count - 1);

            string pathName = Combine(parts.ToArray());

            if (isAbsolute) {
                return ToAbsPath(pathName);
            }

            return pathName;
        }
    }
}