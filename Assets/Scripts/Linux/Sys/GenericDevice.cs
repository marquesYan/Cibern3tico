using System.Collections.Generic;

namespace Linux.Sys
{    
    public enum DevType {
        KEYBOARD,
        DISPLAY,
        NETWORK,
    }

    public class GenericDevice {
        public readonly string Product;
        public readonly byte VendorId;
        public readonly DevType Type;

        public readonly Dictionary<string, string> Options;

        public GenericDevice(
            string product,
            byte vendorId,
            DevType type,
            Dictionary<string, string> options
        ) {
            Product = product;
            VendorId = vendorId;
            Type = type;
            Options = options;
        }

        public GenericDevice(
            string product,
            byte vendorId,
            DevType type
        ) : this(product, vendorId, type, new Dictionary<string, string>()) {}
    }
}