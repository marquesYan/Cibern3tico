using System;
using System.Text;

namespace Linux.IO
{    
    public static class TextUtils {
        public static string FromInt(int code) {
            return ((char) code).ToString();
        }

        public static string FromByte(byte code) {
            return FromByteArray(new byte[] { code });
        }

        public static string FromByteArray(byte[] array) {
            return Encoding.UTF8.GetString(array);
        }

        public static string FromUshort(ushort code) {
            return ((char) code).ToString();
        }

        public static int ToInt(string inputChar) {
            EnsureSingleCharacter(inputChar);

            return (int) ToByte(inputChar);
        }

        public static int ToInt(char inputChar) {
            return (int) ToByte(inputChar);
        }

        public static ushort ToUshort(string inputChar) {
            EnsureSingleCharacter(inputChar);

            return (ushort) ToByte(inputChar);
        }

        public static byte ToByte(string inputChar) {
            EnsureSingleCharacter(inputChar);

            return ToByte(inputChar[0]);
        }

        public static byte ToByte(char input) {
            return Encoding.UTF8.GetBytes(new char[] { input })[0];
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