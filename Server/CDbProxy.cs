﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;
using Newtonsoft.Json;

namespace Server
{   
    // Table 0: key = deviceId, value = device datapoint log
    // Table 1: key = deviceId, value = init dataset for device
    // Table 2: key = deviceId, value = Client class
    // Table 3: key = clientId, value = deviceId
    public class CDbProxy : IDbProxy
    {
        #region Fields

        private static CDbProxy instance;
        private ConnectionMultiplexer connection;
        private IDatabase redisLogs, redisInit, redicClients, redisDevicesByClient;

        #endregion

        #region Constructors
        private CDbProxy()
        {
            connection = ConnectionMultiplexer.Connect("smartchair.redis.cache.windows.net:6380,password=EcwNyqsaIKcM8JcnWbkP8FHy/xs/YHf6omQ2UaHvnlw=,ssl=True,abortConnect=False");
            redisLogs = connection.GetDatabase(0);
            redisInit = connection.GetDatabase(1);
            redicClients = connection.GetDatabase(2);
            redisDevicesByClient = connection.GetDatabase(3);
        }
        #endregion

        #region Properties

        public static CDbProxy Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new CDbProxy();
                }
                return instance;
            }
        }

        #endregion

        #region Methods
        /* ----------------------------------------*
         * LOGS Table                               *
         * ----------------------------------------*/
        // Appends a datapoint encoded in JSON to the deviceId key in Logs table
        // If key does not exist it is added with the datapoint
        public void updateLog(Datapoint datapoint)
        {
            object[] data = { datapoint.datetime.ToString(), datapoint.pressure };
            string jsondata = JsonConvert.SerializeObject(data);
            redisLogs.StringAppend(datapoint.deviceId, jsondata + ",");
            Console.WriteLine("Appended key = " + datapoint.deviceId + "; value = " + jsondata + "," + " to redisLogs");
        }

        // Gets Log value for deviceId key from Logs table in encoded JSON array
        // Returns string [[string datetime, int pressure[numOfSensors]], [string datetime, int pressure[7]], ... ]
        public string getLog(string deviceId)
        {
            string value = redisLogs.StringGet(deviceId);
            Console.WriteLine("Got key = " + deviceId + " from redisLogs");
            return "[" + value.TrimEnd(',') + "]";
        }

        // Deletes key from Logs database
        public void removeLog(string deviceId)
        {
            redisLogs.KeyDelete(deviceId);
            Console.WriteLine("Removed key = " + deviceId + " from redisLogs");
        }


        /* ----------------------------------------*
         * INIT Table                               *
         * ----------------------------------------*/
        // Sets init datapoint value for deviceId key in Init table
        public void setInit(Datapoint datapoint)
        {
            string jsondata = JsonConvert.SerializeObject(datapoint.pressure);
            redisInit.StringSet(datapoint.deviceId, jsondata);
            Console.WriteLine("Set key = " + datapoint.deviceId + "; value = " + jsondata.ToString() + " to redisInit");
        }

        // Gets init data value for deviceId key
        public int[] getInit(string deviceId)
        {
            string value = redisInit.StringGet(deviceId);
            Console.WriteLine("Got key = " + deviceId + " from redisInit");
            if (value == null)
                return new int[0];

            return JsonConvert.DeserializeObject<int[]>(value);
        }

        // Deletes deviceId key from Init table
        public void removeInit(string deviceId)
        {
            redisInit.KeyDelete(deviceId);
            Console.WriteLine("Removed key = " + deviceId + " from redisInit");
        }


        /* ----------------------------------------*
         * CLIENTS Table                            *
         * ----------------------------------------*/
        // Sets clientId value for deviceId key in Client table
        public void setClient(ClientProperties client)
        {
            string jsonClient = JsonConvert.SerializeObject(client);
            redicClients.StringSet(client.deviceId, jsonClient);
            setDevice(client.clientId, client.deviceId);
            Console.WriteLine("Set key = " + client.deviceId + "; value = " + client.clientId + " to redisClients");
        }
        public void setClient(string deviceId, string clientId)
        {
            ClientProperties client = new ClientProperties(clientId, deviceId);
            setClient(client);
        }
        public void setClient(string deviceId, string clientId, bool sendRealTime)
        {
            ClientProperties client = new ClientProperties(clientId, deviceId, sendRealTime);
            setClient(client);
        }

        // Gets clientId value for deviceId key from Clients table
        public ClientProperties getClientByDevice(string deviceId)
        {
            string value = redicClients.StringGet(deviceId);
            Console.WriteLine("Got key = " + deviceId + " from redisClients");
            if (value != null)
                return JsonConvert.DeserializeObject<ClientProperties>(value);
            else
                return null;
        }

        // Deletes deviceId key from Clients table
        public void removeClient(string deviceId)
        {
            ClientProperties client = getClientByDevice(deviceId);
            redicClients.KeyDelete(deviceId);
            if (client != null)
                removeDevice(client.clientId);
            Console.WriteLine("Removed key = " + deviceId + " from redisClients");
        }


        /* ----------------------------------------*
         * DEVICES Table                            *
         * ----------------------------------------*/
        // Sets deviceId value for clientId key in Devices table
        public void setDevice(string clientId, string deviceId)
        {
            redisDevicesByClient.StringSet(clientId, deviceId);
            Console.WriteLine("Set key = " + clientId + "; value = " + deviceId + " to redisDevicesByClient");
        }

        // Gets deviceId data value for clientId key
        public string getDeviceByClient(string clientId)
        {
            string value = redisDevicesByClient.StringGet(clientId);
            Console.WriteLine("Got key = " + clientId.ToString() + " from redisDevicesByClient");
            return value;
        }

        // Deletes clientId key from Devices table
        public void removeDevice(string clientId)
        {
            redisDevicesByClient.KeyDelete(clientId);
            Console.WriteLine("Removed key = " + clientId + " from redisDevicesByClient");
        }
        #endregion
    }
}
