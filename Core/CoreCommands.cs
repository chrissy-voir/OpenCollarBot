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


namespace OpenCollarBot
{
    class CoreCommands
    {
        [CommandGroup("terminate_bot", 5, 0, "", MessageHandler.Destinations.DEST_LOCAL | MessageHandler.Destinations.DEST_AGENT)]
        public void PerformExit(UUID client, int level, GridClient grid, string[] additionalArgs, SysOut log, MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source, CommandRegistry registry, UUID agentKey, string agentName)
        {
            MHE(source, client, "Bot exit initiated.");
            MHE(MessageHandler.Destinations.DEST_ACTION, UUID.Zero, "{'type':'exit'}");
        }
        // !!help
        [CommandGroup("!help", 0, 0, "Prints the entire help registry", MessageHandler.Destinations.DEST_AGENT | MessageHandler.Destinations.DEST_LOCAL | MessageHandler.Destinations.DEST_GROUP)]
        [CommandGroup("bot.help", 0, 0, "Alias to !help", MessageHandler.Destinations.DEST_AGENT | MessageHandler.Destinations.DEST_LOCAL | MessageHandler.Destinations.DEST_GROUP)]
        public void PrintAllHelp(UUID client, int level, GridClient grid, string[] additionalArgs, SysOut log, MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source, CommandRegistry registry, UUID agentKey, string agentName)
        {
            MHE(source, client, "All commands viewable at: https://zontreck.dev:35591/help");
        }
        // !help "command"
        [CommandGroup("help", 0, 1, "Prints help for one command", MessageHandler.Destinations.DEST_AGENT | MessageHandler.Destinations.DEST_LOCAL | MessageHandler.Destinations.DEST_GROUP)]
        public void PrintHelp(UUID client, int level, GridClient grid, string[] additionalArgs, SysOut log, MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source, CommandRegistry registry, UUID agentKey, string agentName)
        {
            registry.PrintHelp(source, additionalArgs[0], client);
        }

        [CommandGroup("set_login_default", 3, 1, "Sets either the region or vector position as default upon login. Argument is a string [region/location]", MessageHandler.Destinations.DEST_AGENT | MessageHandler.Destinations.DEST_LOCAL)]
        public void SetLoginDefault(UUID client, int level, GridClient grid, string[] additionalArgs, SysOut log, MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source, CommandRegistry registry, UUID agentKey, string agentName)
        {
            OCBotMemory mem = OCBotMemory.Memory;
            if (additionalArgs[0].ToLower() == "region")
                mem.DefaultRegion = grid.Network.CurrentSim.Name;
            else
                mem.DefaultLocation = grid.Self.SimPosition;

            mem.Save();
        }

        [CommandGroup("offer_tp", 0, 0, "Offers you a teleport!", MessageHandler.Destinations.DEST_LOCAL | MessageHandler.Destinations.DEST_AGENT)]
        public void OfferTP2User(UUID client, int level, GridClient grid, string[] additionalArgs, SysOut log, MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source, CommandRegistry registry, UUID agentKey, string agentName)
        {
            grid.Self.SendTeleportLure(client, "Here's that Teleport you requested!");
        }


        [CommandGroup("resave", 5, 0, "resave - Zerofills OpenCollarBot.BDF, removes invalid values and then saves fresh file", MessageHandler.Destinations.DEST_LOCAL | MessageHandler.Destinations.DEST_AGENT)]
        public void resave(UUID client, int level, GridClient grid, string[] additionalArgs, SysOut log, MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source, CommandRegistry registry, UUID agentKey, string agentName)
        {
            OCBotMemory ocb = OCBotMemory.Memory;
            if (File.Exists("OpenCollarBot.json")) File.Delete("OpenCollarBot.json");
            ocb.Save();
        }

        [CommandGroup("sit", 4, 1, "Sits the bot on a object now and at login [#UUID/unsit]", MessageHandler.Destinations.DEST_AGENT | MessageHandler.Destinations.DEST_LOCAL)]
        public void PerformSitCommand(UUID client, int level, GridClient grid, string[] additionalArgs, SysOut log, MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source, CommandRegistry registry, UUID agentKey, string agentName)
        {
            OCBotMemory bmem = OCBotMemory.Memory;

            if (additionalArgs[0] == "unsit")
            {
                grid.Self.Stand();
                bmem.sit_cube = UUID.Zero;
                bmem.Save();
            }
            else
            {
                UUID obj = UUID.Zero;
                try
                {
                    obj = UUID.Parse(additionalArgs[0]);
                    grid.Self.RequestSit(obj, Vector3.Zero);
                    bmem.sit_cube = obj;
                    bmem.Save();
                }
                catch (Exception e)
                {
                    MHE(source, client, "Failed to sit! UUID invalid");
                }


            }
        }

        [CommandGroup("invite", 0, 1, "invite [uuid_user] - Sends a group invite! (Note: This variant is only able to be used from within a group. See !invite for requesting invite anywhere)", MessageHandler.Destinations.DEST_GROUP)]
        public void InviteToGroupByChat(UUID client, int level, GridClient grid, string[] additionalArgs, SysOut log, MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source, CommandRegistry registry, UUID agentKey, string agentName)
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
                grid.Groups.Invite(groupID, role, sendTo);

                MHE(source, client, "Sent the invite");
            }
        }


        [CommandGroup("!invite", 4, 2, "!invite [uuid_group] [uuid_person] - Offers a group invite to a person", MessageHandler.Destinations.DEST_GROUP | MessageHandler.Destinations.DEST_AGENT | MessageHandler.Destinations.DEST_LOCAL)]
        public void InviteToGroup(UUID client, int level, GridClient grid, string[] additionalArgs, SysOut log, MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source, CommandRegistry registry, UUID agentKey, string agentName)
        {
            MHE(source, client, "Stand by..");
            OCGroupCaches gc = new OCGroupCaches();
            UUID groupID = UUID.Parse(additionalArgs[0]);
            UUID user = UUID.Parse(additionalArgs[1]);

            if (!File.Exists("GroupCache/" + additionalArgs[0] + ".bdf"))
            {

                MHE(source, client, "Error: Caches for that group ID do not exist. Resetting cache download");

                grid.Groups.RequestGroupRoles(groupID);
            }
            List<UUID> roles = new List<UUID>();
            roles.Add(UUID.Zero);
            grid.Groups.Invite(groupID, roles, user);

            MHE(source, client, "Invitation sent!");

        }

        [CommandGroup("set_group", 4, 1, "set_group [uuid] - Sets the active group title", MessageHandler.Destinations.DEST_AGENT | MessageHandler.Destinations.DEST_LOCAL)]
        public void SetActiveGroup(UUID client, int level, GridClient grid, string[] additionalArgs, SysOut log, MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source, CommandRegistry registry, UUID agentKey, string agentName)
        {
            UUID groupKey = UUID.Parse(additionalArgs[0]);
            grid.Groups.ActivateGroup(groupKey);

            OCBotMemory bmem = OCBotMemory.Memory;
            bmem.GroupKey = groupKey;
            bmem.Save();
        }


        [CommandGroup("assign", 75, 1, "assign [DLL Name] - Sets the active DLL", MessageHandler.Destinations.DEST_AGENT | MessageHandler.Destinations.DEST_LOCAL)]
        public void SetActiveProgram(UUID client, int level, GridClient grid, string[] additionalArgs, SysOut log, MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source, CommandRegistry registry, UUID agentKey, string agentName)
        {
            MHE(MessageHandler.Destinations.DEST_ACTION, UUID.Zero, "{\"type\":\"assignProgram\",\"newProgram\":\"" + additionalArgs[0] + "\"}");
        }


        [CommandGroup("clear_queue", 3, 0, "clear_queue - Full Queue Reset", MessageHandler.Destinations.DEST_AGENT | MessageHandler.Destinations.DEST_LOCAL | MessageHandler.Destinations.DEST_GROUP )]
        public void ClearQueue(UUID client, int level, GridClient grid, string[] additionalArgs, SysOut log, MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source, CommandRegistry registry, UUID agentKey, string agentName)
        {
            MHE(MessageHandler.Destinations.DEST_ACTION, UUID.Zero, "RESET_QUEUE");
            MHE(source, client, "Acknowledged! Cleared the queue!");
        }

        [CommandGroup("load_dll", 5, 1, "load_dll [DLL_Name] - Loads a DLL and searches for entry points", MessageHandler.Destinations.DEST_LOCAL | MessageHandler.Destinations.DEST_AGENT)]
        public void load_DLL(UUID client, int level, GridClient grid, string[] additionalArgs, SysOut log, MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source, CommandRegistry registry, UUID agentKey, string agentName)
        {
            // Load DLL
            OCBotMemory ocb = OCBotMemory.Memory;

            ocb.LinkedDLLs.Add(additionalArgs[0]);
            Dictionary<string, string> cmd = new Dictionary<string, string>();
            cmd.Add("type", "load_program");
            cmd.Add("newProgram", additionalArgs[0]);
            string strCmd = JsonConvert.SerializeObject(cmd);
            MHE(MessageHandler.Destinations.DEST_ACTION, UUID.Zero, strCmd);

            ocb.Save();
        }


        [CommandGroup("unload_dll", 5, 1, "unload_dll [DLL_Name] - Prevents DLL from reloading at next reboot", MessageHandler.Destinations.DEST_LOCAL | MessageHandler.Destinations.DEST_AGENT)]
        public void unload_DLL(UUID client, int level, GridClient grid, string[] additionalArgs, SysOut log, MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source, CommandRegistry registry, UUID agentKey, string agentName)
        {
            // Load DLL
            OCBotMemory ocb = OCBotMemory.Memory;

            ocb.LinkedDLLs.Remove(additionalArgs[0]);

            MHE(MessageHandler.Destinations.DEST_LOCAL, UUID.Zero, "DLL marked for unload at next bot restart");

            ocb.Save();
        }



        [CommandGroup("list_asm", 1, 0, "list_asm - Lists all loaded assembly names", MessageHandler.Destinations.DEST_LOCAL | MessageHandler.Destinations.DEST_GROUP | MessageHandler.Destinations.DEST_AGENT)]
        public void list_asm(UUID client, int level, GridClient grid, string[] additionalArgs,
                                SysOut log, MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source,
                                CommandRegistry registry, UUID agentKey, string agentName)
        {
            foreach (Assembly A in AppDomain.CurrentDomain.GetAssemblies())
            {
                MHE(source, client, "Assembly: " + A.GetName().FullName);
            }
        }



        [CommandGroup("output_bf", 3, 1, "output_bf [Fully dump filename to brainfuck] - Dumps a file as brainfuck", MessageHandler.Destinations.DEST_AGENT | MessageHandler.Destinations.DEST_LOCAL)]
        public void dumpBF(UUID client, int level, GridClient grid, string[] additionalArgs, SysOut log, MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source, CommandRegistry registry, UUID agentKey, string agentName)
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

        [CommandGroup("search", 0, 1, "search [search_term] - Search this term on SL People Search", MessageHandler.Destinations.DEST_LOCAL | MessageHandler.Destinations.DEST_GROUP | MessageHandler.Destinations.DEST_AGENT)]
        public void sl_search(UUID client, int level, GridClient grid, string[] additionalArgs,
                                SysOut log, MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source,
                                CommandRegistry registry, UUID agentKey, string agentName)
        {
            OCBotMemory ocb = OCBotMemory.Memory;
            int queued = 0;
            int queueRequest = 0;
            while (peopleSearchResults.Count == 0 || peopleSearchResults.Count >= 29)
            {

                peopleSearchResults = new List<DirectoryManager.AgentSearchData>();
                grid.Directory.StartPeopleSearch(additionalArgs[0], queueRequest);
                mre.Reset();
                grid.Directory.DirPeopleReply += name_search;
                if (mre.WaitOne(TimeSpan.FromSeconds(30)))
                {
                    // output the search results

                    //MHE(source, client, "Okay! I got a reply from search results! [" + peopleSearchResults.Count.ToString() + "]");
                    grid.Directory.DirPeopleReply -= name_search;

                    foreach (DirectoryManager.AgentSearchData asd in peopleSearchResults)
                    {
                        MHE(source, client, "Result: secondlife:///app/agent/" + asd.AgentID.ToString() + "/about [" + asd.AgentID.ToString() + "] [online? " + asd.Online.ToString() + "]");
                        queued++;
                    }
                }
                else
                {
                    MHE(source, client, "Failed to get results in time");
                    grid.Directory.DirPeopleReply -= name_search;
                    return;
                }
                queueRequest++;

                if (queued > 300)
                {
                    MHE(MessageHandler.Destinations.DEST_ACTION, UUID.Zero, "RESET_QUEUE");
                    peopleSearchResults = new List<DirectoryManager.AgentSearchData>();
                    MHE(source, client, "Search has been canceled. There were more than 100 results!");

                    grid.Directory.DirPeopleReply -= name_search;
                    return;
                }
            }

            peopleSearchResults = new List<DirectoryManager.AgentSearchData>();
        }


        [CommandGroup("search_exact", 0, 2, "search [first] [last] - Find specific avatar", MessageHandler.Destinations.DEST_LOCAL | MessageHandler.Destinations.DEST_GROUP | MessageHandler.Destinations.DEST_AGENT)]
        public void sl_search_exact(UUID client, int level, GridClient grid, string[] additionalArgs,
                                SysOut log, MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source,
                                CommandRegistry registry, UUID agentKey, string agentName)
        {
            OCBotMemory ocb = OCBotMemory.Memory;

            grid.Directory.StartPeopleSearch(additionalArgs[0] + " " + additionalArgs[1], 0);


            mre.Reset();
            grid.Directory.DirPeopleReply += name_search;
            if (mre.WaitOne(TimeSpan.FromSeconds(30)))
            {
                // output the search results

                MHE(source, client, "Okay! I got a reply from search results!");
                grid.Directory.DirPeopleReply -= name_search;

                foreach (DirectoryManager.AgentSearchData asd in peopleSearchResults)
                {

                    MHE(source, client, "Result: secondlife:///app/agent/" + asd.AgentID.ToString() + "/about [" + asd.AgentID.ToString() + "] [online? " + asd.Online.ToString() + "]");

                }
            }
            else
            {
                MHE(source, client, "Failed to get results in time");
                grid.Directory.DirPeopleReply -= name_search;
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

        [CommandGroup("key2name", 0, 1, "key2name [uuid] - Transforms a UUID to username", MessageHandler.Destinations.DEST_LOCAL | MessageHandler.Destinations.DEST_GROUP | MessageHandler.Destinations.DEST_AGENT)]
        public void sl_key2name(UUID client, int level, GridClient grid, string[] additionalArgs,
                                SysOut log, MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source,
                                CommandRegistry registry, UUID agentKey, string agentName)
        {
            OCBotMemory ocb = OCBotMemory.Memory;
            k2n.Reset();
            grid.Avatars.UUIDNameReply += key2name_reply;
            grid.Avatars.RequestAvatarName(UUID.Parse(additionalArgs[0]));

            if (k2n.WaitOne(TimeSpan.FromSeconds(30)))
            {
                grid.Avatars.UUIDNameReply -= key2name_reply;
                MHE(source, client, "UUID [secondlife:///app/agent/" + additionalArgs[0] + "/about " + additionalArgs[0] + "] is " + DiscoveredNames[UUID.Parse(additionalArgs[0])]);
            }
            else
            {
                MHE(source, client, "Failed to lookup that user! Do they exist?");
                grid.Avatars.UUIDNameReply -= key2name_reply;
            }

            DiscoveredNames = new Dictionary<UUID, string>();
        }

        private void key2name_reply(object sender, UUIDNameReplyEventArgs e)
        {
            DiscoveredNames = e.Names;
            k2n.Set();
        }




        [CommandGroup("pwdgen", 0, 5, "pwdgen [Length] [y/n:SpecialChars] [y/n:Numbers] [y/n:MixCase] [NUMBER:randomizerSeed] - Generates a password", MessageHandler.Destinations.DEST_AGENT | MessageHandler.Destinations.DEST_LOCAL)]
        public void PWDGen(UUID client, int level, GridClient grid, string[] additionalArgs,
                                SysOut log, MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source,
                                CommandRegistry registry, UUID agentKey, string agentName)
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


        [CommandGroup("getident", 0, 0, "getident - Prints the bot's unique ID", MessageHandler.Destinations.DEST_AGENT | MessageHandler.Destinations.DEST_GROUP | MessageHandler.Destinations.DEST_LOCAL)]
        [CommandGroup("test_default_lic", 5, 0, "test_default_lic - Generates the default license key for this sytem", MessageHandler.Destinations.DEST_LOCAL | MessageHandler.Destinations.DEST_GROUP | MessageHandler.Destinations.DEST_AGENT)]
        public void test_default_lic(UUID client, int level, GridClient grid, string[] additionalArgs,
                                SysOut log, MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source,
                                CommandRegistry registry, UUID agentKey, string agentName)
        {
            //License LIC = new License();
            //LIC.NewKey();

            //LIC.InitUniqueMachine();

            //MHE(source, client, "Ident: " + LIC.Key);
        }


        [CommandGroup("test_lic", 5, 2, "test_lic [LICCode] [existingCode] - Tests license gen", MessageHandler.Destinations.DEST_LOCAL | MessageHandler.Destinations.DEST_GROUP | MessageHandler.Destinations.DEST_AGENT)]
        public void test_lic(UUID client, int level, GridClient grid, string[] additionalArgs,
                                SysOut log, MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source,
                                CommandRegistry registry, UUID agentKey, string agentName)
        {
            //License LIC = new License();
            //LIC.NewKey();
            //LIC.SetKey(additionalArgs[1]);

            //LIC.Key = additionalArgs[0];
            //MHE(source, client, "Final key: " + LIC.Key);
        }


        [CommandGroup("default_lic", 5, 0, "default_lic - Defaults license gen", MessageHandler.Destinations.DEST_LOCAL | MessageHandler.Destinations.DEST_GROUP | MessageHandler.Destinations.DEST_AGENT)]
        public void default_lic(UUID client, int level, GridClient grid, string[] additionalArgs,
                                SysOut log, MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source,
                                CommandRegistry registry, UUID agentKey, string agentName)
        {
            //License LIC = new License();
            //LIC.NewKey();
            //MHE(source, client, "Final key: " + LIC.Key);
        }


        [CommandGroup("set_staffgroup", 5, 1, "set_staffgroup [uuid] - Sets the staff group ID for where to send the kick notices", MessageHandler.Destinations.DEST_LOCAL | MessageHandler.Destinations.DEST_GROUP | MessageHandler.Destinations.DEST_AGENT)]
        public void set_staffgroup(UUID client, int level, GridClient grid, string[] additionalArgs,
                                SysOut log, MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source,
                                CommandRegistry registry, UUID agentKey, string agentName)
        {
            OCBotMemory ocb = OCBotMemory.Memory;
            ocb.StaffGroup = UUID.Parse(additionalArgs[0]);
            ocb.Save();
            MHE(source, client, "Staff Group has been set to: " + ocb.StaffGroup.ToString() + "\n \n[I will send kick notices to this group automatically]");
        }


        [CommandGroup("auto_buildnotice", 5, 3, "auto_buildnotice [kicked_username_b64:string] [rawkickmsgb64:string] [kickedByb64:string] - This command is reserved for automated scripts. Builds a notice and sends it to staff group if staffgroup is set", MessageHandler.Destinations.DEST_LOCAL | MessageHandler.Destinations.DEST_GROUP | MessageHandler.Destinations.DEST_AGENT)]
        public void auto_buildnotice(UUID client, int level, GridClient grid, string[] additionalArgs,
                                SysOut log, MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source,
                                CommandRegistry registry, UUID agentKey, string agentName)
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
                new_notice.NoticeSummary = "Kick: " + kicked + " by " + kicked_by;
                new_notice.Repeats = false;
                new_notice.NoticeDescription = raw_msg;

                ocb.NoticeLists.Add("kicknotice", new_notice);
                ocb.Save();
            }
        }


    }
}
