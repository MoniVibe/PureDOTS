using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace PureDOTS.Runtime.Networking
{
    /// <summary>
    /// Mock transport implementation for local loopback testing.
    /// Simulates network transport without actual networking.
    /// Later: replace with ENet, UDP, or Relay transports.
    /// </summary>
    public class LocalLoopbackTransport : INetTransport
    {
        private readonly Queue<Packet> _sendQueue = new Queue<Packet>();
        private readonly Dictionary<int, Queue<Packet>> _channelQueues = new Dictionary<int, Queue<Packet>>();

        private struct Packet
        {
            public NativeArray<byte> Data;
            public int Channel;
        }

        public void Send(byte* data, int size, int channel)
        {
            var packetData = new NativeArray<byte>(size, Allocator.Temp);
            UnsafeUtility.MemCpy(packetData.GetUnsafePtr(), data, size);

            var packet = new Packet
            {
                Data = packetData,
                Channel = channel
            };

            _sendQueue.Enqueue(packet);

            // For loopback, immediately queue to receive
            if (!_channelQueues.ContainsKey(channel))
            {
                _channelQueues[channel] = new Queue<Packet>();
            }
            _channelQueues[channel].Enqueue(packet);
        }

        public bool Receive(out byte* data, out int size, int channel)
        {
            data = null;
            size = 0;

            if (!_channelQueues.ContainsKey(channel) || _channelQueues[channel].Count == 0)
            {
                return false;
            }

            var packet = _channelQueues[channel].Dequeue();
            data = (byte*)packet.Data.GetUnsafePtr();
            size = packet.Data.Length;
            return true;
        }

        public void Disconnect()
        {
            // Clean up all queued packets
            while (_sendQueue.Count > 0)
            {
                var packet = _sendQueue.Dequeue();
                if (packet.Data.IsCreated)
                {
                    packet.Data.Dispose();
                }
            }

            foreach (var queue in _channelQueues.Values)
            {
                while (queue.Count > 0)
                {
                    var packet = queue.Dequeue();
                    if (packet.Data.IsCreated)
                    {
                        packet.Data.Dispose();
                    }
                }
            }

            _channelQueues.Clear();
        }
    }
}

