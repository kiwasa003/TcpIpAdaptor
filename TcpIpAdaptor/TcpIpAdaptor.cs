using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace TcpIpAdaptor
{
    /// <summary>
    /// TcpIpAdaptor Class
    /// </summary>
    public class TcpIpAdaptor
    {
        /// <summary>
        /// IP For Listen
        /// </summary>
        private readonly IPAddress listenIp;

        /// <summary>
        /// Port For Listen
        /// </summary>
        private readonly int listenPort;

        /// <summary>
        /// IP For Target
        /// </summary>
        private readonly IPAddress targetIp;

        /// <summary>
        /// Port For Target
        /// </summary>
        private readonly int targetPort;

        /// <summary>
        /// Adaptor is working or not
        /// </summary>
        private bool isConnected = false;

        /// <summary>
        /// Send Data Queue For Listen Port
        /// </summary>
        private readonly Queue<SendData> listenerSendQueue = new Queue<SendData>();

        /// <summary>
        /// Send Data Queue For Target Port
        /// </summary>
        private readonly Queue<SendData> targetSendQueue = new Queue<SendData>();

        /// <summary>
        /// Listen Port Data Received Event
        /// </summary>
        public event EventHandler<DataReceivedEventArgs> ListenPortDataReceived;

        /// <summary>
        /// Target Port Data Received Event
        /// </summary>
        public event EventHandler<DataReceivedEventArgs> TargetPortDataReceived;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="listenIp">IP For Listen</param>
        /// <param name="listenPort">Port For Listen</param>
        /// <param name="targetIp">IP For Target</param>
        /// <param name="targetPort">Port For Target</param>
        public TcpIpAdaptor(IPAddress listenIp, int listenPort, IPAddress targetIp, int targetPort)
        {
            this.listenIp = listenIp;
            this.listenPort = listenPort;
            this.targetIp = targetIp;
            this.targetPort = targetPort;
        }

        /// <summary>
        /// Start Adaptor
        /// </summary>
        /// <returns>Task</returns>
        public async Task Start()
        {
            TcpListener listener = null;
            try
            {
                // Setup Listener
                listener = new TcpListener(this.listenIp, this.listenPort);
                listener.Start();
                this.isConnected = true;

                while (this.isConnected)
                {
                    if (!listener.Pending())
                    {
                        await Task.Delay(10);
                        continue;
                    }

                    // Initialize Queue
                    lock (this.listenerSendQueue)
                    {
                        this.listenerSendQueue.Clear();
                    }
                    lock (this.targetSendQueue)
                    {
                        this.targetSendQueue.Clear();
                    }

                    // Connect Listen Port
                    using (var listenerSocket = listener.AcceptTcpClient())
                    using (var targetSocket = new TcpClient())
                    {
                        // Connect Target Port
                        targetSocket.Connect(this.targetIp, this.targetPort);

                        // Connect between Listen Port and Target Port
                        await this.SocketConnect(listenerSocket, targetSocket);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (listener != null)
                {
                    listener.Stop();
                }
                this.isConnected = false;
            }
        }

        /// <summary>
        /// Stop Adaptor
        /// </summary>
        public void Stop()
        {
            this.isConnected = false;
        }

        /// <summary>
        /// Send data to Listen Port
        /// </summary>
        /// <param name="data">Binary Data</param>
        /// <returns>Result</returns>
        public async Task<bool> ListenerSend(byte[] data)
        {
            var sendData = new SendData(data);
            lock (this.listenerSendQueue)
            {
                // Set data to queue
                this.listenerSendQueue.Enqueue(sendData);
            }

            // Wait to send
            while (this.isConnected && !sendData.IsDone)
            {
                await Task.Delay(10);
            }

            return sendData.IsDone;
        }

        /// <summary>
        /// Send data to Target Port
        /// </summary>
        /// <param name="data">Binary Data</param>
        /// <returns>Result</returns>
        public async Task<bool> TargetSend(byte[] data)
        {
            var sendData = new SendData(data);
            lock (this.targetSendQueue)
            {
                // Set data to queue
                this.targetSendQueue.Enqueue(sendData);
            }

            // Wait to send
            while (this.isConnected && !sendData.IsDone)
            {
                await Task.Delay(10);
            }

            return sendData.IsDone;
        }

        /// <summary>
        /// Connect between Listen Port and Target Port
        /// </summary>
        /// <param name="listenerSocket">Socket For Listen</param>
        /// <param name="targetSocket">Socket For Target</param>
        /// <returns>Task</returns>
        private async Task SocketConnect(TcpClient listenerSocket, TcpClient targetSocket)
        {
            while (this.isConnected && listenerSocket.Available() && targetSocket.Available())
            {
                // Check Listen Port
                var data = listenerSocket.ReadData();
                if (data.Length > 0)
                {
                    // Received data from Listen Port
                    var e = new DataReceivedEventArgs(data);

                    // Data Received Event
                    this.ListenPortDataReceived?.Invoke(this, e);

                    if (e.Type == ActionType.Pass)
                    {
                        // Pass (Send to Target Port)
                        targetSocket.WriteData(e.Data);
                    }
                    else if (e.Type == ActionType.Back)
                    {
                        // Back (Send back to Listen Port)
                        listenerSocket.WriteData(e.Data);
                    }
                    else
                    {
                        // Not send to Anywhere
                    }
                }
                else
                {
                    // Not Received
                    lock (this.listenerSendQueue)
                    {
                        if (this.listenerSendQueue.Count > 0)
                        {
                            // Send Data in queue to Listen Port
                            var sendData = this.listenerSendQueue.Dequeue();
                            listenerSocket.WriteData(sendData.Data);

                            // Set Completed Flag
                            sendData.IsDone = true;
                        }
                    }
                }

                // Check Target Port
                data = targetSocket.ReadData();
                if (data.Length > 0)
                {
                    // Received data from Target Port
                    var e = new DataReceivedEventArgs(data);
                    
                    // Data Received Event
                    this.TargetPortDataReceived?.Invoke(this, e);

                    if (e.Type == ActionType.Pass)
                    {
                        // Pass (Send to Listen Port)
                        listenerSocket.WriteData(e.Data);
                    }
                    else if (e.Type == ActionType.Back)
                    {
                        // Back (Send back to Target Port)
                        targetSocket.WriteData(e.Data);
                    }
                    else
                    {
                        // Not send to Anywhere
                    }
                }
                else
                {
                    // Not Received
                    lock (this.targetSendQueue)
                    {
                        if (this.targetSendQueue.Count > 0)
                        {
                            // Send Data in queue to Target Port
                            var sendData = this.targetSendQueue.Dequeue();
                            targetSocket.WriteData(sendData.Data);

                            // Set Completed Flag
                            sendData.IsDone = true;
                        }
                    }
                }

                await Task.Delay(10);
            }

            return;
        } 
    }
}
