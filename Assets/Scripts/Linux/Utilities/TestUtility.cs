using UnityEngine;
using Linux.Configuration;
using Linux.FileSystem;

namespace Linux.Utilities
{    
    public class TestUtility : File, IUtility {
        public TestUtility() : base("/usr/bin/init", 0, 0, Perm.FromInt(7, 5, 5)) {}

        public void Execute(Process process) {
            Debug.Log("test utility works!");
        }
    }
}