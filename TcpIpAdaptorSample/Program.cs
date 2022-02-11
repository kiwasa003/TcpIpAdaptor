using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace TcpIpAdaptorSample
{
    /// <summary>
    /// Sample Program
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Output Type
        /// </summary>
        private enum OutputType
        {
            /// <summary>
            /// None
            /// </summary>
            N,

            /// <summary>
            /// Binary
            /// </summary>
            B,

            /// <summary>
            /// String
            /// </summary>
            S,
        }

        /// <summary>
        /// Entry Point
        /// </summary>
        /// <param name="args">Args</param>
        static void Main(string[] args)
        {
            if (args.Length == 4)
            {
                var listenIp = IPAddress.Loopback;
                if (int.TryParse(args[0], out int listenPort) &&
                    IPAddress.TryParse(args[1], out IPAddress targetIp) &&
                    int.TryParse(args[2], out int targetPort))
                {
                    if (!Enum.TryParse(args[3], out OutputType outputType))
                    {
                        outputType = OutputType.N;
                    }

                    Start(listenIp, listenPort, targetIp, targetPort, outputType);
                    return;
                }
            }

            Usage();

            return;
        }

        /// <summary>
        /// Usage
        /// </summary>
        private static void Usage()
        {
            Console.WriteLine($"Usage: {System.IO.Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetExecutingAssembly().Location)}.exe ListenPort TargetIp TargetPort [B (binary) or S (string) or N (none)]");
        }

        /// <summary>
        /// Start TcpIpAdaptor
        /// </summary>
        /// <param name="listenIp">Listen IP</param>
        /// <param name="listenPort">Listen Port</param>
        /// <param name="targetIp">Target IP</param>
        /// <param name="targetPort">Target Port</param>
        /// <param name="outputType">Output Type</param>
        private static void Start(IPAddress listenIp, int listenPort, IPAddress targetIp, int targetPort, OutputType outputType)
        {
            try
            {
                Console.WriteLine($"Start.");

                // Create TcpIpAdaptor instance
                var adaptor = new TcpIpAdaptor.TcpIpAdaptor(listenIp, listenPort, targetIp, targetPort);
                var cancel = false;

                var func = GetOutputFunc(outputType);
                if (func != null)
                {
                    var queue = new Queue<Tuple<string, byte[]>>();
                    var log = Task.Run(async () =>
                    {
                        while (!cancel)
                        {
                            Tuple<string, byte[]> tuple = null;
                            lock (queue)
                            {
                                if (queue.Count > 0)
                                {
                                    tuple = queue.Dequeue();
                                }
                            }

                            if (tuple != null)
                            {
                                func.Invoke(tuple.Item1, tuple.Item2);
                            }

                            await Task.Delay(10);
                        }
                    });

                    // Register Listen Port Data Received Event 
                    adaptor.ListenPortDataReceived += (sender, e) =>
                    {
                        lock (queue)
                        {
                            queue.Enqueue(new Tuple<string, byte[]>($"{listenIp}:{listenPort} -> {targetIp}:{targetPort}", e.Data));
                        }
                    };

                    // Register Target Port Data Received Event
                    adaptor.TargetPortDataReceived += (sender, e) =>
                    {
                        lock (queue)
                        {
                            queue.Enqueue(new Tuple<string, byte[]>($"{listenIp}:{listenPort} <- {targetIp}:{targetPort}", e.Data));
                        }
                    };
                }

                // Start Adaptor
                var task = adaptor.Start();

                // Wait for stop
                Console.ReadLine();

                // Stop Adaptor
                cancel = true;
                adaptor.Stop();

                Console.WriteLine($"Stop.");
            } 
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return;
        }

        /// <summary>
        /// Get Output Function
        /// </summary>
        /// <param name="type">Output Type</param>
        /// <returns>Output Function</returns>
        private static Action<string, byte[]> GetOutputFunc(OutputType type)
        {
            Action<string, byte[]> outputFunc = null;

            switch (type)
            {
                case OutputType.B:
                    // Binary
                    outputFunc = (str, data) => 
                    {
                        Console.WriteLine($"[{str}]");
                        Console.WriteLine($"{string.Join(" ", data.Select(d => $"{d:X2}"))}");
                    };
                    break;
                case OutputType.S:
                    // String
                    outputFunc = (str, data) =>
                    {
                        Console.WriteLine($"[{str}]");
                        Console.WriteLine($"{System.Text.Encoding.UTF8.GetString(data)}");
                    };
                    break;
                case OutputType.N:
                default:
                    // None
                    break;
            }

            return outputFunc;
        }
    }
}
