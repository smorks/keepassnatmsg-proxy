using System;
using System.Windows.Forms;

namespace KeePassNatMsgProxy
{
    public class Proxy
    {
        private const string EnableLog = "/log";

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<optimization>")]
        public static int Main(string[] args)
        {
            var enableLog = args.Length == 1 && EnableLog.Equals(args[0], StringComparison.OrdinalIgnoreCase);

            // check if we're running under Mono
            var t = Type.GetType("Mono.Runtime");

            try
            {
                if (t == null)
                {
                    // not Mono, assume Windows
                    var proxy = new PipeProxy
                    {
                        EnableLog = enableLog,
                    };
                    proxy.Run();

                }
                else
                {
                    if (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX)
                    {
                        var proxy = new UnixSocketProxy
                        {
                            EnableLog = enableLog,
                        };
                        proxy.Run();

                    }
                }
            }
            catch (Exception ex)
            {
                DialogResult result = MessageBox.Show(ex.Message, nameof(KeePassNatMsgProxy) + " Error");
            }

            // Set exit code of application
            return 0;
        }
    }
}
