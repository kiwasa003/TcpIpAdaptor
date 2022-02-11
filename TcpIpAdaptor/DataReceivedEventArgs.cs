namespace TcpIpAdaptor
{
    /// <summary>
    /// Data Received Event Args
    /// </summary>
    public class DataReceivedEventArgs
    {
        /// <summary>
        /// Received Binary Data
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// Action Type
        /// </summary>
        public ActionType Type { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="data">Received Data</param>
        public DataReceivedEventArgs(byte[] data)
        {
            this.Data = data;
            this.Type = ActionType.Pass;
        }
    }
}
