using System.Runtime.InteropServices;

namespace P2P {

    public delegate void CallbackVirtualStateChange(int id, int state);
    public delegate void CallbackDelegate(string message);
    public delegate void ConnectNotify();

    public class P2pManager {

        public static event EventHandler<P2PEventArgs> OnVirtualStateChange = delegate { };

        public static bool isConnected;

        readonly string _objectId;
        readonly string[] _bootstrapAddrs;
        readonly bool _isDebugMode;

        #if LINUX
            private const string LIBNAME = "libgo.so";
        #else
            private const string LIBNAME = "libgo.dll";
        #endif

        #region External Methods
            [DllImport(LIBNAME, EntryPoint = "StartP2P", CallingConvention = CallingConvention.Cdecl)]
            public static extern void StartP2P(string[] bootstrapAddrs, int bootstrapCount, CallbackDelegate receiveMessageCallback,
             CallbackDelegate logCallback, ConnectNotify connectNotify, CallbackVirtualStateChange virtualState, bool debug, string playerId);

            [DllImport(LIBNAME, EntryPoint = "WriteData", CallingConvention = CallingConvention.Cdecl)]
            public static extern void WriteData(string sendData); 

            [DllImport(LIBNAME, EntryPoint = "ClosePeer", CallingConvention = CallingConvention.Cdecl)]
            public static extern void ClosePeer(); 

            [DllImport(LIBNAME, EntryPoint = "ConnectToPeer", CallingConvention = CallingConvention.Cdecl)]
            public static extern void ConnectToPeer(string peerID);
        #endregion

        #region Callback Methods
            public static void OnGetMessage(string message) {
                Console.WriteLine($"From another player: {message.TrimEnd('\n')}");
            }

            public static void OnDebugLog(string log) {
                Console.WriteLine(log);
            }

            public static void PeerConnected() {
                isConnected = true; 
            }

            public static void VirtualStateChange(int id, int state) {
                OnVirtualStateChange?.Invoke(null, new P2PEventArgs(id, state));
            }
        #endregion

        public P2pManager(string id, string[] bootstrapAddrs, bool debug) {
            _bootstrapAddrs = bootstrapAddrs;
            _objectId = id;
            _isDebugMode = debug;
        }

        // STARTING PEER
        public void StartPeer() {
            StartP2P(_bootstrapAddrs, _bootstrapAddrs.Length, OnGetMessage, OnDebugLog, PeerConnected, VirtualStateChange, _isDebugMode, _objectId);
        }

        // Closing PEER
        public void StopPeer() => ClosePeer();

        public void PropagateLightState(int state) {
            Console.WriteLine($"NEW STATE PROPAGATED: {state}");
        }

        public void HandlePhysicalStateChange(GpioEventArgs args) {
            PropagateLightState(args.State);
        }

    }
}

public class P2PEventArgs : EventArgs {
    public int State { get; }
    public int Id { get; }

    public P2PEventArgs(int id, int state) {
        State = state;
        Id = id;
    }
}
