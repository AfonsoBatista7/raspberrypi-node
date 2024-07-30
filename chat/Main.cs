using System.Device.Gpio;
using IoT;
#if !CENTRALIZED_ARCH_TEST 
using P2P;
#else
using Centralized;
#endif

public class MainClass {

    private static bool _keepRunning = true;

#if CENTRALIZED_ARCH_TEST
    static async Task Main(string[] args) {
#else 
    static void Main(string[] args) {
#endif
        int pinOutput = 18,
            pinInput = 17;

        string objectId = "",
               connectToPeerID = "";
        string[] bootstrapAddrs = Array.Empty<string>();

        for (int i = 0; i < args.Length; i++) {
            switch (args[i].ToLower()) {
#if !CENTRALIZED_ARCH_TEST
                case "-bootstrapaddrs":
                    if (i + 1 < args.Length) {
                        bootstrapAddrs = args[i + 1].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        i++;
                    }
                    break;
                case "-peerid":
                    if (i + 1 < args.Length) {
                        connectToPeerID = args[i + 1];
                        i++;
                    }
                    break;
#endif
            }
        }

        if (bootstrapAddrs.Length == 0) Console.WriteLine("[DEBUG MODE]");

        objectId = Guid.NewGuid().ToString();
        Console.WriteLine($"Object Id -> {objectId}");

        //GPIO MANAGER
        var gpioManager = new GpioManager(pinOutput, pinInput);

#if !CENTRALIZED_ARCH_TEST
        var p2pManager = new P2pManager(objectId, bootstrapAddrs, bootstrapAddrs.Length==0);

        P2pManager.OnVirtualStateChange += (sender, args) => gpioManager.HandleVirtualStateChange(args);
        gpioManager.OnPhysicalStateChange += (sender, args) => p2pManager.HandlePhysicalStateChange(args);

        p2pManager.StartPeer();

        Console.WriteLine("Waiting for connection...");


        string? end = Console.ReadLine();

        P2pManager.OnVirtualStateChange -= (sender, args) => gpioManager.HandleVirtualStateChange(args);
        gpioManager.OnPhysicalStateChange -= (sender, args) => p2pManager.HandlePhysicalStateChange(args);

        p2pManager.StopPeer();

#else

        Console.WriteLine("CENTRALIZED");

        Console.CancelKeyPress += delegate (object? sender, ConsoleCancelEventArgs e) {
            e.Cancel = true;
            _keepRunning = false;
        };

        var httpServer = new HttpServer();
        var httpClient = new Client();

        httpServer.Start();

        httpServer.OnVirtualStateChange += (sender, args) => gpioManager.HandleVirtualStateChange(args);
        gpioManager.OnPhysicalStateChange += (sender, args) => httpClient.HandlePhysicalStateChange(args);

        while (_keepRunning) { }

        httpServer.Stop();
        httpServer.OnVirtualStateChange -= (sender, args) => gpioManager.HandleVirtualStateChange(args);
        gpioManager.OnPhysicalStateChange -= (sender, args) => httpClient.HandlePhysicalStateChange(args);

        Console.WriteLine("Exiting gracefully...");
#endif
    }
}
