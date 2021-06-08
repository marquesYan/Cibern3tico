namespace Linux.Sys.Input.Drivers.Tty
{
    public static class PtyFlags {
        public const ushort ECHO            = 0b_0100_0000_0000;
        public const ushort BUFFERED        = 0b_0010_0000_0000;
        public const ushort SPECIAL_CHARS   = 0b_0001_0000_0000;
        public const ushort AUTO_CONTROL    = 0b_1000_0000_0000;
    }

    public static class PtyIoctl {
        public const ushort TIO_SET_DRIVER_FLAGS    = 0x1;
        public const ushort TIO_SET_ATTR            = 0x2;
        public const ushort TIO_SEND_KEY            = 0x3;
        public const ushort TIO_RCV_INPUT           = 0x4;
        public const ushort TIO_REMOVE_FRONT        = 0x5;
        public const ushort TIO_REMOVE_BACK         = 0x6;
        public const ushort TIO_TAB                 = 0x7;
        public const ushort TIO_ESCAPE              = 0x8;
        public const ushort TIO_UP_ARROW            = 0x9;
        public const ushort TIO_DOWN_ARROW          = 0xA;
        public const ushort TIO_LEFT_ARROW          = 0xB;
        public const ushort TIO_RIGHT_ARROW         = 0xC;
        public const ushort TIO_CLEAR               = 0xD;

    }
}