/*

Copyright © 2019 Tara Piccari (Aria; Tashia Redrose)
Licensed under the GPLv2

*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using OpenMetaverse;
using Bot;
using Bot.CommandSystem;
using System.IO;
using System.Reflection;
using System.Net;
using Microsoft.VisualBasic.CompilerServices;

namespace OpenCollarBot
{
    class CoreCommands : BaseCommands
    {

        [CommandGroup("open_collar_menu", 0, 0, "open_collar_menu - Opens your OpenCollar menu", Destinations.DEST_LOCAL | Destinations.DEST_AGENT)]
        public void GetOCMenu(UUID client, int level, string[] additionalArgs, Destinations source, UUID agentKey, string agentName)
        {
            MHE(source, client, "Requesting OpenCollar Menu");
            BotSession.Instance.grid.Self.Chat(agentName.Substring(0, 2) + "menu", 1, ChatType.Normal);
        }

        [CommandGroup("manual", 0, 0, "docs - Posts the OpenCollar quick guide link", Destinations.DEST_LOCAL | Destinations.DEST_AGENT | Destinations.DEST_GROUP | Destinations.DEST_DISCORD)]
        [CommandGroup("site", 0, 0, "docs - Posts the OpenCollar quick guide link", Destinations.DEST_LOCAL | Destinations.DEST_AGENT | Destinations.DEST_GROUP | Destinations.DEST_DISCORD)]
        [CommandGroup("web", 0, 0, "docs - Posts the OpenCollar quick guide link", Destinations.DEST_LOCAL | Destinations.DEST_AGENT | Destinations.DEST_GROUP | Destinations.DEST_DISCORD)]
        [CommandGroup("doc", 0, 0, "docs - Posts the OpenCollar quick guide link", Destinations.DEST_LOCAL | Destinations.DEST_AGENT | Destinations.DEST_GROUP | Destinations.DEST_DISCORD)]
        [CommandGroup("docs", 0, 0, "docs - Posts the OpenCollar quick guide link", Destinations.DEST_LOCAL | Destinations.DEST_AGENT | Destinations.DEST_GROUP | Destinations.DEST_DISCORD)]
        public void GetOCDocumentation(UUID client, int level, string[] additionalArgs, Destinations source, UUID agentKey, string agentName)
        {
            MHE(source, client, "You can find the OpenCollar Documentation on this website: https://opencollar.cc/docs/Quick-User-Guide");
            MHE(source, client, "The link posted is to the quick guide, you can find other useful docs on that same website!");
        }

        [CommandGroup("cookie", 0, 0, "cookie - Try to get a cookie", Destinations.DEST_LOCAL | Destinations.DEST_AGENT | Destinations.DEST_GROUP | Destinations.DEST_DISCORD)]
        public void GetACookie(UUID client, int level, string[] additionalArgs, Destinations source, UUID agentKey, string agentName)
        {
            if (OCBSession.Instance.CookieList.Contains(agentKey))
            {
                MHE(source, client, "You already got a cookie today don't ask again!");
                return;
            }
            Random rnd = new Random();
            int iRnd = rnd.Next(1, 20);
            if(iRnd < 10)
            {
                MHE(source, client, "Sorry. All out of cookies!");
            }else
            {
                MHE(source, client, "/me gives you 1 chocolate chip cookie");
                OCBSession.Instance.CookieList.Add(agentKey);
                
            }
        }

        [CommandGroup("lm", 0, 0, "lm - Gives you a clickable link to the bot's location", Destinations.DEST_LOCAL | Destinations.DEST_AGENT | Destinations.DEST_GROUP | Destinations.DEST_DISCORD)]
        public void GetLandmark(UUID client, int level, string[] additionalArgs, Destinations source, UUID agentKey, string agentName)
        {
            string Sim = OCBotMemory.Memory.LMSimName;

            
            MHE(source, client, $"Hi! Here's a clickable landmark, when you get here, you can use the viewer to make a landmark at a spot of your choice : http://maps.secondlife.com/secondlife/{Uri.EscapeUriString(Sim)}/{OCBotMemory.Memory.LMPosition.X}/{OCBotMemory.Memory.LMPosition.Y}/{OCBotMemory.Memory.LMPosition.Z}");
        }

        [CommandGroup("set_lm_pos", 5, 3, "set_lm_pos [x] [y] [z] - Sets the in-memory landmark to current sim at the specified position", Destinations.DEST_LOCAL | Destinations.DEST_AGENT | Destinations.DEST_GROUP | Destinations.DEST_DISCORD)]
        public void SetLandmark(UUID client, int level, string[] additionalArgs, Destinations source, UUID agentKey, string agentName)
        {
            MHE(source, client, $"Setting in-memory landmark...");
            OCBotMemory.Memory.LMSimName = BotSession.Instance.grid.Network.CurrentSim.Name;
            OCBotMemory.Memory.LMPosition = new Vector3(float.Parse(additionalArgs[0]), float.Parse(additionalArgs[1]), float.Parse(additionalArgs[2]));
            OCBotMemory.Memory.Save();
        }

        [CommandGroup("ignore_dialogs", 5, 1, "ignore_dialogs [uuid] - Ignores script dialogs from uuid", Destinations.DEST_LOCAL | Destinations.DEST_AGENT | Destinations.DEST_GROUP)]
        public void ignoreMenu(UUID client, int level, string[] additionalArgs,Destinations source, UUID agentKey, string agentName)
        {
            MHE(source, client, "Ignoring");
            if(!OCBotMemory.Memory.IgnoreScriptDialogsFrom.Contains(UUID.Parse(additionalArgs[0])))
                OCBotMemory.Memory.IgnoreScriptDialogsFrom.Add(UUID.Parse(additionalArgs[0]));

            OCBotMemory.Memory.Save();
        }

        [CommandGroup("unignore_dialogs", 5, 1, "unignore_dialogs [uuid] - unIgnores script dialogs from uuid", Destinations.DEST_LOCAL | Destinations.DEST_AGENT | Destinations.DEST_GROUP)]
        public void unignoreMenu(UUID client, int level, string[] additionalArgs, Destinations source, UUID agentKey, string agentName)
        {
            MHE(source, client, "unIgnoring");
            if (OCBotMemory.Memory.IgnoreScriptDialogsFrom.Contains(UUID.Parse(additionalArgs[0])))
                OCBotMemory.Memory.IgnoreScriptDialogsFrom.Remove(UUID.Parse(additionalArgs[0]));

            OCBotMemory.Memory.Save();
        }

        [CommandGroup("allow_tp", 5, 0, "allow_tp", Destinations.DEST_LOCAL | Destinations.DEST_AGENT)]
        public void allow_tp(UUID client, int level, string[] additionalArgs, Destinations source, UUID agentKey, string agentName)
        {
            OCBotMemory.Memory.iHaveBeenTeleported = true;
            MHE(Destinations.DEST_LOCAL, UUID.Zero, "Allowing Manual Teleport");
            OCBotMemory.Memory.Save();
        }

        [CommandGroup("monitor_sim", 4, 1, "monitor_sim [string:SimName] - Monitors misc data about a sim every 5 seconds", Destinations.DEST_LOCAL | Destinations.DEST_AGENT)]
        public void MonitorSim(UUID client, int level, string[] additionalArgs, Destinations source, UUID agentKey, string agentName)
        {
            string par = additionalArgs[0].Replace('_', ' ');
            OCBSession.Instance.MonitoredRegions.Add(  par);
            /*GridRegion rg;
            if(grid.Grid.GetGridRegion(additionalArgs[0], GridLayerType.Objects, out rg))
            {
                
            }*/
            Thread X = new Thread(() =>
            {
                while (OCBSession.Instance.MonitoredRegions.Count != 0)
                {

                    Thread.Sleep(10000);

                    foreach (string region in OCBSession.Instance.MonitoredRegions)
                    {
                        GridRegion rg;
                        if (BotSession.Instance.grid.Grid.GetGridRegion(region, GridLayerType.Objects, out rg))
                        {
                            List<MapItem> MI = BotSession.Instance.grid.Grid.MapItems(rg.RegionHandle, GridItemType.AgentLocations, GridLayerType.Objects, 60000);
                            int actual = 0;
                            foreach(MapItem mp in MI)
                            {
                                MapAgentLocation MAL = (MapAgentLocation)mp;
                                actual += MAL.AvatarCount;
                            }
                            MHE(Destinations.DEST_LOCAL, UUID.Zero, $"[{rg.Name}] Number of avatars: {actual}; Map TextureID: {rg.MapImageID.ToString()}");


                        }
                    }
                }
                MHE(Destinations.DEST_LOCAL, UUID.Zero, "No more monitored regions. Closing watchdog");
                return;
            });

            X.Start();
        }

        [CommandGroup("get_region_texture", 0, 1, "get_region_texture [sim_name] - prints the UUID Sim map texture", Destinations.DEST_LOCAL | Destinations.DEST_AGENT)]
        public void MapTexture(UUID client, int level, string[] additionalArgs, Destinations source, UUID agentKey, string agentName)
        {
            string par = additionalArgs[0].Replace('_', ' ');

            GridRegion rg;
            if (BotSession.Instance.grid.Grid.GetGridRegion(par, GridLayerType.Objects, out rg))
            {
                MHE(source, client, $"Map Texture: {rg.MapImageID.ToString()}");

            }
        }

        [CommandGroup("demonitor_sim", 4, 1, "demonitor_sim [string:SimName] - DeMonitors misc data about a sim every 5 seconds", Destinations.DEST_LOCAL | Destinations.DEST_AGENT)]
        public void DeMonitorSim(UUID client, int level, string[] additionalArgs, Destinations source, UUID agentKey, string agentName)
        {
            string par = additionalArgs[0].Replace('_', ' ');
            if (OCBSession.Instance.MonitoredRegions.Contains(par))
                OCBSession.Instance.MonitoredRegions.Remove(par);
            /*GridRegion rg;
            if(grid.Grid.GetGridRegion(additionalArgs[0], GridLayerType.Objects, out rg))
            {
                
            }*/
        }


        [CommandGroup("set_login_default", 3, 1, "Sets either the region or vector position as default upon login. Argument is a string [region/location]", Destinations.DEST_AGENT | Destinations.DEST_LOCAL)]
        public void SetLoginDefault(UUID client, int level, string[] additionalArgs, Destinations source, UUID agentKey, string agentName)
        {
            OCBotMemory mem = OCBotMemory.Memory;
            if (additionalArgs[0].ToLower() == "region")
                mem.DefaultRegion = BotSession.Instance.grid.Network.CurrentSim.Name;
            else
                mem.DefaultLocation = BotSession.Instance.grid.Self.SimPosition;

            mem.Save();
        }

        [CommandGroup("offer_tp", 0, 0, "Offers you a teleport!", Destinations.DEST_LOCAL | Destinations.DEST_AGENT)]
        public void OfferTP2User(UUID client, int level, string[] additionalArgs, Destinations source, UUID agentKey, string agentName)
        {
            BotSession.Instance.grid.Self.SendTeleportLure(client, "Here's that Teleport you requested!");
        }


        [CommandGroup("resave", 5, 0, "resave - Zerofills OpenCollarBot.BDF, removes invalid values and then saves fresh file", Destinations.DEST_LOCAL | Destinations.DEST_AGENT)]
        public void resave(UUID client, int level, string[] additionalArgs, Destinations source, UUID agentKey, string agentName)
        {
            OCBotMemory ocb = OCBotMemory.Memory;
            if (File.Exists("OpenCollarBot.json")) File.Delete("OpenCollarBot.json");
            ocb.Save();
        }

        [CommandGroup("sit", 4, 1, "Sits the bot on a object now and at login [#UUID/unsit]", Destinations.DEST_AGENT | Destinations.DEST_LOCAL)]
        public void PerformSitCommand(UUID client, int level, string[] additionalArgs, Destinations source, UUID agentKey, string agentName)
        {
            OCBotMemory bmem = OCBotMemory.Memory;

            if (additionalArgs[0] == "unsit")
            {
                BotSession.Instance.grid.Self.Stand();
                bmem.sit_cube = UUID.Zero;
                bmem.Save();
            }
            else
            {
                UUID obj = UUID.Zero;
                try
                {
                    obj = UUID.Parse(additionalArgs[0]);
                    BotSession.Instance.grid.Self.RequestSit(obj, Vector3.Zero);
                    bmem.sit_cube = obj;
                    bmem.Save();
                }
                catch (Exception e)
                {
                    MHE(source, client, "Failed to sit! UUID invalid");
                }


            }
        }

        [CommandGroup("invite", 0, 1, "invite [uuid_user] - Sends a group invite! (Note: This variant is only able to be used from within a group. See !invite for requesting invite anywhere)", Destinations.DEST_GROUP)]
        public void InviteToGroupByChat(UUID client, int level, string[] additionalArgs, Destinations source, UUID agentKey, string agentName)
        {
            OCBotMemory ocb = OCBotMemory.Memory;
            if (DateTime.Now < ocb.InviteLastSent)
            {
                MHE(source, client, "There is a 2 minute cooldown between sending group invites.");
            }

            ocb.InviteLastSent = DateTime.Now.AddMinutes(2);
            ocb.Save();
            UUID groupID = client;
            UUID sendTo = UUID.Zero;
            if (additionalArgs.Length == 1) sendTo = UUID.Parse(additionalArgs[0]);

            if (sendTo == UUID.Zero) return;
            else
            {
                List<UUID> role = new List<UUID>();
                role.Add(UUID.Zero);
                BotSession.Instance.grid.Groups.Invite(groupID, role, sendTo);

                MHE(source, client, "Sent the invite");
            }
        }


        [CommandGroup("!invite", 4, 2, "!invite [uuid_group] [uuid_person] - Offers a group invite to a person", Destinations.DEST_GROUP | Destinations.DEST_AGENT | Destinations.DEST_LOCAL)]
        public void InviteToGroup(UUID client, int level, string[] additionalArgs, Destinations source, UUID agentKey, string agentName)
        {
            OCGroupCaches gc = new OCGroupCaches();
            UUID groupID = UUID.Parse(additionalArgs[0]);
            UUID user = UUID.Parse(additionalArgs[1]);

            
            if (!File.Exists("GroupCache/" + additionalArgs[0] + ".json"))
            {
                
                BotSession.Instance.grid.Groups.RequestGroupRoles(groupID);
            }
            List<UUID> roles = new List<UUID>();
            roles.Add(UUID.Zero);
            BotSession.Instance.grid.Groups.Invite(groupID, roles, user);


        }

        [CommandGroup("set_group", 4, 1, "set_group [uuid] - Sets the active group title", Destinations.DEST_AGENT | Destinations.DEST_LOCAL)]
        public void SetActiveGroup(UUID client, int level, string[] additionalArgs, Destinations source, UUID agentKey, string agentName)
        {
            UUID groupKey = UUID.Parse(additionalArgs[0]);
            BotSession.Instance.grid.Groups.ActivateGroup(groupKey);

            OCBotMemory bmem = OCBotMemory.Memory;
            bmem.GroupKey = groupKey;
            bmem.Save();
        }



        [CommandGroup("clear_queue", 3, 0, "clear_queue - Full Queue Reset", Destinations.DEST_AGENT | Destinations.DEST_LOCAL | Destinations.DEST_GROUP )]
        public void ClearQueue(UUID client, int level, string[] additionalArgs, Destinations source, UUID agentKey, string agentName)
        {
            BotSession.Instance.MSGSVC.QUEUE.Clear();
            MHE(source, client, "Acknowledged! Cleared the queue!");
        }




        [CommandGroup("list_asm", 1, 0, "list_asm - Lists all loaded assembly names", Destinations.DEST_LOCAL | Destinations.DEST_GROUP | Destinations.DEST_AGENT)]
        public void list_asm(UUID client, int level, string[] additionalArgs,
                                Destinations source,
                                UUID agentKey, string agentName)
        {
            foreach (Assembly A in AppDomain.CurrentDomain.GetAssemblies())
            {
                MHE(source, client, "Assembly: " + A.GetName().FullName);
            }
        }



        [CommandGroup("output_bf", 3, 1, "output_bf [Fully dump filename to brainfuck] - Dumps a file as brainfuck", Destinations.DEST_AGENT | Destinations.DEST_LOCAL)]
        public void dumpBF(UUID client, int level, string[] additionalArgs, Destinations source, UUID agentKey, string agentName)
        {
            MHE(source, client, "Dumping " + additionalArgs[0]);
            if (File.Exists(additionalArgs[0]))
            {

                string filecontents = File.ReadAllText(additionalArgs[0]);
                string BrainfuckData = Brainfuck.str2bf(filecontents);

                if (File.Exists(additionalArgs[0] + "_brainfuck.txt")) File.Delete(additionalArgs[0] + "_brainfuck.txt");
                Stream X = new FileStream(additionalArgs[0] + "_brainfuck.txt", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                StreamWriter XX = new StreamWriter(X);
                XX.Write(BrainfuckData);
                XX.Close();
                X.Close();

                MHE(source, client, "Brainfuck output for " + additionalArgs[0] + " has been generated");
                FileInfo fo = new FileInfo(additionalArgs[0]);
                FileInfo fb = new FileInfo(additionalArgs[0] + "_brainfuck.txt");
                MHE(source, client, "Filesize of original [" + fo.Length.ToString() + "] - brainfucked [" + fb.Length.ToString() + "]");
            }
            else
            {
                MHE(source, client, "The file must exist");
            }
        }

        ManualResetEvent mre = new ManualResetEvent(false);
        List<DirectoryManager.AgentSearchData> peopleSearchResults = new List<DirectoryManager.AgentSearchData>();

        [CommandGroup("search", 0, 1, "search [search_term] - Search this term on SL People Search", Destinations.DEST_LOCAL | Destinations.DEST_GROUP | Destinations.DEST_AGENT)]
        public void sl_search(UUID client, int level, string[] additionalArgs,
                                Destinations source,
                                UUID agentKey, string agentName)
        {
            OCBotMemory ocb = OCBotMemory.Memory;
            int queued = 0;
            int queueRequest = 0;
            while (peopleSearchResults.Count == 0 || peopleSearchResults.Count >= 29)
            {

                peopleSearchResults = new List<DirectoryManager.AgentSearchData>();
                BotSession.Instance.grid.Directory.StartPeopleSearch(additionalArgs[0], queueRequest);
                mre.Reset();
                BotSession.Instance.grid.Directory.DirPeopleReply += name_search;
                if (mre.WaitOne(TimeSpan.FromSeconds(30)))
                {
                    // output the search results

                    //MHE(source, client, "Okay! I got a reply from search results! [" + peopleSearchResults.Count.ToString() + "]");
                    BotSession.Instance.grid.Directory.DirPeopleReply -= name_search;

                    foreach (DirectoryManager.AgentSearchData asd in peopleSearchResults)
                    {
                        MHE(source, client, "Result: secondlife:///app/agent/" + asd.AgentID.ToString() + "/about [" + asd.AgentID.ToString() + "] [online? " + asd.Online.ToString() + "]");
                        queued++;
                    }
                }
                else
                {
                    MHE(source, client, "Failed to get results in time");
                    BotSession.Instance.grid.Directory.DirPeopleReply -= name_search;
                    return;
                }
                queueRequest++;

                if (queued > 300)
                {
                    BotSession.Instance.MSGSVC.QUEUE.Clear();
                    peopleSearchResults = new List<DirectoryManager.AgentSearchData>();
                    MHE(source, client, "Search has been canceled. There were more than 100 results!");

                    BotSession.Instance.grid.Directory.DirPeopleReply -= name_search;
                    return;
                }
            }

            peopleSearchResults = new List<DirectoryManager.AgentSearchData>();
        }


        [CommandGroup("search_exact", 0, 2, "search [first] [last] - Find specific avatar", Destinations.DEST_LOCAL | Destinations.DEST_GROUP | Destinations.DEST_AGENT)]
        public void sl_search_exact(UUID client, int level, string[] additionalArgs,
                                Destinations source,
                                UUID agentKey, string agentName)
        {
            OCBotMemory ocb = OCBotMemory.Memory;

            BotSession.Instance.grid.Directory.StartPeopleSearch(additionalArgs[0] + " " + additionalArgs[1], 0);


            mre.Reset();
            BotSession.Instance.grid.Directory.DirPeopleReply += name_search;
            if (mre.WaitOne(TimeSpan.FromSeconds(30)))
            {
                // output the search results

                MHE(source, client, "Okay! I got a reply from search results!");
                BotSession.Instance.grid.Directory.DirPeopleReply -= name_search;

                foreach (DirectoryManager.AgentSearchData asd in peopleSearchResults)
                {

                    MHE(source, client, "Result: secondlife:///app/agent/" + asd.AgentID.ToString() + "/about [" + asd.AgentID.ToString() + "] [online? " + asd.Online.ToString() + "]");

                }
            }
            else
            {
                MHE(source, client, "Failed to get results in time");
                BotSession.Instance.grid.Directory.DirPeopleReply -= name_search;
            }



            peopleSearchResults = new List<DirectoryManager.AgentSearchData>();
        }
        private void name_search(object sender, DirPeopleReplyEventArgs e)
        {
            peopleSearchResults = e.MatchedPeople;
            mre.Set();

        }


        ManualResetEvent k2n = new ManualResetEvent(false);
        Dictionary<UUID, string> DiscoveredNames = new Dictionary<UUID, string>();

        [CommandGroup("key2name", 0, 1, "key2name [uuid] - Transforms a UUID to username", Destinations.DEST_LOCAL | Destinations.DEST_GROUP | Destinations.DEST_AGENT)]
        public void sl_key2name(UUID client, int level, string[] additionalArgs,
                                Destinations source,
                                UUID agentKey, string agentName)
        {
            OCBotMemory ocb = OCBotMemory.Memory;
            k2n.Reset();
            BotSession.Instance.grid.Avatars.UUIDNameReply += key2name_reply;
            BotSession.Instance.grid.Avatars.RequestAvatarName(UUID.Parse(additionalArgs[0]));

            if (k2n.WaitOne(TimeSpan.FromSeconds(30)))
            {
                BotSession.Instance.grid.Avatars.UUIDNameReply -= key2name_reply;
                MHE(source, client, "UUID [secondlife:///app/agent/" + additionalArgs[0] + "/about " + additionalArgs[0] + "] is " + DiscoveredNames[UUID.Parse(additionalArgs[0])]);
            }
            else
            {
                MHE(source, client, "Failed to lookup that user! Do they exist?");
                BotSession.Instance.grid.Avatars.UUIDNameReply -= key2name_reply;
            }

            DiscoveredNames = new Dictionary<UUID, string>();
        }

        private void key2name_reply(object sender, UUIDNameReplyEventArgs e)
        {
            DiscoveredNames = e.Names;
            k2n.Set();
        }




        [CommandGroup("pwdgen", 0, 5, "pwdgen [Length] [y/n:SpecialChars] [y/n:Numbers] [y/n:MixCase] [NUMBER:randomizerSeed] - Generates a password", Destinations.DEST_AGENT | Destinations.DEST_LOCAL)]
        public void PWDGen(UUID client, int level, string[] additionalArgs,
                                Destinations source,
                                UUID agentKey, string agentName)
        {
            MHE(source, agentKey, "Processing request");
            string UPPER = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string LOWER = UPPER.ToLower();
            string NUM = "0123456789";
            string SPCHAR = "~!@#$%^&*()_-+={[}]|:;<,>.?/";

            int seedPos = 0;
            string seed = additionalArgs[4];
            int Aseed = Convert.ToInt32(seed);
            if (Aseed == 0) Aseed = 1;
            // Begin
            // Initiate data computations
            bool found = false;

            bool SpChars = false;
            bool Numbers = false;
            bool MixedCase = false;
            if (additionalArgs[1] == "y") SpChars = true;
            if (additionalArgs[2] == "y") Numbers = true;
            if (additionalArgs[3] == "y") MixedCase = true;


            List<string> outputLst = new List<string>();


            int Len = Convert.ToInt32(additionalArgs[0]);
            Random rnd = new Random(Convert.ToInt32(Tools.getTimestamp()));
            MHE(source, agentKey, "Assembling password!");
            while (!found)
            {
                // Check stuff

                if (outputLst.Count != Len)
                {

                    int seg = rnd.Next(5);
                    if (seg == 1 && SpChars)
                    {
                        int specialCharList = SPCHAR.Length;
                        // grab a random character
                        int randomChar = rnd.Next(0, specialCharList);
                        outputLst.Add(SPCHAR[randomChar].ToString());
                    }
                    else if (seg == 2 && Numbers)
                    {
                        int numList = NUM.Length;
                        int rndChar = rnd.Next(0, numList);
                        outputLst.Add(NUM[rndChar].ToString());
                    }
                    else if (seg == 3)
                    {
                        int charList = UPPER.Length;
                        int decide = rnd.Next(3);
                        int rndChar = rnd.Next(0, charList);
                        if (decide == 0) outputLst.Add(LOWER[rndChar].ToString());
                        else if (decide == 1) if (MixedCase) outputLst.Add(UPPER[rndChar].ToString());
                    }
                }
                else
                {
                    // Begin to shuffle !!
                    if (Aseed == 0) found = true;
                    else
                    {
                        // Shuffle the calculated password (seed)times
                        List<string> tmp = new List<string>();
                        foreach (string S in outputLst.OrderBy(x => rnd.Next(rnd.Next(Aseed * outputLst.Count)) + Aseed * outputLst.Count))
                        {
                            tmp.Add(S);
                        }

                        Aseed--;
                        outputLst = tmp;
                        tmp = new List<string>();

                    }
                }

            }
            string outputFinal = "";
            foreach (string V in outputLst) { outputFinal += V; }
            MHE(source, agentKey, "Hi! I was able to generate this, I hope you like it!\n \n" + outputFinal);

        }


        [CommandGroup("set_staffgroup", 5, 1, "set_staffgroup [uuid] - Sets the staff group ID for where to send the kick notices", Destinations.DEST_LOCAL | Destinations.DEST_GROUP | Destinations.DEST_AGENT)]
        public void set_staffgroup(UUID client, int level, string[] additionalArgs,
                                Destinations source,
                                UUID agentKey, string agentName)
        {
            OCBotMemory ocb = OCBotMemory.Memory;
            ocb.StaffGroup = UUID.Parse(additionalArgs[0]);
            ocb.Save();
            MHE(source, client, "Staff Group has been set to: " + ocb.StaffGroup.ToString() + "\n \n[I will send kick notices to this group automatically]");
        }


        [CommandGroup("auto_buildnotice", 5, 3, "auto_buildnotice [kicked_username_b64:string] [rawkickmsgb64:string] [kickedByb64:string] - This command is reserved for automated scripts. Builds a notice and sends it to staff group if staffgroup is set", Destinations.DEST_LOCAL | Destinations.DEST_GROUP | Destinations.DEST_AGENT)]
        public void auto_buildnotice(UUID client, int level, string[] additionalArgs,
                                Destinations source,
                                UUID agentKey, string agentName)
        {
            // Check staff group is set
            OCBotMemory ocb = OCBotMemory.Memory;
            if (ocb.StaffGroup != UUID.Zero)
            {
                // Build notice & send
                OCBotMemory.Notices new_notice = new OCBotMemory.Notices();
                new_notice.GroupKey = ocb.StaffGroup;
                new_notice.HasAttachment = false;
                new_notice.InternalName = "kicknotice";
                new_notice.LastSent = DateTime.MinValue;
                new_notice.NoticeAttachment = UUID.Zero;
                string kicked = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(additionalArgs[0]));
                string raw_msg = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(additionalArgs[1]));
                string kicked_by = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(additionalArgs[2]));
                new_notice.NoticeSummary = "Automatic Kick Notification";
                new_notice.Repeats = false;
                new_notice.NoticeDescription = raw_msg;

                ocb.NoticeLists.Add("kicknotice", new_notice);
                ocb.Save();
            }
        }


    }
}
