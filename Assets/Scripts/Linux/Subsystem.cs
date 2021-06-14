using System.Collections.Generic;
using UnityEngine;
using Linux.Sys;
using Linux.Sys.Drivers;
using Linux.Sys.Input.Drivers;
using Linux.Sys.IO;
using Linux.Net;

namespace Linux
{    
    public class Subsystem : MonoBehaviour {
        const int UnityVendorId = 255;

        protected Linux.Kernel Kernel;

        protected Pci DisplayPci;
        protected Pci KeyboardPci;

        public UnityNetwork UnityNetwork;

        void Start() {
            var machine = new VirtualMachine("I4440FX", 4);

            machine.BiosDrivers.Add(GetUnityDriver());

            DisplayPci = machine.AttachUSB(
                "0000:00:04.0",
                new GenericDevice(
                    "Unity Game Display",
                    UnityVendorId,
                    DevType.DISPLAY
                )
            );

            KeyboardPci = machine.AttachUSB(
                "0000:00:05.0",
                new GenericDevice(
                    "Unity Game Keyboard",
                    UnityVendorId,
                    DevType.KEYBOARD
                )
            );

            Pci netCard = machine.AttachNetworkCard(
                "0000:00:06.0",
                new GenericDevice(
                    "Unity Virtual Network",
                    UnityVendorId,
                    DevType.NETWORK,
                    new Dictionary<string, string>() {
                        {"hwAddress", "d4:01:29:9d:10:e2"}
                    }
                )
            );
            
            machine.BiosDrivers.Add(GetUnityNetworkDriver(netCard));

            Kernel = new Linux.Kernel(Application.persistentDataPath, machine);
            Kernel.Bootstrap();

            Kernel.NetTable.LookupName("vt0").IPAddresses.Add(
                NetworkAddress.FromString("10.0.0.1/24")
            );
        }

        IPciDriver GetUnityDriver() {
            var usbDriver = new UsbControllerDriver();
            usbDriver.Register(new UnityKbdDriver());
            usbDriver.Register(new UnityDisplayDriver(1024 ^ 2));
            return usbDriver;
        }

        IPciDriver GetUnityNetworkDriver(Pci netCard) {
            VirtualCable vtCable = UnityNetwork.Hub.Connect(netCard);

            return new NetworkControllerDriver(vtCable);
        }

        void OnApplicationQuit() {
            Kernel.Shutdown();
        }

        void Update() {
            if (!Kernel.IsRunning) {
                Application.Quit();
            }

            Kernel.Interrupt(KeyboardPci, IRQCode.READ);
        }

        void OnGUI() {
            Kernel.Interrupt(DisplayPci, IRQCode.WRITE);
        }
    }
}