namespace PureDOTS.Runtime.Networking
{
    /// <summary>
    /// Transport-agnostic interface for network communication.
    /// Plan adapter interface now; later drop in ENet, UDP, or Relay transports.
    /// </summary>
    public interface INetTransport
    {
        /// <summary>
        /// Sends data over the network.
        /// </summary>
        /// <param name="data">Data to send</param>
        /// <param name="size">Size of data in bytes</param>
        /// <param name="channel">Channel ID (0 = reliable, 1+ = unreliable)</param>
        void Send(byte* data, int size, int channel);

        /// <summary>
        /// Receives data from the network.
        /// </summary>
        /// <param name="data">Output buffer for received data</param>
        /// <param name="size">Output size of received data</param>
        /// <param name="channel">Channel ID</param>
        /// <returns>True if data was received, false otherwise</returns>
        bool Receive(out byte* data, out int size, int channel);

        /// <summary>
        /// Disconnects and cleans up transport resources.
        /// </summary>
        void Disconnect();
    }
}

