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

            Pci netCard = machine.AttachUSB(
                "0000:00:06.0",
                new GenericDevice(
                    "Unity Virtual Network Card",
                    UnityVendorId,
                    DevType.NETWORK,
                    new Dictionary<string, string>() {
                        {"hwAddress", "d4:01:29:9d:10:e2"}
                    }
                )
            );

            UnityNetwork unityNetwork = GameObject.FindObjectsOfType<UnityNetwork>()[0];
            VirtualCable vtCable = unityNetwork.Hub.Connect(netCard);

            var usbDriver = new UsbControllerDriver();
            usbDriver.Register(new VirtualNetDriver(vtCable));
            machine.BiosDrivers.Add(usbDriver);

            Kernel = new Linux.Kernel(Application.persistentDataPath, machine);
            Kernel.Bootstrap();
        }

        IPciDriver GetUnityDriver() {
            var usbDriver = new UsbControllerDriver();
            usbDriver.Register(new UnityKbdDriver());
            usbDriver.Register(new UnityDisplayDriver(1024 ^ 2));
            return usbDriver;
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