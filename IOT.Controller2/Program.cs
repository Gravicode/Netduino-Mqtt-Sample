using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Configuration;
using System.Xml.Linq;
using System.Globalization;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Windows.Threading;

namespace IOT.Controller2
{
    class Program
    {
        public static bool isRecognizing { set; get; }
        public static bool isActive { set; get; }
        public static MqttClient client { set; get; }
        public static Dictionary<string, Module> Perintah { set; get; }
        //Speech recognition & synthetizer

        static PXCMAudioSource source;
        static PXCMSpeechRecognition sr;
        static PXCMSession session;


        const string MQTT_BROKER_ADDRESS = "192.168.1.102";

        #region Speech

        static void SetupRecognizer(PXCMSession session)
        {

            PXCMAudioSource.DeviceInfo dinfo = null;
            if (session != null)
            {

                #region Audio Source

                // session is a PXCMSession instance.
                source = session.CreateAudioSource();

                // Scan and Enumerate audio devices
                source.ScanDevices();

                for (int d = source.QueryDeviceNum() - 1; d >= 0; d--)
                {
                    source.QueryDeviceInfo(d, out dinfo);

                    // Select one and break out of the loop
                    break;
                }
                if (dinfo != null)
                    // Set the active device
                    source.SetDevice(dinfo);

                #endregion

                #region Recognizer Instance

                pxcmStatus sts = session.CreateImpl<PXCMSpeechRecognition>(out sr);

                if (sts < pxcmStatus.PXCM_STATUS_NO_ERROR) return;

                PXCMSpeechRecognition.ProfileInfo pinfo;
                sr.QueryProfile(0, out pinfo);
                sr.SetProfile(pinfo);

                //sr.SetDictation();

                #endregion

                #region Grammar
                Perintah = new Dictionary<string, Module>();
                // sr is a PXCMSpeechRecognition instance.
                using (var data = new JONGOS_DBEntities())
                {
                    var listCommand = from c in data.Modules
                                      orderby c.ID
                                      select c;
                    foreach (var item in listCommand.Distinct())
                    {
                        Perintah.Add(item.VoiceCommand, item);

                    }
                }
                List<string> cmds = new List<string>();
                foreach (var cmd in Perintah.Keys)
                {
                    cmds.Add(cmd);
                }

                // Build the grammar.
                sr.BuildGrammarFromStringList(1, cmds.ToArray(), null);

                // Set the active grammar.
                sr.SetGrammar(1);
                #endregion

                #region recognition Event
                // Set handler
                PXCMSpeechRecognition.Handler handler = new PXCMSpeechRecognition.Handler();
                handler.onRecognition = OnRecognition;
                //handler.onAlert = OnAlert;
                // sr is a PXCMSpeechRecognition instance
                pxcmStatus stsrec = sr.StartRec(source, handler);
                if (stsrec < pxcmStatus.PXCM_STATUS_NO_ERROR)
                {
                    Console.WriteLine("Recognizer error!");
                }


                #endregion


            }
        }
        void OnAlert(PXCMSpeechRecognition.AlertData data)
        {
            //Console.WriteLine(data.label);
        }
        static void OnRecognition(PXCMSpeechRecognition.RecognitionData data)
        {
            /*
            Console.WriteLine("grammar :" + data.grammar);
            foreach (var item in data.scores)
            {
                Console.WriteLine(string.Format("sentence : {0}, label : {1}, tags: {2}, confidence : {3} ", item.sentence, item.label, item.tags, item.confidence));
            }*/
            // Process Recognition Data
            if (isRecognizing) return;
            const int ConfidenceFactor = 45;

            if (data.scores.Length <= 0) return;
            try
            {
                var selVoice = data.scores[0];
                if (selVoice.confidence < ConfidenceFactor) return;
                isRecognizing = true;
                if (Perintah.ContainsKey(selVoice.sentence))
                {
                    string Result = string.Empty;
                    bool GagalSuruh = false;
                    Module selItem = Perintah[selVoice.sentence];
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

                        VoiceSynthesis.Speak(selItem.MachineAnswer, 100, 0, 0);

                    }
                    else
                        if (GagalSuruh)
                        {

                            VoiceSynthesis.Speak("Please say the keyword", 100, 0, 0);

                        }

                    if (!string.IsNullOrEmpty(Result))
                    {

                        VoiceSynthesis.Speak(Result, 100, 0, 0);


                    }


                }


            }
            catch
            {
            }
            finally
            {
                isRecognizing = false;
            }
        }



        #endregion

        static void SubscribeMessage()
        {
            // register to message received 
            client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;

             client.Subscribe(new string[] { "/iot/status", "/iot/change", "/iot/devices" }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });

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
            client.Connect(clientId, "guest", "guest");

            SubscribeMessage();

            isActive = false;
            //Init speech recognizer
            session = PXCMSession.CreateInstance();
            SetupRecognizer(session);

            Console.WriteLine("Siap melayani boss...");
            Console.WriteLine("Teken 'x' kalau mau mecat saya");
            while (true)
            {
                ConsoleKeyInfo pressedKey = Console.ReadKey(true);
                char keychar = pressedKey.KeyChar;
                if (keychar == 'x') break;
            }

            Console.WriteLine("Selamat tinggal bos, senang melayani..");
            client.Disconnect();
            try
            {

                if (sr != null)
                {
                    sr.StopRec();
                    sr.Dispose();
                    sr = null;
                }
                if (source != null)
                {
                    source.Dispose();
                    source = null;
                }
                if (session != null) session.Dispose();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + " : " + ex.StackTrace);
            }
        }


        /*
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
        }*/

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



        static void DoItNow(string CmdStr)
        {
            try
            {
                string Pesan = CmdStr.Replace("PIN", string.Empty).Replace("_", ":").Replace("ON", "True").Replace("OFF", "False");
                client.Publish("/iot/devices", Encoding.UTF8.GetBytes(Pesan), MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, false);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }


    }

}
