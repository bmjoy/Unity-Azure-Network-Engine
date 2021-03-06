using Utility;
using ReliableUdp.Utility;

namespace ReliableUdp.Channel
{
	using ReliableUdp.Enums;
	using ReliableUdp.Packet;

	public class UnreliableOrderedChannel : IUnreliableOrderedChannel
	{
		private SequenceNumber localSequence = new SequenceNumber(0);
		private SequenceNumber remoteSequence = new SequenceNumber(0);
		private readonly ConcurrentQueue<UdpPacket> outgoingPackets = new ConcurrentQueue<UdpPacket>();
		private UdpPeer peer;

		public void Initialize(UdpPeer peer)
		{
			this.peer = peer;
		}

		public void AddToQueue(UdpPacket packet)
		{
			this.outgoingPackets.Enqueue(packet);
		}

		public bool SendNextPacket()
		{
			UdpPacket packet = this.outgoingPackets.Dequeue();
			if (packet == null)
				return false;

			this.localSequence.Value++;
			packet.Sequence = new SequenceNumber(this.localSequence.Value);
			this.peer.SendRawData(packet);
			this.peer.Recycle(packet);
			return true;
		}

		public void ProcessPacket(UdpPacket packet)
		{
			if ((packet.Sequence - this.remoteSequence).Value > 0)
			{
				this.remoteSequence = packet.Sequence;
				this.peer.AddIncomingPacket(packet, ChannelType.UnreliableOrdered);
			}
		}

		public int PacketsInQueue
		{
			get
			{
				return 0;
			}
		}

		public void ProcessAck(UdpPacket packet)
		{
		}

		public void SendAcks()
		{
		}
	}
}