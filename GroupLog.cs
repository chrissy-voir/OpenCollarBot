﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Bot.CommandSystem;
using Bot;
using OpenMetaverse;
using OpenCollarBot.Webhooks;
using System.Collections.Specialized;

namespace OpenCollarBot
{
    public sealed class GroupLog
    {
        private static readonly object _lock = new object();
        private static GroupLog _in;
        private static readonly object _writeLock = new object();

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

        
        public void WriteLogEntry(string LogName, string ToAppend)
        {
            // Log filename will ALWAYS contain the date
            try
            {
                lock (_writeLock)
                {

                    string date = DateTime.Now.ToString("M-d-yyyy");
                    date += " " + LogName + ".log";

                    if (!Directory.Exists("GroupChatLogs")) Directory.CreateDirectory("GroupChatLogs");

                    date = "GroupChatLogs/" + date;

                    File.AppendAllText(date, "["+DateTime.Now.ToString("hh:mm:ss")+"]: "+ToAppend+"\n");
                }
            } catch(Exception e) { }
        }

        private static readonly object _fileRead = new object();
        [CommandGroupMaster("Logging")]
        [CommandGroup("search_log", 5, 2, "search_log [uuid] [search_term]  -  Searches for the search term in all logs relating to the group uuid. The search term may also include the pipe (|) delimiter to include more than 1 word.", Bot.MessageHandler.Destinations.DEST_AGENT| Bot.MessageHandler.Destinations.DEST_LOCAL)]
        public void search_log(UUID client, int level, GridClient grid, string[] additionalArgs,
                                SysOut log, MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source,
                                CommandRegistry registry, UUID agentKey, string agentName)
        {
            string GrpName = grid.Groups.GroupName2KeyCache[UUID.Parse(additionalArgs[0])];
            string[] search = additionalArgs[1].Split('|');

            DirectoryInfo di = new DirectoryInfo("GroupChatLogs");
            foreach (FileInfo fi in di.GetFiles())
            {
                // check if filename contains the group name
                string onlyName = Path.GetFileNameWithoutExtension(fi.Name);

                if (onlyName.Contains(GrpName))
                {
                    // read file
                    lock (_fileRead)
                    {
                        foreach(string S in File.ReadLines("GroupChatLogs/" + onlyName + ".log"))
                        {
                            foreach(string V in search)
                            {
                                if (S.Contains(V, StringComparison.OrdinalIgnoreCase))
                                {
                                    
                                    MHE(source, client, "{[http://xsinode.net:35591/viewlog/"+Uri.EscapeUriString(onlyName)+" "+onlyName+"]} "+S);
                                }
                            }
                        }

                        
                    }
                }

            }

            MHE(source, client, ".\n \n[Search Completed]");
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
                } catch(Exception e)
                {
                    rd.Status = 418;
                    rd.ReplyString = "You burned... the tea";
                }
            }

            return rd;
        }
    }
}
