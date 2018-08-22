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

        protected ProxyBase()
        {
            _writeLock = new object();
        }

        public void Run()
        {
            _active = true;

            try
            {
                Connect();
            }
            catch (Exception)
            {
                return;
            }

            _readThread = new Thread(ClientReadThread)
            {
                Name = nameof(ClientReadThread)
            };
            _readThread.Start();

            _writeThread = new Thread(ClientWriteThread)
            {
                Name = nameof(ClientWriteThread)
            };
            _writeThread.Start();

            _writeThread.Join();

            Close();
            _readThread.Join();
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
                var data = ConsoleRead();
                if (data != null)
                {
                    ClientWrite(data);
                }
                else
                {
                    _active = false;
                }
            } while (_active && IsClientConnected);
        }

        private void ClientReadThread()
        {
            while (_active && IsClientConnected)
            {
                var buffer = new byte[BufferSize];
                var bytes = ClientRead(buffer, 0, buffer.Length);
                if (bytes > 0)
                {
                    var data = new byte[bytes];
                    Array.Copy(buffer, data, bytes);
                    ConsoleWrite(data);
                }
            }
        }

        private byte[] ConsoleRead()
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
                var stdout = Console.OpenStandardOutput();
                var bytes = BitConverter.GetBytes(data.Length);
                stdout.Write(bytes, 0, bytes.Length);
                stdout.Write(data, 0, data.Length);
                stdout.Flush();
            }
        }
    }
}
