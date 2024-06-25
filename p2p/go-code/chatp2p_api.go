package main

// typedef void (*transfer_data)(const char*);
// void debugLogMakeCallback(const char* log, transfer_data logFunc);
// typedef void (*notify)();
// void connectNotifyMakeCallback(notify connectNotify);
// typedef void (*virtual_state)(const char*, int);
// void virtualStateChangeMakeCallback(const char* id, int state, virtual_state virtualStateChangeFunc);
import "C"
import (
	"unsafe"
)

// Struct to hold the peer and control channel
type PeerManager struct {
    done chan bool // Channel to signal termination
    disconnect chan bool // Channel to signal termination
}

var pm *PeerManager

//export StartP2P
func StartP2P(bootstrapPeers **C.char, bootstrapCount int, debugLog C.transfer_data, connectNotify C.notify, virtualStateChange C.virtual_state, debug bool, playerId *C.char) {
	pm = &PeerManager {
		done: make(chan bool),
		disconnect: make(chan bool),
	}

	goDebugLog := func(log string) {
		C.debugLogMakeCallback(C.CString(log), debugLog)
	}

	goConnectNotify := func() {
		C.connectNotifyMakeCallback(connectNotify)
	}

	goVirtualStateChange := func(id string, state int) {
		C.virtualStateChangeMakeCallback(C.CString(id), C.int(state), virtualStateChange)
	}

	goBootstrapPeers := make([]string, bootstrapCount)

	for i := 0; i < bootstrapCount; i++ {
		cBootstrapPointer := (*[1<<30 - 1]*C.char)(unsafe.Pointer(bootstrapPeers))[i]
		goBootstrapPeers[i] = C.GoString(cBootstrapPointer)
	}

	go pm.startProtocolP2P(goBootstrapPeers, goDebugLog, goConnectNotify, goVirtualStateChange, debug, C.GoString(playerId))
}

//export WriteData
func WriteData(sendData *C.char) {
	go writeData(C.GoString(sendData))
}

//export ConnectToPeer
func ConnectToPeer(peerID *C.char) {
	go connectToPeer(C.GoString(peerID))
}

//export ClosePeer
func ClosePeer() {
	go pm.closePeer()
}

