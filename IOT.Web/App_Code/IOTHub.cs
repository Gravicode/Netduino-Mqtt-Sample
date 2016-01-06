using System;
using System.Web;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using System.Configuration;
using uPLibrary.Networking.M2Mqtt;
using System.Net;
using System.Text;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace IOT.Web
{
    [Serializable]
    public class IOTDevice
    {
        public int ID{set;get;}
        public bool State{set;get;}
    }
    [HubName("IOTHub")]
    public class IOTHub : Hub
    {
        public static MqttClient client { set; get; }
        public static string MQTT_BROKER_ADDRESS
        {
            get { return ConfigurationManager.AppSettings["MQTT_BROKER_ADDRESS"]; }
        }
        static void SubscribeMessage()
        {
            // register to message received 
            client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;

            // subscribe to the topic "/home/temperature" with QoS 2 
            client.Subscribe(new string[] { "/iot/status", "/iot/change", "/iot/devices" }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });

        }

       
        static void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            string Pesan = Encoding.UTF8.GetString(e.Message);
            switch (e.Topic)
            {
                case "/iot/status":
                    WriteMessage(Pesan);
                    UpdateState(Pesan);
                    
                    break;

                case "/iot/change":
                    WriteMessage(Pesan);
                    break;

                case "/iot/devices":
                    int PIN = int.Parse(Pesan.Split(':')[0]);
                    bool State = bool.Parse(Pesan.Split(':')[1]);
                    ChangeState(PIN, State);
                    break;
            }
            // handle message received 
            //Console.WriteLine("Message : " + Encoding.UTF8.GetString(e.Message));
        }
        public IOTHub()
        {
            if (client == null)
            {
                // create client instance 
                client = new MqttClient(IPAddress.Parse(MQTT_BROKER_ADDRESS));

                string clientId = Guid.NewGuid().ToString();
                client.Connect(clientId, "guest", "guest");

                SubscribeMessage();
            }
        }

        [HubMethodName("ToggleSwitch")]
        public void ToggleSwitch(int Pin,bool State)
        {
            string Pesan = Pin + ":" + State.ToString();
            client.Publish("/iot/devices", Encoding.UTF8.GetBytes(Pesan), MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, false);

        }
        internal static void UpdateState(string message)
        {
            try
            {
                List<IOTDevice> datas = new List<IOTDevice>();
                var context = GlobalHost.ConnectionManager.GetHubContext<IOTHub>();
                foreach (var str in message.Split(';'))
                {
                    if (string.IsNullOrEmpty(str.Trim())) continue;
                    int PIN = int.Parse(str.Split(':')[0]);
                    bool State = bool.Parse(str.Split(':')[1]);
                    IOTDevice node = new IOTDevice() { ID = PIN, State = State };
                    datas.Add(node);
                }
                dynamic allClients = context.Clients.All.UpdateState(JsonConvert.SerializeObject(datas));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        internal void WriteRawMessage(string msg)
        {
            WriteMessage(msg);
        }
        internal static void WriteMessage(string message)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<IOTHub>();
            dynamic allClients = context.Clients.All.WriteData(message);
        }
        internal static void ChangeState(int PIN,bool Status)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<IOTHub>();
            dynamic allClients = context.Clients.All.ChangeState(PIN,Status);
        }
    }
}