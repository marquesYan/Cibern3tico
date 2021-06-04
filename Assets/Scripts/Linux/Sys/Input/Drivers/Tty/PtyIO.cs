namespace Linux.Sys.Input.Drivers.Tty
{
    public static class PtyFlags {
        public const ushort ECHO          = 0b_0100_0000_0000;
        public const ushort BUFFERED      = 0b_0010_0000_0000;
        public const ushort SPECIAL_CHARS = 0b_0001_0000_0000;
        public const ushort AUTO_CONTROL  = 0b_1000_0000_0000;
    }

    public enum PtyIoctl : ushort {
        TIO_SET_DRIVER_FLAGS,
        TIO_SET_ATTR,
        TIO_SEND_KEY,
        TIO_REMOVE_FRONT,
        TIO_REMOVE_BACK,
        TIO_TAB,
        TIO_ESCAPE,
        TIO_UP_ARROW,
        TIO_DOWN_ARROW,
        TIO_LEFT_ARROW,
        TIO_RIGHT_ARROW,
    }
}