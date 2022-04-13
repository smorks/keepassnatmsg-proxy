using System;
using System.Threading;

namespace KeePassNatMsgProxy
{
    public abstract class ProxyBase
    {
        protected const string ProxyName = "kpxc_server";

        private const int BufferSize = 1024 * 1024;

        private Thread _readThread;
        private Thread _writeThread;
        private bool _active;
        private readonly object _writeLock;
        private readonly ManualResetEventSlim _readEnd;
        private readonly ManualResetEventSlim _writeEnd;

        /// <summary>
        /// Initialize a new instance of ProxyBase.
        /// </summary>
        protected ProxyBase()
        {
            _writeLock = new object();
            _readEnd = new ManualResetEventSlim();
            _writeEnd = new ManualResetEventSlim();
        }

        public LogWriter Log { get; set; }

        // public bool EnableLog { get; set; }

        /// <summary>
        /// Create a thread and run the proxy.
        /// </summary>
        /// <exception cref="KeePassNatMsgProxy.ProxyException">Thrown when any kind of error occurs while create and run the proxy.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<optimization>")]
        public void Run()
        {
            _active = true;

            // Connect proxy to target process.
            try
            {
                Connect();
            }
            catch (Exception ex)
            {
                Log?.Write($"Connect Error: {ex}");
                return;
            }

            // Create the reader thread.
            try
            {
                _readThread = new Thread(ClientReadThread)
                {
                    Name = nameof(ClientReadThread)
                };
                _readThread.Start();
            }
            catch (ArgumentNullException anex)
            {
                throw new ProxyException("Argument null error while creating Reader thread.", anex);
            }
            catch (ThreadStateException tsex)
            {
                throw new ProxyException("Thread state error while creating Reader thread.", tsex);
            }
            catch (OutOfMemoryException oomex)
            {
                throw new ProxyException("Out of memory error while creating Reader thread.", oomex);
            }
            catch (Exception ex)
            {
                throw new ProxyException("Error while creating Reader thread.", ex);
            }

            // Create the reader thread.
            try
            {
                _writeThread = new Thread(ClientWriteThread)
                {
                    Name = nameof(ClientWriteThread)
                };
                _writeThread.Start();
            }
            catch (ArgumentNullException anex)
            {
                throw new ProxyException("Argument null error while creating Writer thread.", anex);
            }
            catch (ThreadStateException tsex)
            {
                throw new ProxyException("Thread state error while creating Writer thread.", tsex);
            }
            catch (OutOfMemoryException oomex)
            {
                throw new ProxyException("Out of memory error while creating Writer thread.", oomex);
            }
            catch (Exception ex)
            {
                throw new ProxyException("Error while creating Writer thread.", ex);
            }

            Log?.Write("Read/Write threads started. Waiting for reset.");

            WaitHandle.WaitAny(new[] {_readEnd.WaitHandle, _writeEnd.WaitHandle});

            Log?.Write("Reset Received! Closing connection.");

            Close();

            // abort the threads if necessary
            if (_readEnd.IsSet && !_writeEnd.IsSet)
            {
                _writeThread.Abort();
            }
            else if (_writeEnd.IsSet && !_readEnd.IsSet)
            {
                _readThread.Abort();
            }

            // Wait until Writer ends.
            try
            {
                _writeThread.Join();
            }
            catch (ThreadStateException tsex)
            {
                throw new ProxyException("Thread state error while Writer thread ends.", tsex);
            }
            catch (ThreadInterruptedException tiex)
            {
                throw new ProxyException("Writer thread received interrupion call.", tiex);
            }
            catch (Exception ex)
            {
                throw new ProxyException("Error while Writer thread ends.", ex);
            }

            Log?.Write("WriteThread joined.");

            // Wait until Reader ends.
            try
            {
                _readThread.Join();
            }
            catch (ThreadStateException tsex)
            {
                throw new ProxyException("Thread state error while Reader thread ends.", tsex);
            }
            catch (ThreadInterruptedException tiex)
            {
                throw new ProxyException("Reader thread received interrupion call.", tiex);
            }
            catch (Exception ex)
            {
                throw new ProxyException("Error while Reader thread ends.", ex);
            }

            Log?.Write("ReadThread joined. Exiting.");
        }

        protected abstract void Connect();
        protected abstract void Close();
        protected abstract bool IsClientConnected { get; }
        protected abstract void ClientWrite(byte[] data);
        protected abstract int ClientRead(byte[] buffer, int offset, int length);

        private void ClientWriteThread()
        {
            do
            {
                try
                {
                    var data = ConsoleRead();
                    if (data != null)
                    {
                        ClientWrite(data);
                    }
                    else
                    {
                        _active = false;
                    }
                }
                catch (ThreadAbortException)
                {
                    _active = false;
                    break;
                }
                catch (Exception ex)
                {
                    Log?.Write($"ClientWriteThread Error: {ex}");
                    _active = false;
                    break;
                }
            } while (_active && IsClientConnected);

            _writeEnd.Set();

            Log?.Write("ClientWriteThread exit.");
        }

        private void ClientReadThread()
        {
            while (_active && IsClientConnected)
            {
                try
                {
                    var buffer = new byte[BufferSize];
                    var bytes = ClientRead(buffer, 0, buffer.Length);
                    if (bytes == 0)
                    {
                        // done?
                        _active = false;
                        break;
                    }

                    var data = new byte[bytes];
                    Array.Copy(buffer, data, bytes);
                    ConsoleWrite(data);
                }
                catch (ThreadAbortException)
                {
                    _active = false;
                    break;
                }
                catch (Exception ex)
                {
                    Log?.Write($"ClientReadThread Error: {ex}");
                    _active = false;
                    break;
                }
            }

            _readEnd.Set();

            Log?.Write("ClientReadThread exit.");
        }

        private static byte[] ConsoleRead()
        {
            var stdin = Console.OpenStandardInput();
            var readBytes = new byte[4];
            var read = stdin.Read(readBytes, 0, readBytes.Length);
            if (read < readBytes.Length) return null;
            var length = BitConverter.ToInt32(readBytes, 0);
            var data = new byte[length];
            read = stdin.Read(data, 0, data.Length);
            if (read > 0)
            {
                if (read < data.Length)
                {
                    var newData = new byte[read];
                    Array.Copy(data, newData, newData.Length);
                    return newData;
                }
                return data;
            }
            return null;
        }

        private void ConsoleWrite(byte[] data)
        {
            lock (_writeLock)
            {
                try
                {
                    var stdout = Console.OpenStandardOutput();
                    var bytes = BitConverter.GetBytes(data.Length);
                    stdout.Write(bytes, 0, bytes.Length);
                    stdout.Write(data, 0, data.Length);
                    stdout.Flush();
                }
                catch (Exception ex)
                {
                    throw new ProxyException("Exception while writing.", ex);
                }
            }
        }
    }
}
