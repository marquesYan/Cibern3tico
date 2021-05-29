using UnityEngine;
using Linux.FileSystem;
using Linux.Utilities;
using Linux;

namespace Linux.Utilities.Sbin {
    public class InitUtility {

        public int Execute() {
            Debug.Log("Kernel just called init");
            return 0;
        }
    }
}