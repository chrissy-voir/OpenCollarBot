/*

Copyright © 2020 Tara Piccari (Aria; Tashia Redrose)
Licensed under the GPLv2

*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bot;
using Bot.CommandSystem;
using OpenMetaverse;
using System.Threading;
using System.IO;
using Newtonsoft.Json;
using OpenMetaverse.Packets;
using OpenCollarBot.GroupCommands;
using Logger = Bot.Logger;

namespace OpenCollarBot
{
    public class Program : BaseCommands, IProgram
    {
        public GridClient grid;
        public CommandManager CM = null;
        public OCBotMemory BMem = null;
        public Logger Log = BotSession.Instance.Logger;
        public CommandRegistry registry;

        public string ProgramName
        {
            get { return "OC"; }
        }
        public float ProgramVersion { get { return 1.0f; } }


        private DateTime LastScheduleCheck;

        public void getTick()
        {
            GroupSystem.PerformCheck(LastScheduleCheck);

            if (DateTime.Now > LastScheduleCheck) LastScheduleCheck = DateTime.Now + TimeSpan.FromMinutes(5);
            
            foreach(KeyValuePair<UUID,OCBotMemory.ReplyData> kvp in OCBotMemory.Memory.AntiSpamReply)
            {
                if (kvp.Value.Ignore) continue; // This will not get reset here

                if(kvp.Value.InitialReply.AddMinutes(OCBotMemory.Memory.REPLY_BLOCK_EXPIRE.TotalMinutes) <= DateTime.Now)
                {
                    if(kvp.Value.TriggerCount < OCBotMemory.Memory.MAX_TRIGGERS)
                    {
                        // add to removal queue
                        if (OCBSession.Instance.RemoveReplyHandle.Contains(kvp.Key) == false)
                            OCBSession.Instance.RemoveReplyHandle.Add(kvp.Key);
                    }
                    else
                    {
                        kvp.Value.SetIgnore();
                    }
                }

                
            }
            BMem = OCBotMemory.Memory; // Read Singleton
            if (!BMem.iHaveBeenTeleported)
            {
                // check current region
                if(BMem.DefaultRegion != "" && DateTime.Now > OCBSession.Instance.NextTeleportAttempt)
                {
                    if (grid.Network.CurrentSim.Name != BMem.DefaultRegion)
                    {
                        OCBSession.Instance.NextTeleportAttempt = DateTime.Now.AddMinutes(1);
                        grid.Self.Teleport(BMem.DefaultRegion, BMem.DefaultLocation);
                    }
                }


            }


            if (BMem.AutoRestartSim && !OCBSession.Instance.RestartTriggered)
            {
                // Do checks
                DateTime timestamp = DateTime.Now;
                bool everyDay = false;
                DayOfWeek restarter = DayOfWeek.Monday;
                switch (BMem.RestartDay)
                {
                    case "mon":
                        restarter = DayOfWeek.Monday;
                        break;
                    case "tue":
                        restarter = DayOfWeek.Tuesday;
                        break;
                    case "wed":
                        restarter = DayOfWeek.Wednesday;
                        break;
                    case "thur":
                        restarter = DayOfWeek.Thursday;
                        break;
                    case "fri":
                        restarter = DayOfWeek.Friday;
                        break;
                    case "sat":
                        restarter = DayOfWeek.Saturday;
                        break;
                    case "sun":
                        restarter = DayOfWeek.Sunday;
                        break;
                    default:
                        everyDay = true;
                        break;
                }

                if (restarter == timestamp.DayOfWeek)
                {
                    everyDay = true; // this will keep logic to a minimum
                }
                if (everyDay)
                {
                    // do more checks
                    string timestampStr = timestamp.ToString("hh:mmtt");
                    timestampStr = timestampStr.ToLower();
                    string compareStr = BMem.TimeStringForRestart;

                    if(compareStr == timestampStr)
                    {
                        // restart
                        OCBSession.Instance.RestartTriggered = true;
                        BotSession.Instance.grid.Estate.RestartRegion();
                        BotSession.Instance.WaitForFiveMinutes = true;
                    }
                }
                else
                {
                    // do nothing
                }
            }

            return;
        }

        public void LoadConfiguration()
        {
            BMem = OCBotMemory.Memory;
        }


        public void passArguments(string data)
        {
            
            CM.RunChatCommand(data);
        }

        public void run()
        {
            if (Directory.Exists("GroupCache")) Directory.Delete("GroupCache", true); // Clear cache on restart
            registry = CommandRegistry.Instance;
            
            grid = BotSession.Instance.grid;
            grid.Inventory.InventoryObjectOffered += On_NewInventoryOffer;
            grid.Groups.GroupRoleDataReply += CacheGroupRoles;
            grid.Groups.GroupMembersReply += Groups_GroupMembersReply;

            LastScheduleCheck = DateTime.Now - TimeSpan.FromMinutes(5);
            Log = BotSession.Instance.Logger;
            BMem = OCBotMemory.Memory;

            if (BMem.status != "")
                MHE(Destinations.DEST_LOCAL, UUID.Zero, BMem.status);

            CM = new CommandManager();

            ReloadGroupsCache();
            if (grid.Network.CurrentSim.Name != BMem.DefaultRegion && BMem.DefaultRegion != "")
            {
                if (BMem.DefaultLocation != Vector3.Zero) grid.Self.Teleport(BMem.DefaultRegion, BMem.DefaultLocation);
            }

            if (BMem.sit_cube != UUID.Zero)
            {
                grid.Self.RequestSit(BMem.sit_cube, Vector3.Zero);
            }

            if (BMem.GroupKey != UUID.Zero) grid.Groups.ActivateGroup(BMem.GroupKey);


            BMem.iHaveBeenTeleported = false;
            BMem.Save(); // disable on relog
            grid.Self.ScriptDialog += onScriptDialog;
        }

        private void Groups_GroupMembersReply(object sender, GroupMembersReplyEventArgs e)
        {
            if (e.RequestID != OCBSession.Instance.MemberLookupRequest) return;


            foreach (KeyValuePair<UUID, GroupMember> kvp in e.Members)
            {
                
                //BotSession.Instance.MHE(MessageHandler.Destinations.DEST_LOCAL, UUID.Zero, $"[DEBUG] secondlife:///app/agent/{kvp.Key.ToString()}/about - {kvp.Value.OnlineStatus}");
                if(OCBSession.Instance.GroupMembers.ContainsKey(kvp.Key)==false)
                    OCBSession.Instance.GroupMembers.Add(kvp.Key, kvp.Value);
            }

            OCBSession.Instance.MemberLookupRE.Set();

        }

        public class ScriptDialogSession
        {
            public UUID ObjectKey = UUID.Zero;
            public string DialogPrompt;
            public List<string> Buttons=new List<string>();
            public string ObjectName;
            public int ReplyChannel;
        }
        private void onScriptDialog(object sender, ScriptDialogEventArgs e)
        {
            if (OCBotMemory.Memory.IgnoreScriptDialogsFrom.Contains(e.OwnerID)) return;
            ScriptDialogSession SDS = new ScriptDialogSession();
            SDS.ObjectKey = e.ObjectID;
            SDS.DialogPrompt = e.Message;
            SDS.Buttons = e.ButtonLabels;
            SDS.ObjectName = e.ObjectName;
            SDS.ReplyChannel = e.Channel;
            string[] Blocks = SDS.ObjectKey.ToString().Split(new[] { '-' });
            int Block2 = Convert.ToInt32("0x"+Blocks[1], 16);
            Block2 -= SDS.ReplyChannel;

            OCBSession.Instance.ScriptSessions.Add(Block2, SDS);
            string BTNStr = "";

            int index = 0;
            foreach(string S in e.ButtonLabels)
            {
                BTNStr += index.ToString()+". "+S + "\n";

                index++;
                
            }

            MHE(Destinations.DEST_AGENT, e.OwnerID, $"Hi! I got this script dialog: \n \nDialogID: {Block2}\nChannel: {SDS.ReplyChannel}\nPrompt: {SDS.DialogPrompt}\nButtons: {BTNStr}\n \n[To respond to this dialog use the !reply_prompt command]");
            MHE(Destinations.DEST_AGENT, e.OwnerID, $"Note: When responding to the dialog, please use the button IDs infront of the labels, not the actual label!\nFor example: !reply_prompt {Block2} 0");
            
        }

        private void On_NewInventoryOffer(object sender, InventoryObjectOfferedEventArgs e)
        {
            MHE(Destinations.DEST_LOCAL, e.Offer.FromAgentID, "Checking notice creation sessions..");
            OCBotMemory ocMem = OCBotMemory.Memory;
            if (ocMem.NoticeSessions.ContainsKey(e.Offer.FromAgentID))
            {
                MHE(Destinations.DEST_LOCAL, e.Offer.FromAgentID, "Checking..");
                OCBotMemory.NoticeCreationSessions nCS = ocMem.NoticeSessions[e.Offer.FromAgentID];
                if (nCS.State == 5)
                {

                    MHE(Destinations.DEST_LOCAL, e.Offer.FromAgentID, "Stand by.. Accepting inventory and moving it into place");
                    AssetType dataType = e.AssetType;
                    UUID AssetID = e.ObjectID;
                    UUID DestinationFolder = grid.Inventory.FindFolderForType(dataType);

                    e.Accept = true;
                    e.FolderID = DestinationFolder;

                    nCS.TemporaryNotice.NoticeAttachment = AssetID;
                    nCS.State++;
                    ocMem.NoticeSessions[e.Offer.FromAgentID] = nCS;
                    ocMem.Save();
                    MHE(Destinations.DEST_LOCAL, e.Offer.FromAgentID, "Here's the details of the built notice, please very it is correct\n \nNotice Summary: " + nCS.TemporaryNotice.NoticeSummary + "\nNotice Description: " + nCS.TemporaryNotice.NoticeDescription + "\nNotice has attachment: " + nCS.TemporaryNotice.HasAttachment.ToString() + "\nNotice Attachment ID: " + nCS.TemporaryNotice.NoticeAttachment.ToString() + "\nRepeats: " + nCS.TemporaryNotice.Repeats.ToString() + "\n \n[To confirm this, say 'confirm']");
                }
            }
            else
            {
                MHE(Destinations.DEST_LOCAL, e.Offer.FromAgentID, "Notice session does not exist!");
                e.Accept = false;
            }

        }

        private void CacheGroupRoles(object sender, GroupRolesDataReplyEventArgs e)
        {
            //MHE(MessageHandler.Destinations.DEST_LOCAL, UUID.Zero, "[debug] role_reply");
            if (!Directory.Exists("GroupCache")) Directory.CreateDirectory("GroupCache"); // this should be purged at every bot restart!!!

            //MHE(MessageHandler.Destinations.DEST_LOCAL, UUID.Zero, "[debug] generating groupcache file");
            OCGroupCaches newCache = new OCGroupCaches();
            OCGroupCaches.GroupMemoryData gmd = new OCGroupCaches.GroupMemoryData();
            foreach (KeyValuePair<UUID, GroupRole> roleData in e.Roles)
            {
                gmd.roleID = roleData.Value.ID;
                gmd.RoleName = roleData.Value.Name;
                gmd.Title = roleData.Value.Title;
                gmd.Powers = roleData.Value.Powers;


                newCache.GMD.Add(gmd);

            }
            newCache.GroupID = e.GroupID;
            newCache.Save(e.GroupID.ToString());
            RoleReply.Set();
            FileInfo fi = new FileInfo("GroupCache/" + e.GroupID.ToString() + ".json");

            //MHE(MessageHandler.Destinations.DEST_LOCAL, UUID.Zero, "[debug] Roles for secondlife:///app/group/" + e.GroupID.ToString() + "/about have been saved to: GroupCache/" + e.GroupID.ToString() + ".bdf\nFileSize: "+fi.Length.ToString(), 55);


        }

        private Dictionary<UUID, Group> GroupsCache = null;
        private ManualResetEvent GroupsEvent = new ManualResetEvent(false);
        private ManualResetEvent RoleReply = new ManualResetEvent(false);
        private void Groups_CurrentGroups(object sender, CurrentGroupsEventArgs e)
        {
            if (null == GroupsCache)
                GroupsCache = e.Groups;
            else
                lock (GroupsCache) { GroupsCache = e.Groups; }
            GroupsEvent.Set();

            foreach (KeyValuePair<UUID, Group> DoCache in GroupsCache)
            {
                bool Retry = true;
                while (Retry)
                {
                    grid.Groups.RequestGroupRoles(DoCache.Value.ID);
                    if (RoleReply.WaitOne(TimeSpan.FromSeconds(30), false)) { Retry = false; }
                    else
                    {
                        MHE(Destinations.DEST_LOCAL, UUID.Zero, "There appears to have been a failure requesting the group roles for secondlife:///app/group/" + DoCache.Value.ID.ToString() + "/about - Trying again");

                    }
                }
            }
        }
        private void ReloadGroupsCache()
        {
            grid.Groups.CurrentGroups += Groups_CurrentGroups;
            grid.Groups.RequestCurrentGroups();
            GroupsEvent.WaitOne(10000, false);
            grid.Groups.CurrentGroups -= Groups_CurrentGroups;
            GroupsEvent.Reset();
        }

        private UUID GroupName2UUID(String groupName)
        {
            UUID tryUUID;
            if (UUID.TryParse(groupName, out tryUUID))
                return tryUUID;
            if (null == GroupsCache)
            {
                ReloadGroupsCache();
                if (null == GroupsCache)
                    return UUID.Zero;
            }
            lock (GroupsCache)
            {
                if (GroupsCache.Count > 0)
                {
                    foreach (Group currentGroup in GroupsCache.Values)
                        if (currentGroup.Name.ToLower() == groupName.ToLower())
                            return currentGroup.ID;
                }
            }
            return UUID.Zero;
        }

        private bool IsGroup(UUID grpKey)
        {
            // For use in IMs since it appears partially broken at the moment
            return GroupsCache.ContainsKey(grpKey);
        }

        public void onIMEvent(object sender, InstantMessageEventArgs e)
        {
            // nothing custom to do here
        }
    }

    public class Brainfuck
    {
        private static readonly int BUFSIZE = 65535;
        private static int[] buf = new int[BUFSIZE];
        private static int ptr { get; set; }

        public static string char2bf(char v)
        {
            string ret = "";
            int ascii = (int)v;
            int factor = ascii / 10;
            int remaining = ascii % 10;
            ret += new string('+', 10);
            ret += "[";
            ret += ">";
            ret += new string('+', factor);
            ret += "<";
            ret += "-";
            ret += "]";
            ret += ">";
            ret += new string('+', remaining);
            ret += ".";
            ret += "[-]";
            return ret;
        }

        public static string str2bf(string theStr)
        {
            string res = "";
            foreach (char t in theStr)
            {
                if (t == ' ') res += char2bf(' ').ToString();
                else
                    res += char2bf(t).ToString();
            }
            return res;
        }

        public static string bytes2bf(byte[] bytes)
        {
            string result = "";
            foreach (byte B in bytes)
            {
                string as_char = B.ToString();
                foreach (char T in as_char)
                {
                    if (T == ' ') result += char2bf(' ').ToString();
                    else if (T == '\n') result += char2bf('\n').ToString();
                    else result += char2bf(T).ToString();
                }
            }

            return result;
        }

        public static string Interpret(string s)
        {
            int i = 0;
            int right = s.Length;
            string returnStr = "";
            while (i < right)
            {
                switch (s[i])
                {
                    case '>':
                        {
                            ptr++;
                            if (ptr >= BUFSIZE)
                            {
                                ptr = 0;
                            }
                            break;
                        }
                    case '<':
                        {
                            ptr--;
                            if (ptr < 0)
                            {
                                ptr = BUFSIZE - 1;
                            }
                            break;
                        }
                    case '.':
                        {
                            returnStr += (((char)buf[ptr]).ToString());
                            break;
                        }
                    case '+':
                        {
                            buf[ptr]++;
                            break;
                        }
                    case '-':
                        {
                            buf[ptr]--;
                            break;
                        }
                    case '[':
                        {
                            if (buf[ptr] == 0)
                            {
                                int loop = 1;
                                while (loop > 0)
                                {
                                    i++;
                                    char c = s[i];
                                    if (c == '[')
                                    {
                                        loop++;
                                    }
                                    else
                                    if (c == ']')
                                    {
                                        loop--;
                                    }
                                }
                            }
                            break;
                        }
                    case ']':
                        {
                            int loop = 1;
                            while (loop > 0)
                            {
                                i--;
                                char c = s[i];
                                if (c == '[')
                                {
                                    loop--;
                                }
                                else
                                if (c == ']')
                                {
                                    loop++;
                                }
                            }
                            i--;
                            break;
                        }
                    case ',':
                        {
                            // read a key
                            //ConsoleKeyInfo key = Console.ReadKey(this.echo);
                            //this.buf[this.ptr] = (int)key.KeyChar;
                            //ch("brainfuck error..");
                            break;
                        }
                }
                i++;
            }

            return returnStr;
        }
    }
    public class Transforms
    {
        public static string TransformString(string untransformedData)
        {
            // _ = SPACE
            return untransformedData.Replace("_", " ");
        }
    }
}
