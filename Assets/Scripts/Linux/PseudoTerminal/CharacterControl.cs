using System;
using System.Reflection;
using System.Collections.Generic;
using Linux.IO;
using UnityEngine;

namespace Linux.PseudoTerminal {

    public static class CharacterControl {

        public const string C_DBACKSPACE = "^[[2~";
        public const string C_DDELETE = "^[[3~";
        public const string C_DTAB = "^[[1~";
        public const string C_DESCAPE = "^[";
        // public const string C_DLEFT_SHIFT = "^LSH";
        // public const string C_DRIGHT_SHIFT = "^RSH";
        public const string C_DCTRL = "^[[4~";
        public const string C_UCTRL = "^[[4";
        public const string C_DUP_ARROW = "^[[A";
        public const string C_DDOWN_ARROW = "^[[B";
        public const string C_DLEFT_ARROW = "^[[D";
        public const string C_DRIGHT_ARROW = "^[[C";
        // public const string C_UBACKSPACE = "BKS^";
        // public const string C_UDELETE = "DEL^";
        // public const string C_UTAB = "TAB^";
        // public const string C_UESCAPE = "ESC^";
        // public const string C_ULEFT_SHIFT = "LSH^";
        // public const string C_URIGHT_SHIFT = "RSH^";
        // public const string C_UCTRL = "CTR^";
        // public const string C_UUP_ARROW = "UPA^";
        // public const string C_UDOWN_ARROW = "DOA^";
        // public const string C_ULEFT_ARROW = "LEA^";
        // public const string C_URIGHT_ARROW = "RIA^";

        // https://stackoverflow.com/questions/10261824/how-can-i-get-all-constants-of-a-type-by-reflection
        public static List<string> GetConstants() {
            Type charControl = typeof(CharacterControl);

            FieldInfo[] fieldInfos = charControl.GetFields(BindingFlags.Public |
                BindingFlags.Static | BindingFlags.FlattenHierarchy);

            List<string> constants = new List<string>();

            foreach(FieldInfo fieldInfo in fieldInfos) {
                if (fieldInfo.IsLiteral && 
                        !fieldInfo.IsInitOnly) {
                    constants.Add((string)fieldInfo.GetRawConstantValue());
                }
            }

            return constants;
        }
    }
}