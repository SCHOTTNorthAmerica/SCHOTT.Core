using SCHOTT.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace SCHOTT.Core.Communication.Ethernet
{
    /// <summary>
    /// A AsyncSocket base class, used in higher level connection classes.
    /// </summary>
    public class AsyncSocket
    {
        /// <summary>
        /// The MessageBroker used by the AsyncSocket class.
        /// </summary>
        protected readonly MessageBroker MessageBroker = new MessageBroker();

        /// <summary>
        /// Class with information on connection status. Used when registering for status updates in the Async Socket.
        /// </summary>
        public class ConnectionUpdateArgs
        {
            /// <summary>
            /// Bool for if the socket is connected.
            /// </summary>
            public bool IsConnected { get; private set; }

            /// <summary>
            /// The IPEndpoint of the socket if connected.
            /// </summary>
            public IPEndPoint IpEndpoint { get; private set; }

            /// <summary>
            /// Create a ConnectionUpdateArgs object.
            /// </summary>
            public ConnectionUpdateArgs()
            {
                IsConnected = false;
                IpEndpoint = null;
            }

            /// <summary>
            /// Create a ConnectionUpdateArgs object with starting parameters.
            /// </summary>
            /// <param name="isConnected">Bool for if the socket is connected.</param>
            /// <param name="ipEndpoint">The IPEndpoint of the socket if connected.</param>
            public ConnectionUpdateArgs(bool isConnected, IPEndPoint ipEndpoint)
            {
                IsConnected = isConnected;
                IpEndpoint = ipEndpoint;
            }

            /// <summary>
            /// Function to test for equality between two ConnectionUpdateArgs objects.
            /// </summary>
            /// <param name="newArgs">ConnectionUpdateArgs object to compare against.</param>
            /// <returns>True = Objects are equal, False = Objects differ</returns>
            public bool IsEqual(ConnectionUpdateArgs newArgs)
            {
                return IsConnected == newArgs.IsConnected &&
                       IpEndpoint != null && newArgs.IpEndpoint != null &&
                       IpEndpoint.Equals(newArgs.IpEndpoint);
            }

            /// <summary>
            /// Function to set one object based on the parameters of another.
            /// </summary>
            /// <param name="newArgs">The object to copy.</param>
            public void SetFrom(ConnectionUpdateArgs newArgs)
            {
                IsConnected = newArgs.IsConnected;
                IpEndpoint = newArgs.IpEndpoint;
            }
        }

        /// <summary>
        /// Allows the user to register for Connection Updates on the socket.
        /// </summary>
        /// <param name="context">Allows the user to specify how the update should arrive for syncing with GUI applications.</param>
        /// <param name="action">The lambda expression to execute on updates.</param>
        public void RegisterConnectionUpdate(MessageBroker.MessageContext context, Action<ConnectionUpdateArgs> action)
        {
            MessageBroker.Register("ConnectionUpdate", context, action);
        }

        private readonly ConnectionUpdateArgs _lastArgs = new ConnectionUpdateArgs();
        private void RunConnectionUpdate(ConnectionUpdateArgs args)
        {
            if (_lastArgs.IsEqual(args))
                return;

            _lastArgs.SetFrom(args);
            MessageBroker.RunActions("ConnectionUpdate", args);
        }

        /// <summary>
        /// The IPEndpoint of the socket if connected.
        /// </summary>
        public IPEndPoint IpEndpoint { get; private set; }

        // variables used in creating the socket
        private Socket _socketClient;
        private NetworkStream _networkStream;
        private IPAddress _serverIpAddress;
        
        /// <summary>
        /// Disconnect the socket if currently connected, otherwise do nothing.
        /// </summary>
        public virtual void Disconnect()
        {
            _socketClient?.Close();

            // check to see if someone is listening for connection updates
            RunConnectionUpdate(new ConnectionUpdateArgs(IsConnected, null));
        }

        /// <summary>
        /// Check to see if the socket is connected.
        /// </summary>
        /// <returns>True = connected, False = not connected.</returns>
        public bool IsConnected => _socketClient?.Connected == true;

        /// <summary>
        /// Socket Connection Status
        /// </summary>
        public enum ConnectionStatus
        {
            /// <summary>
            /// Connected to target server
            /// </summary>
            Connected,

            /// <summary>
            /// Cannot find the supplied address on the network
            /// </summary>
            CannotResolveAddress,

            /// <summary>
            /// Supplied port is not valid
            /// </summary>
            InvalidPortNumber,

            /// <summary>
            /// Unable to connect to the target server
            /// </summary>
            UnknownError
        }

        /// <summary>
        /// Attempt to connect a TCP socket to a server.
        /// </summary>
        /// <param name="address">IP address or Host Name of the server.</param>
        /// <param name="port">Port of the server.</param>
        /// <param name="connectionTimeoutMilliseconds">Timeout for the connect function in milliseconds.</param>
        /// <returns>Returns status of connection</returns>
        public virtual ConnectionStatus Connect(string address, string port, int connectionTimeoutMilliseconds = 500)
        {
            // Disconnect the socket if currently connected
            _socketClient?.Close();

            // Parse the Server Address
            var addresslist = Dns.GetHostAddresses(address);
            if (addresslist.Length > 0)
            {
                _serverIpAddress = addresslist[0];
            }
            else
            {
                return ConnectionStatus.CannotResolveAddress;
            }

            // Parse the Port Number
            int serverPort;
            if (!int.TryParse(port, out serverPort))
            {
                return ConnectionStatus.InvalidPortNumber;
            }

            // Create the Socket Instance
            _socketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socketClient.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

            // Create the End Point
            IpEndpoint = new IPEndPoint(_serverIpAddress, serverPort);

            // attempt to connect to server
            var result = _socketClient.BeginConnect(IpEndpoint, null, null);

            // wait for the socket to connect, close the socket after timeout period
            if (!result.AsyncWaitHandle.WaitOne(connectionTimeoutMilliseconds, true))
            {
                Disconnect();
            }

            if (IsConnected)
            {
                // if connection was successful, create stream for passing data
                _networkStream = new NetworkStream(_socketClient);

                // start listening for data asynchronously
                WaitForData();
            }

            // check to see if someone is listening for connection updates
            RunConnectionUpdate(new ConnectionUpdateArgs(IsConnected, IpEndpoint));

            // Return if we were able to connect
            return IsConnected ? ConnectionStatus.Connected : ConnectionStatus.UnknownError;
        }

        /// <summary>
        /// Sends data over the connected ethernet socket
        /// </summary>
        /// <param name="dataArray">Byte array of data to send.</param>
        public void SendData(byte[] dataArray)
        {
            SendData(dataArray, 0, dataArray.Length);
        }

        /// <summary>
        /// Sends data over the connected ethernet socket
        /// </summary>
        /// <param name="dataArray">Byte array of data to send.</param>
        /// <param name="offset">Index of dataArray to begin transmission at.</param>
        /// <param name="bytesToSend">How many bytes to send from array</param>
        public void SendData(byte[] dataArray, int offset, int bytesToSend)
        {
            if (!IsConnected) return;

            try
            {
                _networkStream.Write(dataArray, 0, bytesToSend);
                _networkStream.Flush();
            }
            catch
            {
                /* if the socket is already closed this will throw an exception
                this is a limitiation of the .net async sockets */
            }
        }

        /// <summary>
        /// Setup an async listener for incoming data on the connected socket.
        /// </summary>
        private void WaitForData()
        {
            try
            {
                var theSocPkt = new SocketPacket(_socketClient);

                _socketClient.BeginReceive(theSocPkt.DataBuffer, 0, theSocPkt.DataBuffer.Length,
                    SocketFlags.None, OnDataReceived, theSocPkt);
            }
            catch
            {
                /* if the socket is already closed this will throw an exception
                this is a limitiation of the .net async sockets */
            }
        }

        /// <summary>
        /// Used to store information from the async listener
        /// </summary>
        private class SocketPacket
        {
            public readonly Socket ThisSocket;
            public readonly byte[] DataBuffer = new byte[2048];

            public SocketPacket(Socket callingSocket)
            {
                ThisSocket = callingSocket;
            }
        }

        /// <summary>
        /// Handle data that arives on the async listener.
        /// </summary>
        /// <param name="asyn">SocketPacket structure that holds the recieved data.</param>
        private void OnDataReceived(IAsyncResult asyn)
        {
            var theSockId = (SocketPacket)asyn.AsyncState;

            try
            {
                var bytesRead = theSockId.ThisSocket.EndReceive(asyn);
                
                // allow any derived classes to process the data directly
                ProcessReceivedData(theSockId.DataBuffer.Take(bytesRead).ToList());
            }
            catch
            {
                /* if the socket is already closed this will throw a ObjectDisposedException */
            }

            WaitForData();
        }

        /// <summary>
        /// Function to be overridden by derived classes. Processing of data should be done here.
        /// </summary>
        /// <param name="data">The data received from the socket.</param>
        protected virtual void ProcessReceivedData(List<byte> data) { }
    }
}
