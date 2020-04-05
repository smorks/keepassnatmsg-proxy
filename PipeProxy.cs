using System;
using System.IO.Pipes;

namespace KeePassNatMsgProxy
{
    public class PipeProxy : ProxyBase
    {
        private const int ConnectTimeout = 5000;

        private NamedPipeClientStream _client;

        protected override bool IsClientConnected => _client.IsConnected;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<optimization>")]
        protected override int ClientRead(byte[] buffer, int offset, int length)
        {
            try
            {
                return _client.Read(buffer, offset, length);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<optimization>")]
        protected override void ClientWrite(byte[] data)
        {
            try
            {
                _client.Write(data, 0, data.Length);
            }
            catch (Exception)
            {
                return;
            }
        }

        protected override void Close()
        {
            _client.Close();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<optimization>")]
        protected override void Connect()
        {
            try
            {
                _client = new NamedPipeClientStream(".", $"keepassxc\\{Environment.UserName}\\{ProxyName}", PipeDirection.InOut, PipeOptions.Asynchronous);
                _client.Connect(ConnectTimeout);
            }
            catch (Exception)
            {
                return;
            }
        }
    }
}
