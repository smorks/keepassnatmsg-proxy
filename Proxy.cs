using System;
using System.IO.Pipes;
using System.Threading;

namespace KeePassHttpProxy
{
    class Proxy
    {
        static void Main(string[] args)
        {
            var proxy = new Proxy();
            proxy.Run();
        }

        private const string PipeName = "KeePassHttp";
        private const int BufferSize = 1024*1024;
        private NamedPipeClientStream _client;
        private Thread _clientThread;
        private bool _active;
        private readonly object _writeLock;

        private Proxy()
        {
            _writeLock = new object();
        }

        private void Run()
        {
            _active = true;

            try
            {
                _client = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                _client.Connect(5000);
            }
            catch (Exception)
            {
                return;
            }

            _clientThread = new Thread(ClientReadThread)
            {
                Name = "ClientReadThread"
            };
            _clientThread.Start();

            do
            {
                var data = ConsoleRead();
                if (data != null)
                {
                    _client.Write(data, 0, data.Length);
                }
                else
                {
                    _active = false;
                }
            } while (_active);

            _client.Close();
            _clientThread.Join();
        }

        private void ClientReadThread()
        {
            while (_active)
            {
                var data = new byte[BufferSize];
                var ar = _client.BeginRead(data, 0, data.Length, ClientRead, data);
                ar.AsyncWaitHandle.WaitOne();
            }
        }

        private void ClientRead(IAsyncResult result)
        {
            if (_active)
            {
                var bytes = _client.EndRead(result);
                if (bytes > 0)
                {
                    var buffer = (byte[]) result.AsyncState;
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
