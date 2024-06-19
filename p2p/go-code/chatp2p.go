package main

// typedef void (*transfer_data)(const char*);
// extern void debugLogMakeCallback(const char* log, transfer_data logFunc) {
//     logFunc(log);
// }
// typedef void (*notify)();
// extern void connectNotifyMakeCallback(notify notifyFunc) {
//     notifyFunc();
// }
// typedef void (*virtual_state)(const char* id, int state);
// extern void virtualStateChangeMakeCallback(const char* id, int state, virtual_state virtualStateChangeFunc) {
//     virtualStateChangeFunc(id, state);
// }
import "C"
import (
	"bufio"
	"context"
	"crypto/rand"
	"errors"
	"fmt"
	"io"
	"strconv"
	"strings"
	"time"

	"github.com/libp2p/go-libp2p"
	dht "github.com/libp2p/go-libp2p-kad-dht"
	"github.com/libp2p/go-libp2p/core/crypto"
	"github.com/libp2p/go-libp2p/core/host"
	"github.com/libp2p/go-libp2p/core/network"
	"github.com/libp2p/go-libp2p/core/peer"
	"github.com/libp2p/go-libp2p/core/peerstore"
	"github.com/libp2p/go-libp2p/p2p/discovery/routing"
	"github.com/libp2p/go-libp2p/p2p/discovery/util"

	"github.com/multiformats/go-multiaddr"
)

type debugLog func(log string)
type connectNotify func()
type virtualStateChange func(id string, state int)

var logCallback debugLog 
var connectNotifyCallback connectNotify
var virtualStateChangeCallback virtualStateChange 

var readWriter []*bufio.ReadWriter 
var hostData host.Host
var contextVar context.Context
var kademliaDht *dht.IpfsDHT
var discovery *routing.RoutingDiscovery

// Will only be run on the receiving side.
func handleStream(s network.Stream) {
	logCallback("Got a new stream!")
	connectNotifyCallback()

	// Create a buffer stream for non-blocking read and write.
	if(cap(readWriter) != len(readWriter)) {
		rw := bufio.NewReadWriter(bufio.NewReader(s), bufio.NewWriter(s))
		readWriter = append(readWriter, rw) 

		go readData(rw)
	} else {
		logCallback("Buffer full")
	}
}

func readData(rw *bufio.ReadWriter) {

	logCallback("Reading Data...")
	for {
		str, _ := rw.ReadString('\n')

		id := strings.Split(str, ":")[0]
		state, err := strconv.Atoi(strings.TrimSpace(strings.Split(str, ":")[1]))

		if err != nil {
			logCallback(fmt.Sprintf("State not an integer: %s\n", err))
		} else {
			logCallback(fmt.Sprintf("ID:%s STATE:%d\n", id, state))
			virtualStateChangeCallback(id, state)
		}

	}
}

func writeData(sendData string) {
	//TODO: Need to handle one readWriter per peer 

  for i := 0; i < len(readWriter); i++ {
		readWriter[i].WriteString(fmt.Sprintf("%s\n", sendData))
		readWriter[i].Flush()
  }
}

func (p *PeerManager) startProtocolP2P(cBootstrapPeers []string, goDebugLog debugLog, goConnectNotify connectNotify, goVirtualStateChange virtualStateChange, debug bool, playerId string) {

	readWriter = make([]*bufio.ReadWriter, 0, 5)

	ctx, cancel := context.WithCancel(context.Background())
	defer cancel()

	contextVar = ctx
	logCallback = goDebugLog
	connectNotifyCallback = goConnectNotify
	virtualStateChangeCallback = goVirtualStateChange

	var r io.Reader = rand.Reader

	h, err := makeHost(r)
	if err != nil {
		logCallback(fmt.Sprintf("Failed to create host: %s\n", err))
		return
	}

	hostData = h

	startPeer(hostData, handleStream)

	logCallback(fmt.Sprintf("Debug mode: %t\n", debug))

	var bootstrapPeers []peer.AddrInfo

	if(debug) {
		bootstrapPeers = make([]peer.AddrInfo, len(dht.DefaultBootstrapPeers))
		for i, addr := range dht.DefaultBootstrapPeers {
			peerinfo, _ := peer.AddrInfoFromP2pAddr(addr)
			bootstrapPeers[i] = *peerinfo
		}
	} else {
		bootstrapPeers = make([]peer.AddrInfo, len(cBootstrapPeers))
		for i, addr := range cBootstrapPeers {
			peerinfo, _ := peer.AddrInfoFromString(addr)
			bootstrapPeers[i] = *peerinfo
		}
	}

	kademliaDht, err = dht.New(ctx, h, dht.BootstrapPeers(bootstrapPeers...))
	if err != nil {
		logCallback(fmt.Sprintf("Failed to create DHT: %s\n", err))
	}

	// Bootstrap the DHT. In the default configuration, this spawns a Background
	// thread that will refresh the peer table every five minutes.
	if err = kademliaDht.Bootstrap(ctx); err != nil {
		logCallback(fmt.Sprintf("Failed to bootstrap the DHT: %s\n", err))
	}

	// Wait a bit to let bootstrapping finish (really bootstrap should block until it's ready, but that isn't the case yet.)
	time.Sleep(5 * time.Second)

	discovery = routing.NewRoutingDiscovery(kademliaDht)
	util.Advertise(ctx, discovery, playerId)

	// Wait until the peer is terminated
	<-p.done
	logCallback("Closing peer...")
}


func makeHost(randomness io.Reader) (host.Host, error) {
	// Creates a new RSA key pair for this host.
	prvKey, _, err := crypto.GenerateKeyPairWithReader(crypto.RSA, 2048, randomness)
	if err != nil {
		logCallback(fmt.Sprintf("Failed to generate private key: %s\n", err))
		return nil, err
	}

	// 0.0.0.0 will listen on any interface device.
	sourceMultiAddrTCP, _ := multiaddr.NewMultiaddr("/ip4/0.0.0.0/tcp/4001")
	sourceMultiAddrUDP, _ := multiaddr.NewMultiaddr("/ip4/0.0.0.0/udp/4001/quic-v1")

	// libp2p.New constructs a new libp2p Host.
	// Other options can be added here.
	return libp2p.New(
		libp2p.ListenAddrs(sourceMultiAddrTCP, sourceMultiAddrUDP),
		libp2p.Identity(prvKey),

		// Attempt to open ports using uPNP for NATed hosts.
		libp2p.NATPortMap(),
		libp2p.EnableHolePunching(),
	)
}

func startPeer(h host.Host, streamHandler network.StreamHandler) {
	// Set a function as stream handler.
	// This function is called when a peer connects, and starts a stream with this protocol.
	// Only applies on the receiving side.
	h.SetStreamHandler("/chat/1.0.0", streamHandler)

	logCallback(fmt.Sprintf("My peer ID -> %s", h.ID()))
}

func connectToPeer(peerID string) {

	peerIdObj, err := findPeer(peerID)
	if err != nil {
		logCallback(fmt.Sprintf("%s\n", err))
		return
	}

	rw, err := connectToPeerAction(peerIdObj)
	if err != nil {
		logCallback(fmt.Sprintf("Failed to start protocol: %s\n", err))
		return
	}

	readWriter = append(readWriter, rw)

	go readData(rw)
}

func findPeer(peerID string) (peer.ID, error){
	//var peerFound peer.AddrInfo
        var foundPeers []peer.AddrInfo

        logCallback("Searching for peer...\n")
//	for i := 0; i < 5; i++ {
		
        peerChan, err := discovery.FindPeers(contextVar, peerID)
        if err != nil {
                logCallback(fmt.Sprintf("Failed to find peer: %s\n", err))
        }

        //peerFound = <-peerChan

        //logCallback(fmt.Sprintf("Peer found: %s\n", peerFound.ID.String()))
        for peerFound := range peerChan {
            if peerFound.ID.String() == "" {
                continue
            }

            logCallback(fmt.Sprintf("Peer found: %s\n", peerFound.ID.String()))
            foundPeers = append(foundPeers, peerFound)


        }

        if len(foundPeers) == 0 {
                return "", errors.New("peer not found")
        }

	/*	
		if peerFound.ID.String() != peerID {
			time.Sleep(2 * time.Second)
			logCallback(".\n")
		} else {
			break
		}
	}


	logCallback("Found peer!\n")*/

	// Add the destination's peer multiaddress in the peerstore.
	// This will be used during connection and stream creation by libp2p.
	//hostData.Peerstore().AddAddrs(peerFound.ID, peerFound.Addrs, peerstore.PermanentAddrTTL)

        for _, peer := range foundPeers {
            if !strings.HasPrefix(peer.ID.String(), "12D3") {
                // Add the peer address to the peerstore
                hostData.Peerstore().AddAddrs(peer.ID, peer.Addrs, peerstore.PermanentAddrTTL)

                return peer.ID, nil
            }
        }


        return "", errors.New("no valid peers found")
}

func connectToPeerAction(peerID peer.ID) (*bufio.ReadWriter, error) {

	// Start a stream with the destination.
	// Multiaddress of the destination peer is fetched from the peerstore using 'peerId'.
	s, err := hostData.NewStream(context.Background(), peerID, "/chat/1.0.0")
	if err != nil {
		logCallback(fmt.Sprintf("Failed to create new stream: %s\n", err))
		return nil, err
	}
	logCallback("Established connection to destination")

	// Create a buffered stream so that read and writes are non-blocking.
	return bufio.NewReadWriter(bufio.NewReader(s), bufio.NewWriter(s)), nil
}

func (p *PeerManager) closePeer() {
	if(hostData != nil) {
		hostData.Close()
		p.done <- true
	}
}

func main() {}
