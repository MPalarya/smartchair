﻿using System;
using System.Management;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Common.Exceptions;

namespace GetDeviceIdentity
{
    class Program
    {
        static RegistryManager registryManager;
        static string connectionString = "HostName=smartchair-iothub.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=1LHpY6zkPYMuj1pa9rBYYAz9EK3a4rNyOIbW8VYn1sk=";

        static void Main(string[] args)
        {
            registryManager = RegistryManager.CreateFromConnectionString(connectionString);
            AddDeviceAsync().Wait();
        }

        private static async Task AddDeviceAsync()
        {
            string deviceId = OsSerialNumber();
            Device device;
            try
            {
                device = await registryManager.AddDeviceAsync(new Device(deviceId));
            }
            catch (DeviceAlreadyExistsException)
            {
                device = await registryManager.GetDeviceAsync(deviceId);
            }
            Console.WriteLine(device.Authentication.SymmetricKey.PrimaryKey);
        }

        //public static HashSet<string> NetworkIds()
        //{
        //    var result = new HashSet<string>();

        //    var networkProfiles = Windows.Networking.Connectivity.NetworkInformation.GetConnectionProfiles().ToList();

        //    foreach (var net in networkProfiles)
        //    {
        //        result.Add(net.NetworkAdapter.NetworkAdapterId.ToString());
        //    }

        //    return result;
        //}

        private static string OsSerialNumber()
        {
            ManagementObject o = new ManagementObject("Win32_OperatingSystem=@");
            string serial = (string)o["SerialNumber"];

            return serial;
        }
    }
}
