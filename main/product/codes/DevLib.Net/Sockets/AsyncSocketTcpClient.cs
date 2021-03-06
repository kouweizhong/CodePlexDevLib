﻿//-----------------------------------------------------------------------
// <copyright file="AsyncSocketTcpClient.cs" company="YuGuan Corporation">
//     Copyright (c) YuGuan Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace DevLib.Net.Sockets
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;

    /// <summary>
    /// Async Socket Tcp Client Class.
    /// </summary>
    public class AsyncSocketTcpClient : MarshalByRefObject, IDisposable
    {
        /// <summary>
        /// Field _disposed.
        /// </summary>
        private bool _disposed = false;

        /// <summary>
        /// Counter of the total bytes received by AsyncSocketTcpClient.
        /// </summary>
        private long _totalBytesReceived;

        /// <summary>
        /// Counter of the total bytes sent by AsyncSocketTcpClient.
        /// </summary>
        private long _totalBytesSent;

        /// <summary>
        /// The socket used to connect to server.
        /// </summary>
        private Socket _clientSocket;

        /// <summary>
        /// Field _connectSocketAsyncEventArgs.
        /// </summary>
        private SocketAsyncEventArgs _connectSocketAsyncEventArgs;

        /// <summary>
        /// Field _receiveSocketAsyncEventArgs.
        /// </summary>
        private SocketAsyncEventArgs _receiveSocketAsyncEventArgs;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncSocketTcpClient" /> class.
        /// </summary>
        public AsyncSocketTcpClient()
            : this(IPAddress.Loopback.ToString(), IPEndPoint.MinPort, 8192)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncSocketTcpClient" /> class.
        /// </summary>
        /// <param name="remoteIP">The IP address of the remote host.</param>
        /// <param name="remotePort">The port number of the remote host.</param>
        /// <param name="bufferSize">Buffer size to use with receive data.</param>
        public AsyncSocketTcpClient(string remoteIP, int remotePort, int bufferSize = 8192)
        {
            this.RemoteIP = remoteIP;
            this.RemotePort = remotePort;
            this.BufferSize = bufferSize;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="AsyncSocketTcpClient" /> class.
        /// </summary>
        ~AsyncSocketTcpClient()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Client Connected Event.
        /// </summary>
        public event EventHandler<AsyncSocketSessionEventArgs> Connected;

        /// <summary>
        /// Client Disconnected Event.
        /// </summary>
        public event EventHandler<AsyncSocketSessionEventArgs> Disconnected;

        /// <summary>
        /// Client Data Received Event.
        /// </summary>
        public event EventHandler<AsyncSocketSessionEventArgs> DataReceived;

        /// <summary>
        /// Client Data Sent Event.
        /// </summary>
        public event EventHandler<AsyncSocketSessionEventArgs> DataSent;

        /// <summary>
        /// Error Occurred Event.
        /// </summary>
        public event EventHandler<AsyncSocketErrorEventArgs> ErrorOccurred;

        /// <summary>
        /// Client Started Event.
        /// </summary>
        public event EventHandler Started;

        /// <summary>
        /// Client Stopped Event.
        /// </summary>
        public event EventHandler Stopped;

        /// <summary>
        /// Gets or sets buffer size to use with receive data.
        /// </summary>
        public int BufferSize
        {
            get;
            set;
        }

        /// <summary>
        /// Gets total bytes received by AsyncSocketTcpClient.
        /// </summary>
        public long TotalBytesReceived
        {
            get
            {
                return Interlocked.Read(ref this._totalBytesReceived);
            }
        }

        /// <summary>
        /// Gets total bytes sent by AsyncSocketTcpClient.
        /// </summary>
        public long TotalBytesSent
        {
            get
            {
                return Interlocked.Read(ref this._totalBytesSent);
            }
        }

        /// <summary>
        /// Gets current session Id.
        /// </summary>
        public int SessionId
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the IP address of the remote host.
        /// </summary>
        public string RemoteIP
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the port number of the remote host.
        /// </summary>
        public int RemotePort
        {
            get;
            set;
        }

        /// <summary>
        /// Gets a value indicating whether AsyncSocketTcpClient is working or not.
        /// </summary>
        public bool IsConnected
        {
            get
            {
                this.CheckDisposed();

                try
                {
                    return this._clientSocket == null ? false : this._clientSocket.IsBound;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Establishes a connection to a remote host.
        /// </summary>
        /// <param name="throwOnError">true to throw any exception that occurs.-or- false to ignore any exception that occurs.</param>
        /// <returns>true if succeeded; otherwise, false.</returns>
        public bool Start(bool throwOnError = false)
        {
            this.CheckDisposed();

            if (!this.IsConnected)
            {
                try
                {
                    IPEndPoint remoteIPEndPoint = new IPEndPoint(IPAddress.Parse(this.RemoteIP), this.RemotePort);

                    this.CloseConnectSocketAsyncEventArgs();
                    this.CloseClientSocket();
                    this.CloseReceiveSocketAsyncEventArgs();

                    this._clientSocket = new Socket(remoteIPEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    this._clientSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    this._clientSocket.UseOnlyOverlappedIO = true;

                    this._connectSocketAsyncEventArgs = new SocketAsyncEventArgs();
                    this._connectSocketAsyncEventArgs.RemoteEndPoint = remoteIPEndPoint;
                    this._connectSocketAsyncEventArgs.UserToken = this._clientSocket;
                    this._connectSocketAsyncEventArgs.Completed += this.ConnectSocketAsyncEventArgsCompleted;

                    this.SessionId = this._clientSocket.GetHashCode();

                    try
                    {
                        bool willRaiseEvent = this._clientSocket.ConnectAsync(this._connectSocketAsyncEventArgs);
                        if (!willRaiseEvent)
                        {
                            this.ProcessConnect();
                        }

                        Debug.WriteLine(AsyncSocketTcpClientConstants.TcpClientStartSucceeded);

                        this.RaiseEvent(this.Started);

                        return true;
                    }
                    catch
                    {
                        throw;
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(AsyncSocketTcpClientConstants.TcpClientStartException);

                    InternalLogger.Log(e);

                    this.RaiseEvent(
                                    this.ErrorOccurred,
                                    new AsyncSocketErrorEventArgs(
                                                                  AsyncSocketTcpClientConstants.TcpClientStartException,
                                                                  e,
                                                                  AsyncSocketErrorCodeEnum.TcpClientStartException));

                    if (throwOnError)
                    {
                        throw;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Closes the socket connection.
        /// </summary>
        /// <param name="throwOnError">true to throw any exception that occurs.-or- false to ignore any exception that occurs.</param>
        /// <returns>true if succeeded; otherwise, false.</returns>
        public bool Stop(bool throwOnError = false)
        {
            this.CheckDisposed();

            if (this.IsConnected)
            {
                try
                {
                    this.CloseReceiveSocketAsyncEventArgs();
                    this.CloseConnectSocketAsyncEventArgs();
                    this.CloseClientSocket();

                    Debug.WriteLine(AsyncSocketTcpClientConstants.TcpClientStopSucceeded);

                    this.RaiseEvent(this.Stopped);

                    return true;
                }
                catch (Exception e)
                {
                    Debug.WriteLine(AsyncSocketTcpClientConstants.TcpClientStopException);

                    InternalLogger.Log(e);

                    this.RaiseEvent(
                                    this.ErrorOccurred,
                                    new AsyncSocketErrorEventArgs(
                                                                  AsyncSocketTcpClientConstants.TcpClientStopException,
                                                                  e,
                                                                  AsyncSocketErrorCodeEnum.TcpClientStopException));

                    if (throwOnError)
                    {
                        throw;
                    }
                }
                finally
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }
            }

            return false;
        }

        /// <summary>
        /// Sends data asynchronously to a connected socket.
        /// </summary>
        /// <param name="buffer">Data to send.</param>
        /// <param name="userToken">A user or application object associated with this asynchronous socket operation.</param>
        /// <returns>true if succeeded; otherwise, false.</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Reviewed.")]
        public bool Send(byte[] buffer, object userToken = null)
        {
            this.CheckDisposed();

            if (!this.IsConnected)
            {
                return false;
            }

            if (this._clientSocket != null)
            {
                SocketAsyncEventArgs sendSocketAsyncEventArgs = null;

                try
                {
                    sendSocketAsyncEventArgs = new SocketAsyncEventArgs();
                    sendSocketAsyncEventArgs.UserToken = userToken;
                    sendSocketAsyncEventArgs.RemoteEndPoint = this._connectSocketAsyncEventArgs.RemoteEndPoint;
                    sendSocketAsyncEventArgs.SetBuffer(buffer, 0, buffer.Length);
                    sendSocketAsyncEventArgs.Completed += this.SendSocketAsyncEventArgsCompleted;

                    try
                    {
                        bool willRaiseEvent = this._clientSocket.SendAsync(sendSocketAsyncEventArgs);
                        if (!willRaiseEvent)
                        {
                            this.ProcessSend(sendSocketAsyncEventArgs);
                        }

                        return true;
                    }
                    catch
                    {
                        throw;
                    }
                }
                catch (Exception e)
                {
                    if (sendSocketAsyncEventArgs != null)
                    {
                        sendSocketAsyncEventArgs.Completed -= this.SendSocketAsyncEventArgsCompleted;
                        sendSocketAsyncEventArgs.UserToken = null;
                        sendSocketAsyncEventArgs.Dispose();
                        sendSocketAsyncEventArgs = null;
                    }

                    InternalLogger.Log(e);

                    this.RaiseEvent(
                                    this.ErrorOccurred,
                                    new AsyncSocketErrorEventArgs(
                                                                  AsyncSocketTcpClientConstants.TcpClientSendException,
                                                                  e,
                                                                  AsyncSocketErrorCodeEnum.TcpClientSendException));
                }
            }

            return false;
        }

        /// <summary>
        /// Releases all resources used by the current instance of the <see cref="AsyncSocketTcpClient" /> class.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases all resources used by the current instance of the <see cref="AsyncSocketTcpClient" /> class.
        /// </summary>
        public void Close()
        {
            this.Dispose();
        }

        /// <summary>
        /// Releases all resources used by the current instance of the <see cref="AsyncSocketTcpClient" /> class.
        /// protected virtual for non-sealed class; private for sealed class.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (this._disposed)
            {
                return;
            }

            this._disposed = true;

            if (disposing)
            {
                // dispose managed resources
                ////if (managedResource != null)
                ////{
                ////    managedResource.Dispose();
                ////    managedResource = null;
                ////}

                try
                {
                    this.CloseReceiveSocketAsyncEventArgs();
                    this.CloseConnectSocketAsyncEventArgs();
                    this.CloseClientSocket();
                }
                catch (Exception e)
                {
                    InternalLogger.Log(e);
                }
                finally
                {
                    this._receiveSocketAsyncEventArgs = null;
                    this._connectSocketAsyncEventArgs = null;
                    this._clientSocket = null;
                }
            }

            // free native resources
            ////if (nativeResource != IntPtr.Zero)
            ////{
            ////    Marshal.FreeHGlobal(nativeResource);
            ////    nativeResource = IntPtr.Zero;
            ////}
        }

        /// <summary>
        /// Method RaiseEvent.
        /// </summary>
        /// <param name="eventHandler">Instance of EventHandler.</param>
        /// <param name="eventArgs">Instance of AsyncSocketSessionEventArgs.</param>
        private void RaiseEvent(EventHandler<AsyncSocketSessionEventArgs> eventHandler, AsyncSocketSessionEventArgs eventArgs)
        {
            // Copy a reference to the delegate field now into a temporary field for thread safety
            EventHandler<AsyncSocketSessionEventArgs> temp = Interlocked.CompareExchange(ref eventHandler, null, null);

            if (temp != null)
            {
                temp(this, eventArgs);
            }
        }

        /// <summary>
        /// Method RaiseEvent.
        /// </summary>
        /// <param name="eventHandler">Instance of EventHandler.</param>
        /// <param name="eventArgs">Instance of AsyncSocketErrorEventArgs.</param>
        private void RaiseEvent(EventHandler<AsyncSocketErrorEventArgs> eventHandler, AsyncSocketErrorEventArgs eventArgs)
        {
            // Copy a reference to the delegate field now into a temporary field for thread safety
            EventHandler<AsyncSocketErrorEventArgs> temp = Interlocked.CompareExchange(ref eventHandler, null, null);

            if (temp != null)
            {
                temp(this, eventArgs);
            }
        }

        /// <summary>
        /// Method RaiseEvent.
        /// </summary>
        /// <param name="eventHandler">Instance of EventHandler.</param>
        private void RaiseEvent(EventHandler eventHandler)
        {
            // Copy a reference to the delegate field now into a temporary field for thread safety
            EventHandler temp = Interlocked.CompareExchange(ref eventHandler, null, null);

            if (temp != null)
            {
                temp(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Method ConnectSocketAsyncEventArgsCompleted.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="connectSocketAsyncEventArgs">Instance of SocketAsyncEventArgs.</param>
        private void ConnectSocketAsyncEventArgsCompleted(object sender, SocketAsyncEventArgs connectSocketAsyncEventArgs)
        {
            try
            {
                switch (connectSocketAsyncEventArgs.LastOperation)
                {
                    case SocketAsyncOperation.Connect:
                        this.ProcessConnect();
                        break;
                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                InternalLogger.Log(e);
                throw;
            }
        }

        /// <summary>
        /// Method ProcessConnect.
        /// </summary>
        private void ProcessConnect()
        {
            if (this._connectSocketAsyncEventArgs != null && this._connectSocketAsyncEventArgs.SocketError == SocketError.Success)
            {
                IPEndPoint sessionIPEndPoint = null;

                try
                {
                    sessionIPEndPoint = (this._connectSocketAsyncEventArgs.UserToken as Socket).LocalEndPoint as IPEndPoint;
                }
                catch
                {
                }

                this.RaiseEvent(this.Connected, new AsyncSocketSessionEventArgs(this.SessionId, sessionIPEndPoint));

                try
                {
                    this._receiveSocketAsyncEventArgs = new SocketAsyncEventArgs();
                    this._receiveSocketAsyncEventArgs.RemoteEndPoint = this._clientSocket.RemoteEndPoint;
                    this._receiveSocketAsyncEventArgs.SetBuffer(new byte[this.BufferSize], 0, this.BufferSize);
                    this._receiveSocketAsyncEventArgs.Completed += this.ReceiveSocketAsyncEventArgsCompleted;

                    try
                    {
                        bool willRaiseEvent = this._clientSocket.ReceiveAsync(this._receiveSocketAsyncEventArgs);
                        if (!willRaiseEvent)
                        {
                            this.ProcessReceive(this._receiveSocketAsyncEventArgs);
                        }
                    }
                    catch (Exception e)
                    {
                        InternalLogger.Log(e);

                        this.RaiseEvent(
                                        this.ErrorOccurred,
                                        new AsyncSocketErrorEventArgs(
                                                                      AsyncSocketTcpClientConstants.TcpClientReceiveException,
                                                                      e,
                                                                      AsyncSocketErrorCodeEnum.TcpClientReceiveException));
                    }
                }
                catch (Exception e)
                {
                    InternalLogger.Log(e);

                    this.RaiseEvent(
                                    this.ErrorOccurred,
                                    new AsyncSocketErrorEventArgs(
                                                                  AsyncSocketTcpClientConstants.TcpClientConnectException,
                                                                  e,
                                                                  AsyncSocketErrorCodeEnum.TcpClientConnectException));
                }
            }
        }

        /// <summary>
        /// Method ProcessReceive.
        /// </summary>
        /// <param name="receiveSocketAsyncEventArgs">Instance of SocketAsyncEventArgs.</param>
        private void ProcessReceive(SocketAsyncEventArgs receiveSocketAsyncEventArgs)
        {
            if (receiveSocketAsyncEventArgs != null && receiveSocketAsyncEventArgs.BytesTransferred > 0 && receiveSocketAsyncEventArgs.SocketError == SocketError.Success)
            {
                IPEndPoint sessionIPEndPoint = null;

                try
                {
                    sessionIPEndPoint = this._clientSocket.LocalEndPoint as IPEndPoint;
                }
                catch
                {
                }

                try
                {
                    Interlocked.Add(ref this._totalBytesReceived, receiveSocketAsyncEventArgs.BytesTransferred);

                    this.RaiseEvent(
                                    this.DataReceived,
                                    new AsyncSocketSessionEventArgs(
                                                                    this.SessionId,
                                                                    sessionIPEndPoint,
                                                                    receiveSocketAsyncEventArgs.Buffer,
                                                                    receiveSocketAsyncEventArgs.BytesTransferred,
                                                                    receiveSocketAsyncEventArgs.Offset,
                                                                    receiveSocketAsyncEventArgs.UserToken));

                    try
                    {
                        bool willRaiseEvent = this._clientSocket.ReceiveAsync(receiveSocketAsyncEventArgs);
                        if (!willRaiseEvent)
                        {
                            this.ProcessReceive(receiveSocketAsyncEventArgs);
                        }
                    }
                    catch
                    {
                        throw;
                    }
                }
                catch (Exception e)
                {
                    InternalLogger.Log(e);

                    this.RaiseEvent(
                                    this.ErrorOccurred,
                                    new AsyncSocketErrorEventArgs(
                                                                  AsyncSocketTcpClientConstants.TcpClientReceiveException,
                                                                  e,
                                                                  AsyncSocketErrorCodeEnum.TcpClientReceiveException));
                }
            }
            else
            {
                IPEndPoint sessionIPEndPoint = null;

                try
                {
                    sessionIPEndPoint = this._clientSocket.LocalEndPoint as IPEndPoint;
                }
                catch
                {
                }

                this.RaiseEvent(this.Disconnected, new AsyncSocketSessionEventArgs(this.SessionId, sessionIPEndPoint));
            }
        }

        /// <summary>
        /// Method ReceiveSocketAsyncEventArgsCompleted.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="receiveSocketAsyncEventArgs">Instance of SocketAsyncEventArgs.</param>
        private void ReceiveSocketAsyncEventArgsCompleted(object sender, SocketAsyncEventArgs receiveSocketAsyncEventArgs)
        {
            try
            {
                switch (receiveSocketAsyncEventArgs.LastOperation)
                {
                    case SocketAsyncOperation.Receive:
                        this.ProcessReceive(receiveSocketAsyncEventArgs);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                InternalLogger.Log(e);
                throw;
            }
        }

        /// <summary>
        /// Method SendSocketAsyncEventArgsCompleted.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="sendSocketAsyncEventArgs">Instance of SocketAsyncEventArgs.</param>
        private void SendSocketAsyncEventArgsCompleted(object sender, SocketAsyncEventArgs sendSocketAsyncEventArgs)
        {
            try
            {
                switch (sendSocketAsyncEventArgs.LastOperation)
                {
                    case SocketAsyncOperation.Send:
                        this.ProcessSend(sendSocketAsyncEventArgs);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                InternalLogger.Log(e);
                throw;
            }
        }

        /// <summary>
        /// Method ProcessSend.
        /// </summary>
        /// <param name="sendSocketAsyncEventArgs">Instance of SocketAsyncEventArgs.</param>
        private void ProcessSend(SocketAsyncEventArgs sendSocketAsyncEventArgs)
        {
            if (sendSocketAsyncEventArgs != null && sendSocketAsyncEventArgs.BytesTransferred > 0 && sendSocketAsyncEventArgs.SocketError == SocketError.Success)
            {
                if (this._receiveSocketAsyncEventArgs != null)
                {
                    this._receiveSocketAsyncEventArgs.UserToken = sendSocketAsyncEventArgs.UserToken;
                }

                IPEndPoint sessionIPEndPoint = null;

                try
                {
                    sessionIPEndPoint = this._clientSocket.LocalEndPoint as IPEndPoint;
                }
                catch
                {
                }

                try
                {
                    Interlocked.Add(ref this._totalBytesSent, sendSocketAsyncEventArgs.BytesTransferred);

                    this.RaiseEvent(
                                    this.DataSent,
                                    new AsyncSocketSessionEventArgs(
                                                                    this.SessionId,
                                                                    sessionIPEndPoint,
                                                                    sendSocketAsyncEventArgs.Buffer,
                                                                    sendSocketAsyncEventArgs.BytesTransferred,
                                                                    sendSocketAsyncEventArgs.Offset,
                                                                    sendSocketAsyncEventArgs.UserToken));
                }
                catch (Exception e)
                {
                    InternalLogger.Log(e);
                }
            }

            if (sendSocketAsyncEventArgs != null)
            {
                sendSocketAsyncEventArgs.Completed -= this.SendSocketAsyncEventArgsCompleted;
                sendSocketAsyncEventArgs.UserToken = null;
                sendSocketAsyncEventArgs.Dispose();
                sendSocketAsyncEventArgs = null;
            }
        }

        /// <summary>
        /// Method CloseClientSocket.
        /// </summary>
        private void CloseClientSocket()
        {
            if (this._clientSocket != null)
            {
                try
                {
                    this._clientSocket.Disconnect(false);
                }
                catch
                {
                }

                try
                {
                    this._clientSocket.Shutdown(SocketShutdown.Both);
                }
                catch
                {
                }
                finally
                {
                    this._clientSocket.Close();
                    this._clientSocket = null;
                }
            }
        }

        /// <summary>
        /// Method CloseConnectSocketAsyncEventArgs.
        /// </summary>
        private void CloseConnectSocketAsyncEventArgs()
        {
            if (this._connectSocketAsyncEventArgs != null)
            {
                this._connectSocketAsyncEventArgs.Completed -= this.ConnectSocketAsyncEventArgsCompleted;

                Socket sessionSocket = this._connectSocketAsyncEventArgs.UserToken as Socket;

                if (sessionSocket != null)
                {
                    try
                    {
                        sessionSocket.Disconnect(false);
                    }
                    catch
                    {
                    }

                    try
                    {
                        sessionSocket.Shutdown(SocketShutdown.Both);
                    }
                    catch
                    {
                    }
                    finally
                    {
                        sessionSocket.Close();
                        sessionSocket = null;
                    }
                }

                if (this._connectSocketAsyncEventArgs.AcceptSocket != null)
                {
                    try
                    {
                        this._connectSocketAsyncEventArgs.AcceptSocket.Disconnect(false);
                    }
                    catch
                    {
                    }

                    try
                    {
                        this._connectSocketAsyncEventArgs.AcceptSocket.Shutdown(SocketShutdown.Both);
                    }
                    catch
                    {
                    }
                    finally
                    {
                        this._connectSocketAsyncEventArgs.AcceptSocket.Close();
                        this._connectSocketAsyncEventArgs.AcceptSocket = null;
                    }
                }

                this._connectSocketAsyncEventArgs.UserToken = null;
                this._connectSocketAsyncEventArgs.Dispose();
                this._connectSocketAsyncEventArgs = null;
            }
        }

        /// <summary>
        /// Method CloseReceiveSocketAsyncEventArgs.
        /// </summary>
        private void CloseReceiveSocketAsyncEventArgs()
        {
            if (this._receiveSocketAsyncEventArgs != null)
            {
                this._receiveSocketAsyncEventArgs.Completed -= this.ReceiveSocketAsyncEventArgsCompleted;

                Socket sessionSocket = this._receiveSocketAsyncEventArgs.UserToken as Socket;

                if (sessionSocket != null)
                {
                    try
                    {
                        sessionSocket.Disconnect(false);
                    }
                    catch
                    {
                    }

                    try
                    {
                        sessionSocket.Shutdown(SocketShutdown.Both);
                    }
                    catch
                    {
                    }
                    finally
                    {
                        sessionSocket.Close();
                        sessionSocket = null;
                    }
                }

                if (this._receiveSocketAsyncEventArgs.AcceptSocket != null)
                {
                    try
                    {
                        this._receiveSocketAsyncEventArgs.AcceptSocket.Disconnect(false);
                    }
                    catch
                    {
                    }

                    try
                    {
                        this._receiveSocketAsyncEventArgs.AcceptSocket.Shutdown(SocketShutdown.Both);
                    }
                    catch
                    {
                    }
                    finally
                    {
                        this._receiveSocketAsyncEventArgs.AcceptSocket.Close();
                        this._receiveSocketAsyncEventArgs.AcceptSocket = null;
                    }
                }

                this._receiveSocketAsyncEventArgs.UserToken = null;
                this._receiveSocketAsyncEventArgs.Dispose();
                this._receiveSocketAsyncEventArgs = null;
            }
        }

        /// <summary>
        /// Method CheckDisposed.
        /// </summary>
        private void CheckDisposed()
        {
            if (this._disposed)
            {
                throw new ObjectDisposedException("DevLib.Net.Sockets.AsyncSocketTcpClient");
            }
        }
    }
}
