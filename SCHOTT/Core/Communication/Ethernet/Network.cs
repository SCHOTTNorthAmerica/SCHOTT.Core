using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;

namespace SCHOTT.Core.Communication.Ethernet
{
    /// <summary>
    /// A utility class containing functions for Ethernet Networks.
    /// </summary>
    public static class Network
    {
        /// <summary>
        /// Function to see if a host is reachable using the standard ping function.
        /// </summary>
        /// <param name="host">This can be a DNS name, or an IP address.</param>
        /// <param name="timeoutMilliseconds">Timeout for the ping function in milliseconds.</param>
        /// <returns>True = Ping Successful, False = Ping Failed</returns>
        public static bool PingHost(string host, int timeoutMilliseconds = 500)
        {
            var buffer = new byte[32];
            var ping = new Ping();
            var ipAddressList = Dns.GetHostAddresses(host);
            var pingReply = ping.Send(ipAddressList[0], timeoutMilliseconds, buffer, new PingOptions(128, true));
            return pingReply != null && ipAddressList.Length > 0 && pingReply.Status == IPStatus.Success;
        }

        /// <summary>
        /// Function to turn an IP string into a standard string format.
        /// </summary>
        /// <param name="input">The IP string to clean.</param>
        /// <returns>The cleaned IP string.</returns>
        public static string CleanIp(string input)
        {
            return IPAddress.Parse(Regex.Replace(input.Replace(':', '.'), "0*([0-9]+)", "${1}")).ToString();
        }

    }
}
