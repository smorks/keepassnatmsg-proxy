using System;
using System.Net;
using System.Net.Sockets;

namespace KeePassHttpProxy
{
    class Proxy
    {
        static void Main(string[] args)
        {
            var proxy = new Proxy();
            proxy.Run();
        }

        private const int DefaultPort = 19700;
        private System.Text.UTF8Encoding _utf8;
        private UdpClient _udp;
        private System.IO.StreamWriter _log;

        private Proxy()
        {
            _utf8 = new System.Text.UTF8Encoding(false);
        }

        private void Run()
        {
            _log = new System.IO.StreamWriter(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(GetType().Assembly.Location), "proxy.log"), true)
            {
                AutoFlush = true
            };
            var lep = new IPEndPoint(IPAddress.Loopback, 0);
            _udp = new UdpClient(lep);
            _udp.Client.ReceiveTimeout = 5000;
            _udp.Client.SendTimeout = 5000;

            var ep = new IPEndPoint(IPAddress.Loopback, DefaultPort);
            _udp.Connect(ep);

            bool done = false;

            do
            {
                try
                {
                    var data = Read();
                    if (data != null)
                    {
                        var msgIn = _utf8.GetString(data);
                        _log.WriteLine("Got Message:");
                        _log.WriteLine(msgIn);
                        _udp.Send(data, data.Length);
                        var rep = new IPEndPoint(IPAddress.Any, 0);
                        var rdata = _udp.Receive(ref rep);
                        var msgOut = _utf8.GetString(rdata);
                        _log.WriteLine("Got Response:");
                        _log.WriteLine(msgOut);
                        Write(rdata);
                    }
                    else
                    {
                        done = true;
                    }
                }
                catch (Exception ex)
                {
                    _log.WriteLine(ex.ToString());
                }
            } while (!done);
            _udp.Close();
            _log.WriteLine("All done!");
            _log.Close();
        }

        private byte[] Read()
        {
            var stdin = Console.OpenStandardInput();
            var readBytes = new byte[4];
            stdin.Read(readBytes, 0, readBytes.Length);
            var length = BitConverter.ToInt32(readBytes, 0);
            var data = new byte[length];
            var read = stdin.Read(data, 0, data.Length);
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

        private void Write(byte[] data)
        {
            var stdout = Console.OpenStandardOutput();
            var bytes = BitConverter.GetBytes(data.Length);
            stdout.Write(bytes, 0, bytes.Length);
            stdout.Write(data, 0, data.Length);
            stdout.Flush();
        }
    }
}
