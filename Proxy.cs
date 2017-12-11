using System;

namespace KeePassHttpProxy
{
    class Proxy
    {
        static void Main(string[] args)
        {
            // check if we're running under Mono
            var t = Type.GetType("Mono.Runtime");

            if (t == null)
            {
                // not Mono, assume Windows
                var proxy = new PipeProxy();
                proxy.Run();
            }
            else
            {
                if (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX)
                {
                    var proxy = new UnixSocketProxy();
                    proxy.Run();
                }
            }
        }
    }
}
