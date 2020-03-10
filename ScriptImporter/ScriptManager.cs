/*

Copyright © 2019 Tara Piccari (Aria; Tashia Redrose)
Licensed under the GPLv2

*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Bot;
using Bot.CommandSystem;
using OpenMetaverse;

namespace OpenCollarBot.ScriptImporter
{
    class ScriptManager : IProgram
    {
        public string ProgramName
        {
            get
            {
                return "ScriptManager";
            }
            set
            {
                // dont set anything
            }
        }

        public float ProgramVersion
        {
            get
            {
                return 1.03f;
            }
        }

        public string getTick()
        {
            // Check Script Queue
            QueueRunner.run();
            return "";
        }

        public void LoadConfiguration()
        {
            // Initialize Script Manager Singletons
        }

        public void onIMEvent(object sender, InstantMessageEventArgs e)
        {

        }

        public void passArguments(string data)
        {

        }

        public void run(GridClient client, MessageHandler MH, CommandRegistry registry)
        {
            // Start Plugin
        }


        public static void AddQueue(ScriptList ListItems, UUID Requester, string GitOwner="OpenCollarTeam", string GitBranch="master")
        {
            Queue inst = Queue.Instance;
            List<Queue.QueueType> Queued = new List<Queue.QueueType>();
            foreach(KeyValuePair<string, ScriptList.ScriptListFlags> kvp in ListItems.Scripts)
            {
                Queue.QueueType QT = new Queue.QueueType();
                QT.Name = kvp.Value.ScriptName;
                QT.GitOwner = GitOwner;

                HttpWebRequest hwr = null;
                HttpWebResponse hwresp = null;

                try
                {
                    hwr = (HttpWebRequest)HttpWebRequest.Create("https://raw.githubusercontent.com/" + GitOwner + "/OpenCollar/"+GitBranch+"/src/"+kvp.Value.Container+"/"+kvp.Value.ScriptName+kvp.Value.FileExt);

                    hwr.Method = "GET";
                    hwresp = (HttpWebResponse)hwr.GetResponse();

                }
                catch (Exception e) { }

                if(hwresp.StatusCode == HttpStatusCode.NotFound)
                {
                    BotSession.Instance.MHE(MessageHandler.Destinations.DEST_AGENT, Requester, "ALERT: BDF Entry: " + kvp.Value.ScriptName+kvp.Value.FileExt + "; Does not exist on the server!");
                    continue;
                } else if(hwresp.StatusCode == HttpStatusCode.OK)
                {
                    // Get the file text and add to queued items
                    StreamReader sr = new StreamReader(hwresp.GetResponseStream());
                    QT.Text = sr.ReadToEnd();
                    //Console.WriteLine("Generating ZHX for string with length : "+QT.Text.Length.ToString());
                    Console.WriteLine("MD5 [" + QT.Name + "] : " + Tools.MD5Hash(QT.Text));
                    QT.Hash = Tools.MD5Hash(QT.Text);
                    //Console.WriteLine("ZHX [" + QT.Name + "] : " + QT.Hash);
                    QT.GitBranch = GitBranch;
                    QT.Container = kvp.Value.Container;
                    QT.ItemType = kvp.Value.Type;
                    QT.FileExt = kvp.Value.FileExt;

                    Queued.Add(QT);
                }
            }

            inst.ActualQueue.Add(Requester, Queued);
        }



        [CommandGroup("reply_prompt", 0, 2, "reply_prompt [int:PromptID] [int:ButtonNumber]", MessageHandler.Destinations.DEST_LOCAL | MessageHandler.Destinations.DEST_AGENT)]
        public void dialog_reply(UUID client, int level, GridClient grid, string[] additionalArgs,
                                MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source,
                                CommandRegistry registry, UUID agentKey, string agentName)
        {
            MHE(source, client, "Responding..");

            int DialogID = Convert.ToInt32(additionalArgs[0]);
            int ButtonNum = Convert.ToInt32(additionalArgs[1]);

            // Get the scriptsession

            if (OCBSession.Instance.ScriptSessions.ContainsKey(DialogID) == false)
            {
                MHE(source, client, "No such dialog ID");
                return;
            }
            Program.ScriptDialogSession SDS = OCBSession.Instance.ScriptSessions[DialogID];

            

            BotSession.Instance.grid.Self.ReplyToScriptDialog(SDS.ReplyChannel, ButtonNum, SDS.Buttons[ButtonNum], SDS.ObjectKey);

            OCBSession.Instance.ScriptSessions.Remove(DialogID);
        }
    }
}
