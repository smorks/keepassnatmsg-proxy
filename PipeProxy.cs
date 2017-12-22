using System;
using System.IO.Pipes;

namespace KeePassNatMsgProxy
{
    public class PipeProxy : ProxyBase
    {
        private NamedPipeClientStream _client;
        private const int ConnectTimeout = 5000;

        protected override bool IsClientConnected => _client.IsConnected;

        protected override int ClientRead(byte[] buffer, int offset, int length)
        {
            return _client.Read(buffer, offset, length);
        }

        protected override void ClientWrite(byte[] data)
        {
            _client.Write(data, 0, data.Length);
        }

        protected override void Close()
        {
            _client.Close();
        }

        protected override void Connect()
        {
            _client = new NamedPipeClientStream(".", $"keepassxc\\{Environment.UserName}\\{ProxyName}", PipeDirection.InOut, PipeOptions.Asynchronous);
            _client.Connect(ConnectTimeout);
        }
    }
}
