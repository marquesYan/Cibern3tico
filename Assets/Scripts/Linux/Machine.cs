using System;
using System.Collections.Generic;
using Linux.Sys;
using Linux.Sys.Drivers;

namespace Linux
{    
    public class VirtualMachine {
        protected int pciCount = 0;

        public Dictionary<Pci, GenericDevice> Chassis { get; protected set; }

        public string ChipsetEmulation { get; protected set; }
        public int CpuCores { get; protected set; }
        public List<IPciDriver> BiosDrivers { get; protected set; }

        public VirtualMachine(string chipsetEmulation, int cpuCores) {
            ChipsetEmulation = chipsetEmulation;
            CpuCores = cpuCores;
            Chassis = new Dictionary<Pci, GenericDevice>();
            BiosDrivers = new List<IPciDriver>();
        }

        public Pci AttachUSB(string slot, GenericDevice usb) {
            pciCount++;

            var usbPci = new Pci(
                "xHCI Host Controller",
                "SAD",
                slot,
                189,
                pciCount * 16,
                PciClass.INPUT
            );

            Chassis.Add(usbPci, usb);

            return usbPci;
        }

        public Pci AttachNetworkCard(string slot, GenericDevice connector) {
            pciCount++;

            var netPci = new Pci(
                "Gigabit Ethernet Controller",
                "SAD",
                slot,
                8086,
                pciCount * 16,
                PciClass.NET
            );

            Chassis.Add(netPci, connector);

            return netPci;
        }
    }
}