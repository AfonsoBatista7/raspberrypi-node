package main

// typedef void (*transfer_data)(const char*);
// extern void debugLogMakeCallback(const char* log, transfer_data logFunc) {
//     logFunc(log);
// }
// typedef void (*notify)();
// extern void connectNotifyMakeCallback(notify notifyFunc) {
//     notifyFunc();
// }
// typedef void (*virtual_state)(const char*, int);
// extern void virtualStateChangeMakeCallback(const char* id, int state, virtual_state virtualStateChangeFunc) {
//     virtualStateChangeFunc(id, state);
// }
import "C"
import (
	"bufio"
	"context"
	"crypto/rand"
	"fmt"
	"io"
	"strconv"
	"strings"
	"time"

	"github.com/libp2p/go-libp2p"
	dht "github.com/libp2p/go-libp2p-kad-dht"
	pubsub "github.com/libp2p/go-libp2p-pubsub"
	"github.com/libp2p/go-libp2p/core/crypto"
	"github.com/libp2p/go-libp2p/core/host"
	"github.com/libp2p/go-libp2p/core/network"
	"github.com/libp2p/go-libp2p/core/peer"
	"github.com/libp2p/go-libp2p/p2p/discovery/routing"
	"github.com/libp2p/go-libp2p/p2p/discovery/util"
	quic "github.com/libp2p/go-libp2p/p2p/transport/quic"
	"github.com/multiformats/go-multiaddr"
)

type debugLog func(log string)
type connectNotify func()
type virtualStateChange func(id string, state int)

var logCallback debugLog 
var connectNotifyCallback connectNotify
var virtualStateChangeCallback virtualStateChange 

var hostData host.Host
var contextVar context.Context
var kademliaDht *dht.IpfsDHT
var discovery *routing.RoutingDiscovery
var topic *pubsub.Topic

var rendezvousString = "METAVERSE"

// Will only be run on the receiving side.
func handleStream(s network.Stream) {
	logCallback("Got a new stream!")
	connectNotifyCallback()

	// Create a buffer stream for non-blocking read and write.
	rw := bufio.NewReadWriter(bufio.NewReader(s), bufio.NewWriter(s))
	go readData(s, rw)
}

func readData(s network.Stream, rw *bufio.ReadWriter) {

	logCallback("Reading Data...")
	for {
		str, _ := rw.ReadString('\n')

		logCallback(fmt.Sprintf("Received data: %s\n", str))
		if(str == "") {
			s.Close()
			logCallback("Closing connection...")
			return
		}

		id := strings.Split(str, ":")[0]
		state, err := strconv.Atoi(strings.TrimSpace(strings.Split(str, ":")[1]))

		if err != nil {
			logCallback(fmt.Sprintf("State not an integer: %s\n", err))
		} else {
			virtualStateChangeCallback(id, state)
		}
	}
}

func (p *PeerManager) startProtocolP2P(cBootstrapPeers []string, goDebugLog debugLog, goConnectNotify connectNotify, goVirtualStateChange virtualStateChange, debug bool, playerId string) {

	ctx, cancel := context.WithCancel(context.Background())
	defer cancel()

	contextVar = ctx
	logCallback = goDebugLog
	connectNotifyCallback = goConnectNotify
	virtualStateChangeCallback = goVirtualStateChange

	var r io.Reader = rand.Reader

	host, err := makeHost(r)
	if err != nil {
		logCallback(fmt.Sprintf("Failed to create host: %s\n", err))
		return
	}

	logCallback(fmt.Sprintf("My peer ID -> %s", host.ID()))

	hostData = host

	host.SetStreamHandler("/metaverse/1.0.0", handleStream)

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

	kademliaDht, err = dht.New(ctx, host, dht.BootstrapPeers(bootstrapPeers...))
	if err != nil {
		logCallback(fmt.Sprintf("Failed to create DHT: %s\n", err))
	}

	// Bootstrap the DHT. In the default configuration, this spawns a Background
	// thread that will refresh the peer table every five minutes.
	if err = kademliaDht.Bootstrap(ctx); err != nil {
		logCallback(fmt.Sprintf("Failed to bootstrap the DHT: %s\n", err))
	}

	time.Sleep(5 * time.Second)

	// create a new PubSub service using the GossipSub router
	gossipSub, err := pubsub.NewGossipSub(ctx, host)
	if err != nil {
		logCallback(fmt.Sprintf("Failed to create GossipSub: %s\n", err))	
		p.done <- true
	}

	go p.Discover(ctx, host, kademliaDht, playerId)

	// join the pubsub topic
	room := "iot"
	topic, err = gossipSub.Join(room)
	if err != nil {
		logCallback(fmt.Sprintf("Failed to join topic: %s\n", err))	
		p.done <- true
	}

	// Wait until the peer is terminated
	<- p.done
	logCallback("Closing peer...")
}

func (p *PeerManager) Discover(ctx context.Context, host host.Host, dht *dht.IpfsDHT, playerId string) {
	discovery = routing.NewRoutingDiscovery(kademliaDht)
	util.Advertise(ctx, discovery, playerId)
	util.Advertise(ctx, discovery, rendezvousString)

	ticker := time.NewTicker(time.Second * 1)
	defer ticker.Stop()

	for {
		select {
		case <- p.done:
			return
		case <-ticker.C:

			peers, _ := util.FindPeers(ctx, discovery, rendezvousString)

			for _, peer := range peers {
				if peer.ID == host.ID() { continue }

				if host.Network().Connectedness(peer.ID) != network.Connected {
					_, err := host.Network().DialPeer(ctx, peer.ID)

					if err != nil { continue }

					logCallback(fmt.Sprintf("Connected to peer %s\n", peer.ID.String()))
				}
			}
		}
	}
}

// publish to topic
func publish(stateData string) {
	if len(stateData) != 0 {

		// publish message to topic
		bytes := []byte(stateData)
		topic.Publish(contextVar, bytes)
	}
}

func makeHost(randomness io.Reader) (host.Host, error) {
	// Creates a new RSA key pair for this host.
	prvKey, _, err := crypto.GenerateKeyPairWithReader(crypto.RSA, 2048, randomness)
	if err != nil {
		logCallback(fmt.Sprintf("Failed to generate private key: %s\n", err))
		return nil, err
	}

	// 0.0.0.0 will listen on any interface device.
	//sourceMultiAddrTCP, _ := multiaddr.NewMultiaddr("/ip4/0.0.0.0/tcp/4001")
	sourceMultiAddrUDP, _ := multiaddr.NewMultiaddr("/ip4/0.0.0.0/udp/4001/quic-v1")

	// libp2p.New constructs a new libp2p Host.
	// Other options can be added here.
	return libp2p.New(
		libp2p.ListenAddrs(/*sourceMultiAddrTCP,*/ sourceMultiAddrUDP),
		libp2p.Transport(quic.NewTransport),
		libp2p.Identity(prvKey),

		// Attempt to open ports using uPNP for NATed hosts.
		libp2p.NATPortMap(),
		libp2p.EnableHolePunching(),
	)

}

func (p *PeerManager) closePeer() {
	if(hostData != nil) {
		topic.Close()
		hostData.Close()
		p.done <- true
	}
}

func main() {}
