using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;

namespace SCHOTT.Core.Communication.Serial
{
    /// <summary>
    /// A worker class to find ComPorts on the system. 
    /// If there is advanced information available to the system, it will be in the properties of this object.
    /// </summary>
    public class ComPortInfo
    {
        private static class ProcessConnection
        {
            public static ConnectionOptions ProcessConnectionOptions()
            {
                var options = new ConnectionOptions
                {
                    Impersonation = ImpersonationLevel.Impersonate,
                    Authentication = AuthenticationLevel.Default,
                    EnablePrivileges = true
                };
                return options;
            }

            public static ManagementScope ConnectionScope(string machineName, ConnectionOptions options, string path)
            {
                var connectScope = new ManagementScope
                {
                    Path = new ManagementPath(@"\\" + machineName + path),
                    Options = options
                };
                connectScope.Connect();
                return connectScope;
            }
        }

        /// <summary>
        /// Name of the system port, format of "COM#"
        /// </summary>
        public string Port { get; set; } = "";

        /// <summary>
        /// Manufacturer of the COM device.
        /// </summary>
        public string Manufacturer { get; set; } = "";

        /// <summary>
        /// Caption of the COM device. This is the string that will be displayed in device manager.
        /// </summary>
        public string Caption { get; set; } = "";

        /// <summary>
        /// System Device ID.
        /// </summary>
        public string DeviceId { get; set; } = "";

        /// <summary>
        /// Description string of the ComPort
        /// </summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// Name of the ComPort.
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Manufacturer VID
        /// </summary>
        public string Vid { get; set; } = "";

        /// <summary>
        /// Product PID
        /// </summary>
        public string Pid { get; set; } = "";

        /// <summary>
        /// Serial number if provided.
        /// </summary>
        public string Serial { get; set; } = "";
        
        /// <summary>
        /// Gets a list of COMPortInfo objects. This contains a port name and description of every com port in the system, including ones in use.
        /// </summary>
        /// <returns></returns>
        public static List<ComPortInfo> GetDescriptions()
        {
            var comPortInfoList = new List<ComPortInfo>();

            var options = ProcessConnection.ProcessConnectionOptions();
            var connectionScope = ProcessConnection.ConnectionScope(Environment.MachineName, options, @"\root\CIMV2");

            var objectQuery = new ObjectQuery("SELECT * FROM Win32_PnPEntity WHERE ClassGuid=\"{4d36e978-e325-11ce-bfc1-08002be10318}\"");
            var comPortSearcher = new ManagementObjectSearcher(connectionScope, objectQuery);

            using (comPortSearcher)
            {
                foreach (var obj in comPortSearcher.Get())
                {
                    if (obj["Caption"]?.ToString().Contains("COM") != true)
                        continue;

                    var comPortInfo = new ComPortInfo
                    {
                        Manufacturer = obj["Manufacturer"]?.ToString(),
                        Caption = obj["Caption"]?.ToString(),
                        DeviceId = obj["DeviceID"]?.ToString(),
                        Description = obj["Description"]?.ToString(),
                        Name = obj["Name"]?.ToString()
                    };
                        
                    comPortInfo.Caption = obj["Caption"]?.ToString();

                    var tempString = comPortInfo.Caption.Substring(comPortInfo.Caption.IndexOf("(", StringComparison.Ordinal) + 1);
                    tempString = tempString.Substring(0, tempString.IndexOf(")", StringComparison.Ordinal));
                    comPortInfo.Port = tempString;

                    if (comPortInfo.DeviceId?.Contains("VID") == true)
                        comPortInfo.Vid = comPortInfo.DeviceId.Substring(comPortInfo.DeviceId.IndexOf("VID", StringComparison.Ordinal) + 4, 4);

                    if (comPortInfo.DeviceId?.Contains("PID") == true)
                        comPortInfo.Pid = comPortInfo.DeviceId.Substring(comPortInfo.DeviceId.IndexOf("PID", StringComparison.Ordinal) + 4, 4);

                    if (comPortInfo.Manufacturer?.Contains("FTDI") == true)
                    {
                        tempString = comPortInfo.DeviceId?.Substring(comPortInfo.DeviceId.IndexOf("PID", StringComparison.Ordinal) + 9);
                        if (tempString != null)
                        {
                            var tempInt = tempString.IndexOf("\\", StringComparison.Ordinal);
                            if (tempInt > 0)
                            {
                                tempString = tempString.Substring(0, tempInt);
                                comPortInfo.Serial = tempString;
                            }
                        }
                    }

                    comPortInfoList.Add(comPortInfo);
                }
            }

            return comPortInfoList.OrderBy(x => x.Description).ToList();
        }

    }

}
