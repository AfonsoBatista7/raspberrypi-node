// main.cs
using System;
using System.Runtime.InteropServices;

public delegate void CallbackDelegate(string message);
public delegate void ConnectNotify();

public class MainClass {

    private static bool _isConnected = false;

    [DllImport("libgo.dll")]
    public static extern void StartP2P(string[] bootstrapAddrs, int bootstrapCount, CallbackDelegate receiveMessageCallback,
     CallbackDelegate logCallback, ConnectNotify connectNotify, bool debug, string playerId);

    [DllImport("libgo.dll")]
    public static extern void WriteData(string sendData); 

    [DllImport("libgo.dll")]
    public static extern void ClosePeer(); 

    [DllImport("libgo.dll")]
    public static extern void ConnectToPeer(string peerID); 

    public static void OnGetMessage(string message) {
        Console.WriteLine($"From another player: {message.TrimEnd('\n')}");
    }
    public static void OnDebugLog(string log) {
        Console.WriteLine(log);
    }

    public static void PeerConnected() {
        _isConnected = true; 
    }

    static void Main(string[] args) {

        bool debug = true;
        string[] bootstrapAddrs = new string[0];
        string playerId = "0";

        if(!debug) {
            Console.WriteLine("Enter bootstrap addresses separated by comma:");
            string bootstrapAddrsString = Console.ReadLine();
            // Split the input into an array of strings
            bootstrapAddrs = bootstrapAddrsString.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        } else {
            Console.WriteLine("Write your player ID: ");
            playerId = Console.ReadLine();
        }


        StartP2P(bootstrapAddrs, bootstrapAddrs.Length, OnGetMessage, OnDebugLog, PeerConnected, debug, playerId);

        Console.WriteLine("Connect to some peer? y or n");
        if (Console.ReadLine().Equals("y")) {
            Console.Write("Enter peer ID: ");
            string peerID = Console.ReadLine();

            ConnectToPeer(peerID);
        } else {
            Console.WriteLine("Waiting for connection...");
            while(!_isConnected) { } 
        }
        
        string userInput = "";
        while(!userInput.Equals("exit")){
            userInput = Console.ReadLine();
            WriteData($"{userInput}");
        }

        ClosePeer();


    }
}
