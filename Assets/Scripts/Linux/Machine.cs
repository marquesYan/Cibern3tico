using System;
using System.Collections.Generic;
using Linux.Sys;

namespace Linux
{    
    public class VirtualMachine {
        protected Random Rnd;

        protected Dictionary<Pci, GenericDevice> Chassis;
        protected int usbCount = 0;

        public string ChipsetEmulation { get; protected set; }
        public int CpuCores { get; protected set; }

        public VirtualMachine(string chipsetEmulation, int cpuCores) {
            ChipsetEmulation = chipsetEmulation;
            CpuCores = cpuCores;
            Chassis = new Dictionary<Pci, GenericDevice>();
            Rnd = new Random();
        }

        public string AttachUSB(
            string product,
            byte vendorId,
            DevType type
        ) {
            usbCount++;

            var usb = new GenericDevice(
                product,
                vendorId,
                Rnd.Next(32).ToString(),
                type
            );

            var usbPci = new Pci(
                "xHCI Host Controller",
                "SAD",
                "0000:00:04.0",
                189,
                usbCount * 16,
                PciClass.INPUT
            );

            Chassis.Add(usbPci, usb);

            return usb.Id;
        }
    }
}