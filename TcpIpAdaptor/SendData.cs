namespace TcpIpAdaptor
{
    /// <summary>
    /// Send Data
    /// </summary>
    internal class SendData
    {
        /// <summary>
        /// Binary Data
        /// </summary>
        public byte[] Data { get; private set; }

        /// <summary>
        /// Completed Flag
        /// </summary>
        public bool IsDone { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="data">Binary Data</param>
        public SendData(byte[] data)
        {
            this.Data = data;
            this.IsDone = false;
        }
    }
}
