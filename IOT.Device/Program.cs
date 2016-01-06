using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;
using uPLibrary.Networking.M2Mqtt;
using System.Text;
using System.Collections;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace IOT.Device
{
    public class Program
    {
        const string MQTT_BROKER_ADDRESS = "192.168.100.8";
        public static ArrayList DevicePorts { set; get; }
        public static ArrayList devices { set; get; }
        public static MqttClient client { set; get; }
        static void SetupDevice()
        {
            //waiting till connect...
            if (!Microsoft.SPOT.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()[0].IsDhcpEnabled)
            {
                // using static IP
                while (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable()) ; // wait for network connectivity
            }
            else
            {
                // using DHCP
                while (IPAddress.GetDefaultLocalAddress() == IPAddress.Any) ; // wait for DHCP-allocated IP address
            }
            //Debug print our IP address
            Debug.Print("Device IP: " + Microsoft.SPOT.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()[0].IPAddress);
            //PingServer();
            // write your code here
            devices = new ArrayList();
            for (int i = 0; i < 15; i++)
            {
                devices.Add(false);
            }
            DevicePorts = new ArrayList();
            DevicePorts.Add(new OutputPort(Pins.GPIO_PIN_D0, false));
            DevicePorts.Add(new OutputPort(Pins.GPIO_PIN_D1, false));
            DevicePorts.Add(new OutputPort(Pins.GPIO_PIN_D2, false));
            DevicePorts.Add(new OutputPort(Pins.GPIO_PIN_D3, false));
            DevicePorts.Add(new OutputPort(Pins.GPIO_PIN_D4, false));
            DevicePorts.Add(new OutputPort(Pins.GPIO_PIN_D5, false));
            DevicePorts.Add(new OutputPort(Pins.GPIO_PIN_D6, false));
            DevicePorts.Add(new OutputPort(Pins.GPIO_PIN_D7, false));
            DevicePorts.Add(new OutputPort(Pins.GPIO_PIN_D8, false));
            DevicePorts.Add(new OutputPort(Pins.GPIO_PIN_D9, false));
            DevicePorts.Add(new OutputPort(Pins.GPIO_PIN_D10, false));
            DevicePorts.Add(new OutputPort(Pins.GPIO_PIN_D11, false));
            DevicePorts.Add(new OutputPort(Pins.GPIO_PIN_D12, false));
            DevicePorts.Add(new OutputPort(Pins.GPIO_PIN_D13, false));
            DevicePorts.Add(new OutputPort(Pins.ONBOARD_LED, false));
        }
        static void PingServer()
        {
            RawSocketPing pingSocket = null;

            IPAddress remoteAddress = IPAddress.Parse(MQTT_BROKER_ADDRESS);

            int dataSize = 512, ttlValue = 128, sendCount = 8;

            try
            {
                // Create a RawSocketPing class that wraps all the ping functionality
                pingSocket = new RawSocketPing(
                    ttlValue,
                    dataSize,
                    sendCount,
                    123
                    );

                // Set the destination address we want to ping
                pingSocket.PingAddress = remoteAddress;

                // Initialize the raw socket
                pingSocket.InitializeSocket();

                // Create the ICMP packets to send
                pingSocket.BuildPingPacket();

                // Actually send the ping request 
                bool success = pingSocket.DoPing();
                OutputPort ledblink = (OutputPort)DevicePorts[14];
                if (success)
                {
                    Debug.Print("Hey, we got a response!");
                    ledblink.Write(true);
                    ledblink.Write(false);
                    ledblink.Write(true);
                    ledblink.Write(false);
                }
            }
            catch (SocketException err)
            {
                Debug.Print("Socket error occured: " + err.Message);
            }
            finally
            {
                if (pingSocket != null)
                    pingSocket.Close();
            }
            return;
        }
        public static void Main()
        {
            try
            {
                SetupDevice();
                // create mqtt client instance 
                client = new MqttClient(IPAddress.Parse(MQTT_BROKER_ADDRESS));
                string clientId = Guid.NewGuid().ToString();
                client.Connect(clientId);
                SubscribeMessage();
                //update status periodically
                while (true)
                {
                    string Status = string.Empty;
                    for (int i = 0; i < devices.Count; i++)
                    {
                        Status += i + ":" + devices[i].ToString() + ";";
                    }
                    PublishMessage("/iot/status", Status);
                    Thread.Sleep(1000);
                }
                client.Disconnect();
                //loop forever
                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message + "_" + ex.StackTrace);
            }
        }
        static void PublishMessage(string Topic, string Pesan)
        {
            client.Publish(Topic, Encoding.UTF8.GetBytes(Pesan), MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, false);
        }


        static void SubscribeMessage()
        {
            // register to message received 
            client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
            client.Subscribe(new string[] { "/iot/devices" }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });

        }

        static void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {

            string Message = new string(Encoding.UTF8.GetChars(e.Message));
            if (Message.IndexOf(":") < 1) return;
            // handle message received 
            Debug.Print("Message Received : " + Message);
            string[] pindevice = Message.Split(';');
            foreach (string item in pindevice)
            {
                string[] pinItem = item.Split(':');
                int pinsel = Convert.ToInt32(pinItem[0]);
                bool state = pinItem[1] == "True" ? true : false;
                if ((bool)devices[pinsel] != state)
                {
                    devices[pinsel] = state;
                    OutputPort selectedPort = (OutputPort)DevicePorts[pinsel];
                    selectedPort.Write(state);
                    PublishMessage("/iot/change", pinsel + ":" + state.ToString());
                }
            }

        }
    }
}
