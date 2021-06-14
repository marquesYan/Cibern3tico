using System.Collections.Generic;
using UnityEngine;
using Linux.Sys;
using Linux.Sys.Drivers;
using Linux.Sys.Input.Drivers;
using Linux.Sys.IO;
using Linux.Net;
using Linux;

public class UnityPc : MonoBehaviour {
    const int UnityVendorId = 255;

    protected Kernel Kernel;

    protected Pci DisplayPci;
    protected Pci KeyboardPci;

    public UnityNetwork UnityNetwork;

    public string IpAddress;

    public string MacAddress;

    public bool Client = false;

    void Start() {
        var machine = new VirtualMachine("I4440FX", 4);

        if (Client) {
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
        }

        Pci netCard = machine.AttachNetworkCard(
            "0000:00:06.0",
            new GenericDevice(
                "Unity Virtual Network",
                UnityVendorId,
                DevType.NETWORK,
                new Dictionary<string, string>() {
                    {"hwAddress", MacAddress}
                }
            )
        );
        
        machine.BiosDrivers.Add(GetUnityNetworkDriver(netCard));

        Kernel = new Linux.Kernel(Application.persistentDataPath, machine);
        Kernel.Bootstrap();

        Kernel.NetTable.LookupName("vt0").IPAddresses.Add(
            NetworkAddress.FromString(IpAddress)
        );
    }

    IPciDriver GetUnityDriver() {
        var usbDriver = new UsbControllerDriver();
        usbDriver.Register(new UnityKbdDriver());
        usbDriver.Register(new UnityDisplayDriver(1024 ^ 2));
        return usbDriver;
    }

    IPciDriver GetUnityNetworkDriver(Pci netCard) {
        VirtualEthernetTransport transport = UnityNetwork.Hub.Connect(netCard);

        return new NetworkControllerDriver(transport);
    }

    void OnApplicationQuit() {
        Kernel.Shutdown();
    }

    void Update() {
        if (!Kernel.IsRunning) {
            Application.Quit();
        }

        if (Client) {
            Kernel.Interrupt(KeyboardPci, IRQCode.READ);
        }
    }

    void OnGUI() {
        if (Client) {
            Kernel.Interrupt(DisplayPci, IRQCode.WRITE);
        }
    }
}