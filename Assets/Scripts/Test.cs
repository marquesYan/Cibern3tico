using System;
using System.Text;
using UnityEngine;

namespace Test {
    public class Test : MonoBehaviour {
        void Start() {
            Debug.Log(KeyCode.Tab.ToString());
        }

        void Update() {
            string inputStr = Input.inputString;
            if (!string.IsNullOrEmpty(inputStr)) {
                byte[] b = Encoding.ASCII.GetBytes(inputStr);
                Debug.Log(BitConverter.ToString(b));
            }
        }
    }
}