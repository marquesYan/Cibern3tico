namespace Linux.PseudoTerminal {
    public static class CharacterControl {
        public const string C_BLOCK = "^BLK";

        public const string C_DBACKSPACE = "^BKS";
        public const string C_DDELETE = "^DEL";
        public const string C_DTAB = "^TAB";
        public const string C_DESCAPE = "^ESC";
        public const string C_DLEFT_SHIFT = "^LSH";
        public const string C_DRIGHT_SHIFT = "^RSH";
        public const string C_DCTRL = "^CTR";
        public const string C_DUP_ARROW = "^UPA";
        public const string C_DDOWN_ARROW = "^DOA";
        public const string C_DLEFT_ARROW = "^LEA";
        public const string C_DRIGHT_ARROW = "^RIA";

        public const string C_UBACKSPACE = "BKS^";
        public const string C_UDELETE = "DEL^";
        public const string C_UTAB = "TAB^";
        public const string C_UESCAPE = "ESC^";
        public const string C_ULEFT_SHIFT = "LSH^";
        public const string C_URIGHT_SHIFT = "RSH^";
        public const string C_UCTRL = "CTR^";
        public const string C_UUP_ARROW = "UPA^";
        public const string C_UDOWN_ARROW = "DOA^";
        public const string C_ULEFT_ARROW = "LEA^";
        public const string C_URIGHT_ARROW = "RIA^";
    }
}