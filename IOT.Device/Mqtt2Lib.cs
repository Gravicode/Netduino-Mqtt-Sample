using System;
using Microsoft.SPOT;
using System.Threading;
using System.Net.Sockets;
using Microsoft.SPOT.Hardware;
using System.Net;
using SecretLabs.NETMF.Hardware.Netduino;

namespace IOT.Device
{
    class Mqtt2Lib
    {

        static Thread listenerThread;
        static Socket mySocket = null;

        public static void Mqtt2()
        {

            int returnCode = 0;

            // You can subscribe to multiple topics in one go 
            // (If your broker supports this RSMB does, mosquitto does not)
            // Our examples use one topic per request.
            //
            //int[] topicQoS = { 0, 0 };
            //String[] subTopics = { "test", "test2" };
            //int numTopics = 2;

            int[] topicQoS = { 0 };
            String[] subTopics = { "test" };
            int numTopics = 1;

            // Get broker's IP address.
            //IPHostEntry hostEntry = Dns.GetHostEntry("test.mosquitto.org");
            IPHostEntry hostEntry = Dns.GetHostEntry("192.168.100.8");

            // Create socket and connect to the broker's IP address and port
            mySocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                mySocket.Connect(new IPEndPoint(hostEntry.AddressList[0], 1883));
            }
            catch (SocketException SE)
            {
                Debug.Print("Connection Error: " + SE.ErrorCode);
                return;
            }

            // Send the connect message
            // You can use UTF8 in the clientid, username and password - be careful, this can be a pain
            //returnCode = NetduinoMQTT.ConnectMQTT(mySocket, "tester\u00A5", 2000, true, "roger\u00A5", "password\u00A5");
            returnCode = NetduinoMQTT.ConnectMQTT(mySocket, "guest001", 20, true, "guest", "guest");
            if (returnCode != 0)
            {
                Debug.Print("Connection Error: " + returnCode.ToString());
                return;
            }

            // Set up so that we ping the server after 1 second, then every 10 seconds
            // First time is initial delay, Second is subsequent delays
            Timer pingTimer = new Timer(new TimerCallback(pingIt), null, 1000, 10000);

            // Setup and start a new thread for the listener
            listenerThread = new Thread(mylistenerThread);
            listenerThread.Start();

            // setup our interrupt port (on-board button)
            InterruptPort button = new InterruptPort(Pins.ONBOARD_SW1, false, Port.ResistorMode.Disabled, Port.InterruptMode.InterruptEdgeLow);

            // assign our interrupt handler
            button.OnInterrupt += new NativeEventHandler(button_OnInterrupt);

            // Subscribe to our topic(s)
            returnCode = NetduinoMQTT.SubscribeMQTT(mySocket, subTopics, topicQoS, numTopics);

            //***********************************************
            // This is just some example stuff:
            //***********************************************

            // Publish a message
            NetduinoMQTT.PublishMQTT(mySocket, "/iot/status", "Testing from NetduinoMQTT");

            // Subscribe to "test/two"
            subTopics[0] = "/iot/status";
            returnCode = NetduinoMQTT.SubscribeMQTT(mySocket, subTopics, topicQoS, numTopics);

            // Send a message to "test/two"
            NetduinoMQTT.PublishMQTT(mySocket, "/iot/status", "Testing from NetduinoMQTT to test/two");

            // Unsubscribe from "test/two"
            returnCode = NetduinoMQTT.UnsubscribeMQTT(mySocket, subTopics, topicQoS, numTopics);

            // go to sleep until the interrupt or the timer wakes us 
            // (mylistenerThread is in a seperate thread that continues)
            Thread.Sleep(Timeout.Infinite);
        }

        // the interrupt handler for the button
        static void button_OnInterrupt(uint data1, uint data2, DateTime time)
        {
            // Send our message
            NetduinoMQTT.PublishMQTT(mySocket, "/iot/status", "Ow! Quit it!");
            return;
        }

        // The thread that listens for inbound messages
        private static void mylistenerThread()
        {
            NetduinoMQTT.listen(mySocket);
        }

        // The function that the timer calls to ping the server
        // Our keep alive is 15 seconds - we ping again every 10. 
        // So we should live forever.
        static void pingIt(object o)
        {
            Debug.Print("pingIT");
            NetduinoMQTT.PingMQTT(mySocket);
        }

    }
}
