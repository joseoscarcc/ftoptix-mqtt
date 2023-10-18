#region StandardUsing
using System;
using FTOptix.CoreBase;
using FTOptix.HMIProject;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.NetLogic;
using FTOptix.UI;
using FTOptix.OPCUAServer;
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

public class PublicLogic : BaseNetLogic
{
    private PeriodicTask MiTask;
    public override void Start()
    {
         // Configure MQTT credentials
        var broker = LogicObject.GetVariable("Broker");
        string brokerIpAddress = broker.Value;
        var port = LogicObject.GetVariable("Port");
        var clientID = LogicObject.GetVariable("ClientID");
        var username = LogicObject.GetVariable("Username");
        var password = LogicObject.GetVariable("Broker");

        //publishClient = new MqttClient(brokerIpAddress, 1883, true, null, null, MqttSslProtocols.TLSv1_2);
        publishClient = new MqttClient(brokerIpAddress);
       
        publishClient.Connect(clientID.Value);
        // Assign a callback to be executed when a message is published to the broker
        publishClient.MqttMsgPublished += PublishClientMqttMsgPublished;

        
        //MiTask = new PeriodicTask(PublishMessage, 250, LogicObject);
        //MiTask.Start();

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

    [ExportMethod]
    public void PublishMessage()
    {
        float Mixer_temperature = Project.Current.GetVariable("Model/Asset Indicators/Mixer/Mixer_Temperature").Value;
        float Mixer_Pressure = Project.Current.GetVariable("Model/Asset Indicators/Mixer/Mixer_Pressure").Value;
        float Mixer_Vibration = Project.Current.GetVariable("Model/Asset Indicators/Mixer/Mixer_Vibration").Value;
        float Oven_temperature = Project.Current.GetVariable("Model/Asset Indicators/Oven/Oven_Temperature").Value;
        float Oven_Pressure = Project.Current.GetVariable("Model/Asset Indicators/Oven/Oven_Pressure").Value;
        float Oven_Vibration = Project.Current.GetVariable("Model/Asset Indicators/Oven/Oven_Vibration").Value;
        float Packaging_temperature = Project.Current.GetVariable("Model/Asset Indicators/Packaging/Packaging_Temperature").Value;
        float Packaging_Pressure = Project.Current.GetVariable("Model/Asset Indicators/Packaging/Packaging_Pressure").Value;
        float Packaging_Vibration = Project.Current.GetVariable("Model/Asset Indicators/Packaging/Packaging_Vibration").Value;
        float Labeler_temperature = Project.Current.GetVariable("Model/Asset Indicators/Labeler/Labeler_Temperature").Value;
        float Labeler_Pressure = Project.Current.GetVariable("Model/Asset Indicators/Labeler/Labeler_Pressure").Value;
        float Labeler_Vibration = Project.Current.GetVariable("Model/Asset Indicators/Labeler/Labeler_Vibration").Value;

        
            // Get a common timestamp for all variables (assuming Unix timestamp in milliseconds)
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Create a JSON object with your variables and add the timestamp to each variable
        var jsonData = new
        {
            Mixer = new
            {
                Temperature = new
                {
                    Value = Mixer_temperature,
                    Timestamp = timestamp
                },
                Pressure = new
                {
                    Value = Mixer_Pressure,
                    Timestamp = timestamp
                },
                Vibration = new
                {
                    Value = Mixer_Vibration,
                    Timestamp = timestamp
                }
            },
            Oven = new
            {
                Temperature = new
                {
                    Value = Oven_temperature,
                    Timestamp = timestamp
                },
                Pressure = new
                {
                    Value = Oven_Pressure,
                    Timestamp = timestamp
                },
                Vibration = new
                {
                    Value = Oven_Vibration,
                    Timestamp = timestamp
                }
            },
            Packaging = new
            {
                Temperature = new
                {
                    Value = Packaging_temperature,
                    Timestamp = timestamp
                },
                Pressure = new
                {
                    Value = Packaging_Pressure,
                    Timestamp = timestamp
                },
                Vibration = new
                {
                    Value = Packaging_Vibration,
                    Timestamp = timestamp
                }
            },
            Labeler = new
            {
                Temperature = new
                {
                    Value = Labeler_temperature,
                    Timestamp = timestamp
                },
                Pressure = new
                {
                    Value = Labeler_Pressure,
                    Timestamp = timestamp
                },
                Vibration = new
                {
                    Value = Labeler_Vibration,
                    Timestamp = timestamp
                }
            }
        };

        // Serialize the JSON object to a string
        string jsonMessage = JsonConvert.SerializeObject(jsonData);

        // Publish the JSON message
        var topic = LogicObject.GetVariable("Topic");
        ushort msgId = publishClient.Publish(topic.Value, // topic
            System.Text.Encoding.UTF8.GetBytes(jsonMessage), // message body
            MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, // QoS level
            false); // retained
        
    }

    private MqttClient publishClient;
}
