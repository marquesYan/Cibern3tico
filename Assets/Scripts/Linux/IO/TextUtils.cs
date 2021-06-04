using System;
using System.Text;

namespace Linux.IO
{    
    public static class TextUtils {
        public static string FromInt(int code) {
            return ((char) code).ToString();
        }

        public static string FromUshort(ushort code) {
            return ((char) code).ToString();
        }

        public static int ToInt(string inputChar) {
            EnsureSingleCharacter(inputChar);

            return (int) ToByte(inputChar);
        }

        public static ushort ToUshort(string inputChar) {
            EnsureSingleCharacter(inputChar);

            return (ushort) ToByte(inputChar);
        }

        public static byte ToByte(string inputChar) {
            EnsureSingleCharacter(inputChar);

            return Encoding.UTF8.GetBytes(inputChar)[0];
        }

        public static void EnsureSingleCharacter(string input) {
            if (input.Length != 1) {
                throw new ArgumentException(
                    "Input should be a single character"  
                );
            }
        }
    }
}