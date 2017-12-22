using KeePassHttpProxy.Mono.Unix;
using System;
using System.Net.Sockets;

namespace KeePassNatMsgProxy
{
    public class UnixSocketProxy : ProxyBase
    {
        private Socket _client;

        protected override bool IsClientConnected => _client.Connected;

        protected override int ClientRead(byte[] buffer, int offset, int length)
        {
            return _client.Receive(buffer, offset, length, SocketFlags.None);
        }

        protected override void ClientWrite(byte[] data)
        {
            _client.Send(data);
        }

        protected override void Close()
        {
            _client.Disconnect(true);
            _client.Close();
        }

        protected override void Connect()
        {
            var path = $"/tmp/{ProxyName}";
            var xdg = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");
            if (!string.IsNullOrEmpty(xdg))
            {
                path = System.IO.Path.Combine(xdg, ProxyName);
            }
            var rep = new UnixEndPoint(path);
            _client = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
            _client.Connect(rep);
        }
    }
}
