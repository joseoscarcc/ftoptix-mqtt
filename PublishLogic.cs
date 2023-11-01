#region StandardUsing
using System;
using FTOptix.CoreBase;
using FTOptix.HMIProject;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.NetLogic;
using FTOptix.UI;
using FTOptix.OPCUAServer;

using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;


#endregion
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using Newtonsoft.Json;
using System.Globalization;
using System.Text;
using System.IO;
using FTOptix.SQLiteStore;
using FTOptix.Store;
using FTOptix.ODBCStore;
using FTOptix.DataLogger;
using FTOptix.Recipe;
using Newtonsoft.Json.Linq;
using System.Linq;

public class PublicLogic : BaseNetLogic
{
    private PeriodicTask MiTask;
    public override void Start()
    {
        // Configure MQTT credentials
        var broker = LogicObject.GetVariable("Broker");
        string brokerIpAddress = broker.Value;
        int port = LogicObject.GetVariable("Port").Value;
        string clientID = LogicObject.GetVariable("ClientID").Value;
        string username = LogicObject.GetVariable("Username").Value;
        string password = LogicObject.GetVariable("Password").Value;

        publishClient = new MqttClient(brokerIpAddress, port, true, null, null, MqttSslProtocols.TLSv1_2);
        //publishClient = new MqttClient(brokerIpAddress);

        // Connect to the broker
        publishClient.Connect(clientID, username, password, false, 60);
        // Assign a callback to be executed when a message is published to the broker
        publishClient.MqttMsgPublished += PublishClientMqttMsgPublished;


        MiTask = new PeriodicTask(PublishMessage, 250, LogicObject);
        MiTask.Start();

    }

    public override void Stop()
    {
        publishClient.Disconnect();
        publishClient.MqttMsgPublished -= PublishClientMqttMsgPublished;
    }

    private void PublishClientMqttMsgPublished(object sender, MqttMsgPublishedEventArgs e)
    {
        Log.Info("Message " + e.MessageId + " - published = " + e.IsPublished);
    }

    public void PublishMessage()
    {
        var dataLoggerStore = LogicObject.GetVariable("DataLogger").Value;
        var dataInfo = InformationModel.Get<DataLogger>(dataLoggerStore);

        Dictionary<string, Dictionary<string, string>> dictOfDicts = new Dictionary<string, Dictionary<string, string>>();
        Dictionary<string, string> currentDict = null;
        string lastAssetName = null;

        foreach (var variableToLog in dataInfo.VariablesToLog.ToList())
        {
            string[] parts = variableToLog.BrowseName.Split('_');
            if (parts.Length < 3)
            {
                if (lastAssetName != parts[0])
                {
                    // New asset name, create a new dictionary
                    currentDict = new Dictionary<string, string>();
                    dictOfDicts[parts[0]] = currentDict;
                    lastAssetName = parts[0];
                }
                // Add the variable to the current dictionary
                currentDict[parts[1]] = variableToLog.Value;
            } 
            else if (parts.Length > 2)
            {
                if (parts[1] == "Asset" || parts[1] == "High")
                {
                    if (lastAssetName == parts[0])
                    {
                        currentDict[parts[1] + "_" + parts[2]] = variableToLog.Value;
                    }
                }
            }
            // Add timestamp to the current dictionary
            currentDict["Timestamp"] = DateTime.Now.ToString();
        }

        string json = JsonConvert.SerializeObject(dictOfDicts, Formatting.Indented);
        Log.Info(json);
        var topic = LogicObject.GetVariable("Topic");

        ushort msgId = publishClient.Publish(topic.Value, // topic
            System.Text.Encoding.UTF8.GetBytes(json), // message body
            MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, // QoS level
            false); // retained

    }
    


    private MqttClient publishClient;


}
