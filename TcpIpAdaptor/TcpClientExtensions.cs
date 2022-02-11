using System;
using System.Linq;
using System.Net.Sockets;

namespace TcpIpAdaptor
{
    /// <summary>
    /// TcpClient Class Extension
    /// </summary>
    internal static class TcpClientExtensions
    {
        /// <summary>
        /// Check TcpClient instance available
        /// </summary>
        /// <param name="src">TcpClient instance</param>
        /// <returns>Available or not</returns>
        public static bool Available(this TcpClient src)
        {
            return src.Connected &&
                   src.Client.Connected &&
                   (!src.Client.Poll(1, SelectMode.SelectRead) || src.Available > 0);
        }

        /// <summary>
        /// Read Stream Data
        /// </summary>
        /// <param name="src">TcpClient instance</param>
        /// <returns>Received Data</returns>
        public static byte[] ReadData(this TcpClient src)
        {
            var data = Array.Empty<byte>();
            var stream = src.GetStream();
            while (stream.DataAvailable)
            {
                var d = new byte[1024];
                var num = stream.Read(d, 0, d.Length);
                data = data.Concat(d.Take(num)).ToArray();
            }
            return data;
        }

        /// <summary>
        /// Write Stream Data 
        /// </summary>
        /// <param name="src">TcpClient istance</param>
        /// <param name="data">Binary Data</param>
        public static void WriteData(this TcpClient src, byte[] data)
        {
            src.GetStream().Write(data, 0, data.Length);

            return;
        }
    }
}
