using System.Runtime.InteropServices;

namespace P2P {

    public delegate void CallbackVirtualStateChange(string id, int state);
    public delegate void CallbackDelegate(string message);
    public delegate void ConnectNotify();

    public class P2pManager {

        public static event EventHandler<IotEventData> OnVirtualStateChange = delegate { };

        public static bool isConnected;

        readonly string _objectId;
        readonly string[] _bootstrapAddrs;
        readonly bool _isDebugMode;

        private const string LIBNAME = "libgo.so";

        #region External Methods
            [DllImport(LIBNAME, EntryPoint = "StartP2P", CallingConvention = CallingConvention.Cdecl)]
            public static extern void StartP2P(string[] bootstrapAddrs, int bootstrapCount, CallbackDelegate logCallback,
                ConnectNotify connectNotify, CallbackVirtualStateChange virtualState, bool debug, string playerId);

            [DllImport(LIBNAME, EntryPoint = "PropagateData", CallingConvention = CallingConvention.Cdecl)]
            public static extern void PropagateData(string sendData); 

            [DllImport(LIBNAME, EntryPoint = "ClosePeer", CallingConvention = CallingConvention.Cdecl)]
            public static extern void ClosePeer(); 
        #endregion

        #region Callback Methods
            public static void OnDebugLog(string log) {
                Console.WriteLine(log);
            }

            public static void PeerConnected() {
                isConnected = true; 
            }

            public static void VirtualStateChange(string id, int state) {
                Console.WriteLine($"ID -> {id} and STATE -> {state}");
                OnVirtualStateChange?.Invoke(null, new IotEventData(id, state));
            }
        #endregion

        public P2pManager(string id, string[] bootstrapAddrs, bool debug) {
            _bootstrapAddrs = bootstrapAddrs;
            _objectId = id;
            _isDebugMode = debug;
        }

        // STARTING PEER
        public void StartPeer() {
            StartP2P(_bootstrapAddrs, _bootstrapAddrs.Length, OnDebugLog, PeerConnected, VirtualStateChange, _isDebugMode, _objectId);
        }

        // Closing PEER
        public void StopPeer() => ClosePeer();

        public void PropagateLightState(IotEventData data) {
            Console.WriteLine($"NEW STATE PROPAGATED: {data.State}");

            PropagateData(data.DataToString());
        }

        public void HandlePhysicalStateChange(IotEventData args) {
            PropagateLightState(args);
        }
    }
}

