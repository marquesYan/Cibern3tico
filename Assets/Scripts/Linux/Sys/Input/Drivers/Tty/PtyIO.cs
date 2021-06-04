namespace Linux.Sys.Input.Drivers.Tty
{
    public static class PtyFlags {
        public const ushort ECHO          = 0b_0100_0000_0000;
        public const ushort BUFFERED      = 0b_0010_0000_0000;
        public const ushort SPECIAL_CHARS = 0b_0001_0000_0000;
    }

    public enum PtyIoctl : ushort {
        TIO_SET_DRIVER_FLAGS,
        TIO_SET_ATTR,
        TIO_SEND_KEY,
    }
}