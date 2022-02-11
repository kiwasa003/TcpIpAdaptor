namespace TcpIpAdaptor
{
    /// <summary>
    /// Action Type
    /// </summary>
    public enum ActionType
    {
        /// <summary>
        /// Pass (Sender To Receiver)
        /// </summary>
        Pass,

        /// <summary>
        /// Back (Sender To Sender)
        /// </summary>
        Back,

        /// <summary>
        /// Stop (Not Send)
        /// </summary>
        Stop,
    }
}
