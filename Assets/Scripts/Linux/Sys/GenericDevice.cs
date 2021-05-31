using System;

namespace Linux.Sys
{    
    public enum DevType {
        KEYBOARD,
        CONSOLE,
    }

    public class GenericDevice {
        public string Product { get; protected set; }
        public byte VendorId { get; protected set; }
        public DevType Type { get; protected set; }

        public GenericDevice(
            string product,
            byte vendorId,
            DevType type
        ) {
            Product = product;
            VendorId = vendorId;
            Type = type;
        }
    }
}