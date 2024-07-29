using System.Device.Gpio;
using IoT;
#if !CENTRALIZED_ARCH_TEST 
using P2P;
#else
using System.Net;
#endif

public class MainClass {

    static void Main(string[] args) {
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

        if(bootstrapAddrs.Length==0) Console.WriteLine([DEBUG MODE])

        objectId = Guid.NewGuid().ToString();
        Console.WriteLine($"Object Id -> {objectId}");

#if !CENTRALIZED_ARCH_TEST
        P2pManager p2pManager = new P2pManager(objectId, bootstrapAddrs, bootstrapAddrs.Length==0);


        //GPIO MANAGER
        GpioManager gpioManager = new GpioManager(pinOutput, pinInput);

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
        
        using var client = new HttpClient
        {
            DefaultRequestVersion =  HttpVersion.Version30,
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact
        };
        HttpResponseMessage resp = await client.GetAsync("https://cloudflare-quic.com");
        string body = await resp.Content.ReadAsStringAsync();

        Console.WriteLine(
            $"status: {resp.StatusCode}, version: {resp.Version}, " +
            $"body: {body.Substring(0, Math.Min(100, body.Length))}");
#endif

    }
}
