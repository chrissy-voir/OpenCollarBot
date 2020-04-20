using System;
using System.Collections.Generic;
using System.IO;
using System.Collections.Specialized;
using Bot.WebHookServer;

namespace OpenCollarBot
{
    public sealed class GroupLog
    {
        private static readonly object _lock = new object();
        private static GroupLog _in;
        private static readonly object _writeLock = new object();
        private static readonly object _fileRead = new object();
        static GroupLog() { }

        public static GroupLog Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_in == null) _in = new GroupLog();
                    return _in;
                }
            }
        }

        

        [WebhookAttribs("/viewlog/%", HTTPMethod = "GET")]
        public WebhookRegistry.HTTPResponseData View_Log(List<string> arguments, string body, string method, NameValueCollection headers)
        {
            WebhookRegistry.HTTPResponseData rd = new WebhookRegistry.HTTPResponseData();

            string FinalOutput = "";
            lock (_fileRead)
            {
                try
                {

                    foreach (string s in File.ReadLines("GroupChatLogs/" + Uri.UnescapeDataString(arguments[0]) + ".log"))
                    {
                        string tmp = s;
                        string[] Ltmp = tmp.Split(' ');
                        tmp = "";
                        foreach(string K in Ltmp)
                        {
                            if (K.StartsWith("secondlife://"))
                            {
                                // DO NOT ADD TO OUTPUT
                            }
                            else
                            {
                                tmp += K + " ";
                            }
                        }

                        FinalOutput += tmp+"<br/>";

                    }
                    rd.Status = 200;
                    rd.ReplyString = FinalOutput;
                    rd.ReturnContentType = "text/html";
                } catch(Exception e)
                {
                    rd.Status = 418;
                    rd.ReplyString = "You burned... the tea";
                }
            }

            return rd;
        }

        [WebhookAttribs("/logs", HTTPMethod = "GET")]
        public WebhookRegistry.HTTPResponseData List_Logs(List<string> arguments, string body, string method, NameValueCollection headers)
        {
            WebhookRegistry.HTTPResponseData hrd = new WebhookRegistry.HTTPResponseData();
            hrd.Status = 200;
            hrd.ReplyString = "<center><h2>Group Chat Logs</h2></center>";
            DirectoryInfo di = new DirectoryInfo("GroupChatLogs");
            foreach(FileInfo fi in di.GetFiles())
            {
                hrd.ReplyString += "<br/><a href='/viewlog/"+Path.GetFileNameWithoutExtension(fi.Name)+"'> " + fi.Name + "</a>";
            }
            hrd.ReturnContentType = "text/html";

            return hrd;
        }
    }
}
