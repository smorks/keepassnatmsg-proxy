using System;

namespace KeePassNatMsgProxy
{
    public class Proxy
    {
        private const string EnableLog = "/log";

        private static LogWriter _log;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<optimization>")]
        public static int Main(string[] args)
        {
            ProxyBase proxy = null;
            var enableLog = args.Length == 1 && EnableLog.Equals(args[0], StringComparison.OrdinalIgnoreCase);

            if (enableLog)
            {
                _log = new LogWriter();
            }

            // check if we're running under Mono
            var t = Type.GetType("Mono.Runtime");

            try
            {
                if (t == null)
                {
                    // not Mono, assume Windows
                    proxy = new PipeProxy();
                }
                else
                {
                    if (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX)
                    {
                        proxy = new UnixSocketProxy();
                    }
                }

                if (proxy != null)
                {
                    proxy.Log = _log;
                    proxy.Run();
                }
                else
                {
                    _log?.Write($"Unable to create proxy. Platform: {Environment.OSVersion.Platform}");
                }
            }
            catch (Exception ex)
            {
                _log?.Write($"{nameof(KeePassNatMsgProxy)} Error: {ex}");
            }

            _log?.Close();

            // Set exit code of application
            return 0;
        }
    }
}
