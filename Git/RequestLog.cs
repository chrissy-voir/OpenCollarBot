/*

Copyright © 2019 Tara Piccari (Aria; Tashia Redrose)
Licensed under the GPLv2

*/

using System;
using System.Collections.Generic;
using Bot;
using System.IO;

namespace OpenCollarBot.Git
{
    [Serializable()]
    public class RequestLog : IConfig
    {
        public float ConfigVersion { get; set; }
        public string ConfigFor { get; set; }
        public List<string> Data { get; set; }

        [Serializable()]
        public struct HTTPData
        {
            public string GitEventHeader;
            public string body;
            public void setBody(string T)
            {
                body = T;
            }
        }

        public List<HTTPData> logged_data { get; set; }

        public void Save(string CustomName = "last_request")
        {
            //if (!File.Exists("OpenCollarBot.bdf")) return;
            SerialManager sm = new SerialManager();
            sm.Write<RequestLog>("request_log/" + CustomName, this);
            sm = null;
        }


        public static RequestLog Reload(string CustomName = "last_request")
        {
            if (!File.Exists("request_log/" + CustomName + ".bdf")) return new RequestLog();
            SerialManager sm = new SerialManager();
            RequestLog RL = sm.Read<RequestLog>("request_log/" + CustomName);
            if (RL == null)
            {
                Console.WriteLine("BDF is null. Returning new instance");
                return new RequestLog();
            }
            RL.Integrity();
            return RL;
        }
        public RequestLog()
        {
            Data = new List<string>();
            ConfigFor = "";
            ConfigVersion = 1.0f;
            logged_data = new List<HTTPData>();
            Integrity();
        }

        public void Integrity()
        {
            if (Data == null) Data = new List<string>();
            if (ConfigFor == null) ConfigFor = "";
            if (ConfigVersion == null) ConfigVersion = 1.0f;
            if (logged_data == null) logged_data = new List<HTTPData>();
        }
    }
}
