using System;
using System.IO;
using System.Text;

namespace KeePassNatMsgProxy
{
    public class LogWriter
    {
        private readonly StreamWriter _sw;

        public LogWriter()
        {
            _sw = new StreamWriter(File.OpenWrite("proxy.log"), new UTF8Encoding(false));
        }

        public void Write(string msg)
        {
            _sw.WriteLine("{0}: {1}", DateTime.Now, msg);
        }

        public void Close() => _sw.Close();
    }
}
