using IoT;
using P2P;

public class MainClass {

    static void Main(string[] args) {
        int pinOutput = 18,
            pinInput = 17;
        string[] bootstrapAddrs = Array.Empty<string>();
        string objectId;

        Console.WriteLine("Debug Mode? [Y/n]");
        string? debugString = Console.ReadLine();

        bool debug = string.IsNullOrEmpty(debugString) || debugString.Equals("y", StringComparison.OrdinalIgnoreCase);


        //ENTER BOOTSTRAP ADDRS
        if (!debug) {
            Console.WriteLine("Enter bootstrap addresses separated by comma:");
            string? bootstrapAddrsString = Console.ReadLine();
            // Split the input into an array of strings
            if (!string.IsNullOrEmpty(bootstrapAddrsString))
                bootstrapAddrs = bootstrapAddrsString.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }

        objectId = Guid.NewGuid().ToString();
        Console.WriteLine($"Object Id -> {objectId}");

        P2pManager p2pManager = new P2pManager(objectId, bootstrapAddrs, debug);


        //GPIO MANAGER
        using GpioManager gpioManager = new GpioManager(pinOutput, pinInput);

        P2pManager.OnVirtualStateChange += (sender, args) => gpioManager.HandleVirtualStateChange(args);
        gpioManager.OnPhysicalStateChange += (sender, args) => p2pManager.HandlePhysicalStateChange(args);

        p2pManager.StartPeer();

        Console.WriteLine("Waiting for connection...");

        Console.Write("Press ENTER to Toggle the Light");

        string? end = "";
        while (!end.Equals("exit")) {

            if(P2pManager.isConnected) {
                Console.ReadLine();

                gpioManager.ToggleLight();
            }
        }

        P2pManager.OnVirtualStateChange -= (sender, args) => gpioManager.HandleVirtualStateChange(args);
        gpioManager.OnPhysicalStateChange -= (sender, args) => p2pManager.HandlePhysicalStateChange(args);

        p2pManager.StopPeer();

    }
}
