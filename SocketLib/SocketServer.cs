﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using System.Diagnostics;

namespace co.Surfea.net
{
    public class SocketServer
    {
        #region Member Variables

        private StreamSocketListener _listener;
        private List<StreamSocket> _connections;

        private int _port;

        #endregion

        #region Events

        /// <summary>
        ///  A message is defined as a '\n' delimited line received from a client.
        /// </summary>
        public event EventHandler MessageEvent;

        /// <summary>
        /// Event to fire when new data is received from the client.
        /// </summary>
        /// <param name="e">Information to pass back</param>
        public virtual void OnMessage(MessageEventArgs e)
        {
            if (MessageEvent != null)
                MessageEvent(this, e);
        }

        #endregion

        # region Constructors

        // Default to telnet (23)
        public SocketServer() : this(23) { }

        public SocketServer(int port)
        {
            _listener = new StreamSocketListener();
            _connections = new List<StreamSocket>();

            _port = port;
        }

        #endregion

        /// <summary>
        /// Start the socket server
        /// </summary>
        public async void Start()
        {
            _listener.ConnectionReceived += _listener_ConnectionReceived;
            await _listener.BindServiceNameAsync(_port.ToString());

            Debug.WriteLine("Server started - listening on {0}", _listener.Information.LocalPort);
        }

        /// <summary>
        /// Callback for when a new client connects to the server.
        /// </summary>
        /// <param name="sender">Socket listener</param>
        /// <param name="args">Contains information about the newly connected client.</param>
        void _listener_ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            _connections.Add(args.Socket);

            Debug.WriteLine(string.Format("Incoming connection from {0}", args.Socket.Information.RemoteHostName.DisplayName));

            ProcessData(args.Socket);
        }

        /// <summary>
        /// Routine to process incoming data from the client socket.  Will parse the data by line
        /// </summary>
        /// <param name="socket">Client socket</param>
        async private void ProcessData(StreamSocket socket)
        {
            string buffer = "";

            var reader = new DataReader(socket.InputStream);
            reader.InputStreamOptions = InputStreamOptions.Partial;

            // indicates we have a complete packet of data
            bool gotNewline = false;

            while (!gotNewline)
            {

                var count = await reader.LoadAsync(512);

                if (count > 0)
                {
                    // Read until the newline
                    buffer = buffer + reader.ReadString(count);
                }
                else
                {
                    Debug.WriteLine(string.Format("Disconnected (from {0})", socket.Information.RemoteHostName.DisplayName));
                    return;
                }

                if (buffer.Contains("\n"))
                {
                    // TODO: Take care of case where there may be more than one ticket per connection
                    int nlPosition = buffer.IndexOf('\n');
                    string ticket = buffer.Substring(0, nlPosition + 1).Trim();
                    buffer = buffer.Substring(nlPosition + 1);
                    Debug.WriteLine("Final msg: {0}\r\n", ticket);
                    OnMessage(new MessageEventArgs(ticket));
                    buffer = "";
                    continue;
                }
            }


            return;
        }
    }

    #region Events

    /// <summary>
    /// Event class to pass back received client data.
    /// </summary>
    public class MessageEventArgs : EventArgs
    {
        public MessageEventArgs(string msg)
        {
            this.Message = msg;
        }

        public string Message { get; private set; }
    }

    #endregion

}
