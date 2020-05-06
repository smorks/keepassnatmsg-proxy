using System;
using System.Runtime.Serialization;

namespace KeePassNatMsgProxy
{
    /// <summary>
    /// This a generic exception class used by ProxyBase and all descendants.
    /// </summary>
    [Serializable]
    public class ProxyException : Exception
    {
        /// <summary>
        /// Initialize a new instance of ProxyException.
        /// </summary>
        public ProxyException() : this("")
        {
        }


        /// <summary>
        /// Initialize a new instance of ProxyException.
        /// </summary>
        /// <param name="message">The message explaining the exception.</param>
        public ProxyException(string message) : this(message, null)
        {
        }


        /// <summary>
        /// Initialize a new instance of ProxyException.
        /// </summary>
        /// <param name="message">The message explaining the exception.</param>
        /// <param name="innerException">Reference to the original exception, which is covered by this new instance.</param>
        public ProxyException(string message, Exception innerException) : base(message, innerException)
        {
        }


        /// <summary>
        /// Method implementation forced by inheritance.
        /// </summary>
        protected ProxyException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
