using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Net;
using System.Configuration;
using System.Xml.Linq;
using System.Globalization;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace IOT.Controller
{
    class Program
    {
        public static bool isRecognizing { set; get; }
        public static bool isActive { set; get; }
        public static string URL_SERVICE { set; get; }
        public static MqttClient client{set;get;}
        public static Dictionary<string, Module> Perintah { set; get; }
        static SpeechRecognitionEngine _recognizer = null;
        static ManualResetEvent manualResetEvent = null;

        const string MQTT_BROKER_ADDRESS = "192.168.100.8";

        static void SubscribeMessage()
        {
            // register to message received 
            client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;

            // subscribe to the topic "/home/temperature" with QoS 2 
            client.Subscribe(new string[] { "/iot/status", "/iot/change", "/iot/devices" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
            
        }

        static void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            // handle message received 
            Console.WriteLine("Message : " + Encoding.UTF8.GetString(e.Message));
        }

        static void Main(string[] args)
        {
            // create client instance 
            client = new MqttClient(IPAddress.Parse(MQTT_BROKER_ADDRESS));
            
            string clientId = Guid.NewGuid().ToString();
            client.Connect(clientId,"guest","guest");

            SubscribeMessage();

            Thread.Sleep(1000);
            
            Perintah = new Dictionary<string, Module>();
            URL_SERVICE = ConfigurationManager.AppSettings["URL_SERVICE"];
            isActive = false;
            manualResetEvent = new ManualResetEvent(false);

            ListenToBoss();
            Console.WriteLine("Siap melayani boss...");
            Console.WriteLine("Teken 'x' kalau mau mecat saya");
            while (true)
            {
                ConsoleKeyInfo pressedKey = Console.ReadKey(true);
                char keychar = pressedKey.KeyChar;
                if (keychar == 'x') break;
            }
            manualResetEvent.Set();
            if (_recognizer != null)
            {
                _recognizer.Dispose();
            }
            Console.WriteLine("Selamat tinggal bos, senang melayani..");
            client.Disconnect();

        }

        static void ListenToBoss()
        {
            CultureInfo ci = new CultureInfo("en-US");
            _recognizer = new SpeechRecognitionEngine(ci);
            // Select a voice that matches a specific gender.  

            using (var data = new JONGOS_DBEntities())
            {
                var listCommand = from c in data.Modules
                                  orderby c.ID
                                  select c;
                foreach (var item in listCommand.Distinct())
                {
                    Perintah.Add(item.VoiceCommand, item);
                    _recognizer.LoadGrammar(new Grammar(new GrammarBuilder(item.VoiceCommand)));
                }
            }
            isRecognizing = false;
            // load a "hello computer" grammar
            _recognizer.SpeechRecognized += _recognizer_SpeechRecognized; // if speech is recognized, call the specified method
            _recognizer.SpeechRecognitionRejected += _recognizer_SpeechRecognitionRejected;
            _recognizer.SetInputToDefaultAudioDevice(); // set the input to the default audio device
            _recognizer.RecognizeAsync(RecognizeMode.Multiple); // recognize speech asynchronous
        }

        static void _recognizer_SpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            if (e.Result.Alternates.Count == 0)
            {
                Console.WriteLine("Perintah tidak dikenal.");
                return;
            }
            Console.WriteLine("Perintah tidak dikenali, mungkin maksud tuan ini:");
            foreach (RecognizedPhrase r in e.Result.Alternates)
            {
                Console.WriteLine("    " + r.Text);
            }
        }

        private static string getDeviceName(int DeviceID)
        {
            using (var data = new JONGOS_DBEntities())
            {
                var listCommand = from c in data.Devices
                                  where c.DeviceID == DeviceID
                                  select c;
                foreach (var item in listCommand)
                {
                    return item.Name;
                }
            }
            return "unknown";
        }

        static void _recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (isRecognizing) return;
            isRecognizing = true;
            if (Perintah.ContainsKey(e.Result.Text))
            {
                string Result = string.Empty;
                bool GagalSuruh = false;

                SpeechSynthesizer speechSynthesizer = new SpeechSynthesizer();
                // Select a voice that matches a specific gender.  
                speechSynthesizer.SelectVoiceByHints(VoiceGender.Female);

                Module selItem = Perintah[e.Result.Text];
                if (selItem.SpecialParam == "ACTIVATION")
                {
                    isActive = true;
                }
                else if (selItem.SpecialParam == "DEACTIVATION")
                {
                    isActive = false;
                }
                else if (selItem.isAction.HasValue && selItem.isAction.Value)
                {
                    if (isActive)
                    {
                        if (!string.IsNullOrEmpty(selItem.CommandUrl))
                        {
                            DoItNow(selItem.CommandUrl);
                        }
                    }
                    else
                        GagalSuruh = true;
                }

                if (selItem.isSpeak.HasValue && selItem.isSpeak.Value && !GagalSuruh)
                {
                    speechSynthesizer.Speak(selItem.MachineAnswer);
                }
                else
                    if (GagalSuruh)
                    {
                        speechSynthesizer.Speak("Please say the keyword");
                    }

                if (!string.IsNullOrEmpty(Result))
                {
                    speechSynthesizer.Speak(Result);
                }
                speechSynthesizer.Dispose();

            }
            isRecognizing = false;
        }

        static void DoItNow(string CmdStr)
        {
            try
            {
                string Pesan = CmdStr.Replace("PIN", string.Empty).Replace("_", ":").Replace("ON", "True").Replace("OFF", "False");
                client.Publish("/iot/devices",Encoding.UTF8.GetBytes(Pesan), MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, false);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        
    }
   
}
