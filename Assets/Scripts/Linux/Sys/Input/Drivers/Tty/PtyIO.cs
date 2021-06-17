namespace Linux.Sys.Input.Drivers.Tty
{
    public static class PtyFlags {
        public const int ECHO            = 0b_0100_0000_0000;
        public const int BUFFERED        = 0b_0010_0000_0000;
        public const int SPECIAL_CHARS   = 0b_0001_0000_0000;
        public const int AUTO_CONTROL    = 0b_1000_0000_0000;
    }

    public static class PtyIoctl {
        public const ushort TIO_SET_DRIVER_FLAGS        = 0x1;
        public const ushort TIO_SET_FLAG                = 0x2;
        public const ushort TIO_SEND_KEY                = 0x3;
        public const ushort TIO_RCV_INPUT               = 0x4;
        public const ushort TIO_REMOVE_FRONT            = 0x5;
        public const ushort TIO_REMOVE_BACK             = 0x6;
        public const ushort TIO_TAB                     = 0x7;
        public const ushort TIO_ESCAPE                  = 0x8;
        public const ushort TIO_UP_ARROW                = 0x9;
        public const ushort TIO_DOWN_ARROW              = 0xA;
        public const ushort TIO_LEFT_ARROW              = 0xB;
        public const ushort TIO_RIGHT_ARROW             = 0xC;
        public const ushort TIO_CLEAR                   = 0xD;
        public const ushort TIO_SET_SPECIAL_CHARS       = 0xE;
        public const ushort TIO_DEL_SPECIAL_CHARS       = 0x11;
        public const ushort TIO_UNSET_FLAG              = 0x12;
        public const ushort TIO_SET_UNBUFFERED_CHARS    = 0x13;
        public const ushort TIO_ADD_UNBUFFERED_CHARS    = 0x14;
        public const ushort TIO_SET_PID_FLAG            = 0x15;
        public const ushort TIO_SET_PID                 = 0x16;
    }
}