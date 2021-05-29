using Linux;

namespace Linux.Sys
{    
    public static class Class {
        public const string INPUT = "input"; 
        public const string NET = "net"; 
        public const string BLOCK = "block"; 
    }

    public class Pci {
        public string Vendor { get; protected set; }
        public string Product { get; protected set; }
        public int Major { get; protected set; }
        public string Slot { get; protected set; }
        public int Minor;

        public Pci(
            string product,
            string vendor,
            string slot,
            int major,
            int minor
        ) {
            Product = product;
            Vendor = vendor;
            Slot = slot;
            Major = major;
            Minor = minor;
        }
    }
}