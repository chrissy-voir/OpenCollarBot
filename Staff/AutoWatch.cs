using System;
using System.Collections.Generic;
using System.Text;
using Bot;
using Bot.CommandSystem;
using OpenMetaverse;
using Bot.NonCommands;
using System.Linq;
using System.Drawing.Imaging;

namespace OpenCollarBot.Staff
{
    /// <summary>
    /// Contains the commands, and the Plugin Hook to watch for name changes, and to watch for those who have been banned for spamming with predictable chat patterns
    /// </summary>
    public class AutoWatch : BaseCommands, IProgram
    {
        public string ProgramName => "AutoWatch";

        public float ProgramVersion => 1.4f;

        public void getTick()
        {
            // Equivalent of a game tick
            foreach(UUID ID in OCBSession.Instance.RemoveReplyHandle)
            {
                if (OCBotMemory.Memory.AntiSpamReply.ContainsKey(ID))
                {
                    OCBotMemory.Memory.AntiSpamReply.Remove(ID);
                    OCBotMemory.Memory.Save();
                }
            }

            OCBSession.Instance.RemoveReplyHandle.Clear();
            return;
        }

        public void LoadConfiguration()
        {
            // Nothing to load - Use OCBotMemory.Instance
        }

        public void onIMEvent(object sender, InstantMessageEventArgs e)
        {
            // Deregister this IM hook
            BotSession.Instance.grid.Self.IM -= onIMEvent;
        }

        public void passArguments(string data)
        {
            // Ignore this method
        }

        public void run()
        {
            // Reset the OCBotMemory Antispam list

            OCBotMemory.Memory.AntiSpamReply = new Dictionary<UUID, OCBotMemory.ReplyData>();
            OCBotMemory.Memory.Save();
        }


        [NotCommand(SourceType = Destinations.DEST_GROUP | Destinations.DEST_DISCORD)]
        public void RunNonCommand(string text, UUID User, string agentName, Destinations src, UUID originator)
        {
            // Checks the chat data against the watchdog

            //NameDB DB = NCConf.Instance.GetEntry(agentName);
            text = text.ToLower();
            foreach(KeyValuePair<string,string> KVP in OCBotMemory.Memory.SpamWatchdogPatterns)
            {
                string[] PatternChecks = KVP.Value.Split(new[] { '|' });
                bool C = true;
                if (text.Length < PatternChecks.Length || text.Length < 4) C = false ;
                if (C)
                {

                    int Tolerance = 4;
                    foreach (string S in PatternChecks)
                    {
                        if (!text.Contains(S.ToLower()))
                        {
                            Tolerance--;
                        }

                    }

                    if (Tolerance > 0)
                    {
                        // Less than 4 failed checks.
                        MHE(Destinations.DEST_GROUP, OCBotMemory.Memory.StaffGroup, "[SpamWatchdog] Suspected spam has been detected for pattern '" + KVP.Key + "'. This alert was triggered by '" + agentName + "'");
                    }
                }
            }

            if(OCBSession.Instance.LastReplyPosted.AddMinutes(OCBotMemory.Memory.DelayTimeReplyMin) > DateTime.Now)
            {
                return;
            }


            foreach(KeyValuePair<string,ReplacePattern> KVP in OCBotMemory.Memory.AutoReplyWatchPatterns)
            {
                int Tolerance = KVP.Value.Tolerance;
                string[] Pattern = KVP.Value.Triggers.Split(new[] { '|' });
                bool C = true;
                if (text.Length <= Tolerance || text.Length <= Pattern.Length) C=false;
                if (C)
                {

                    foreach (string S in Pattern)
                    {
                        if (!text.Contains(S.ToLower()))
                        {
                            Tolerance--;
                        }
                    }

                    if (Tolerance > 0)
                    {
                        bool has_reply_data = false;
                        OCBotMemory.ReplyData reply_data = new OCBotMemory.ReplyData();
                        if (OCBotMemory.Memory.AntiSpamReply.ContainsKey(User))
                        {
                            if (OCBotMemory.Memory.AntiSpamReply[User].Ignore) return;
                            reply_data = OCBotMemory.Memory.AntiSpamReply[User];
                            has_reply_data = true;
                        }else
                        {
                            reply_data.Ignore = false;
                            reply_data.InitialReply = DateTime.Now;
                            
                        }


                        if (!reply_data.Ignore)
                        {
                            OCBSession.Instance.LastReplyPosted = DateTime.Now;
                            MHE(src, originator, KVP.Value.Reply);
                            if (!has_reply_data)
                            {
                                OCBotMemory.Memory.AntiSpamReply.Add(User, reply_data);
                                OCBotMemory.Memory.Save();
                            }

                        }
                        if (has_reply_data)
                        {

                            if (reply_data.TriggerCount >= OCBotMemory.Memory.MAX_TRIGGERS)
                            {
                                bool save = false;
                                if (!reply_data.Ignore) save = true;
                                reply_data.SetIgnore();
                                OCBotMemory.Memory.AntiSpamReply[User] = reply_data;

                                if (save) OCBotMemory.Memory.Save();
                            }
                            else
                            {

                                reply_data.TriggerCount++;
                                OCBotMemory.Memory.AntiSpamReply[User] = reply_data;

                                OCBotMemory.Memory.Save();
                            }
                        }
                        else
                        {
                            reply_data.TriggerCount = 1;
                            reply_data.InitialReply = DateTime.Now;
                            reply_data.Ignore = false;

                            if (!OCBotMemory.Memory.AntiSpamReply.ContainsKey(User))
                                OCBotMemory.Memory.AntiSpamReply.Add(User, reply_data);
                            else
                                OCBotMemory.Memory.AntiSpamReply[User] = reply_data;
                        }

                    }
                }
            }

        }


        [CommandGroup("reset_ignore_list",3,0,"reset_ignore_list\t\t- Resets the ignore block list", Destinations.DEST_AGENT | Destinations.DEST_DISCORD | Destinations.DEST_GROUP | Destinations.DEST_LOCAL)]
        public void reset_ignore_list(UUID client, int level, string[] additionalArgs,
                                 Destinations source,
                                UUID agentKey, string agentName)
        {
            MHE(source, client, "Resetting ignore list");
            OCBotMemory.Memory.AntiSpamReply = new Dictionary<UUID, OCBotMemory.ReplyData>();
            OCBSession.Instance.RemoveReplyHandle = new List<UUID>();
            OCBotMemory.Memory.Save();
            MHE(source, client, "Complete!");
        }


        [CommandGroup("spam_watch", 5, 2, "spam_watch [watch_label] [watch_for_pattern] - Watches for a specific pattern. Use a pipe delimiter to separate words", Destinations.DEST_AGENT | Destinations.DEST_DISCORD | Destinations.DEST_GROUP | Destinations.DEST_LOCAL)]
        public void watch_for_spam(UUID client, int level, string[] additionalArgs,
                                 Destinations source,
                                UUID agentKey, string agentName)
        {
            MHE(source, client, "[SpamWatchdog] Stand by");
            if( OCBotMemory.Memory.SpamWatchdogPatterns.ContainsKey(additionalArgs[0]) )
            {
                MHE(source, client, "[SpamWatchdog] Adding the new watch terms to Ident '" + additionalArgs[0] + "'");
                additionalArgs[1] = additionalArgs[1].Replace("&", " ");
                string[] Pattern = OCBotMemory.Memory.SpamWatchdogPatterns[additionalArgs[0]].Split(new[] { '|' });
                string[] Pattern2 = additionalArgs[1].Split(new[] { '|' });
                List<string> Patterns = new List<string>();
                foreach(string P in Pattern)
                {
                    Patterns.Add(P);
                }
                foreach(string P in Pattern2)
                {
                    if (Patterns.Contains(P))
                    {
                        // skip
                    }
                    else
                    {
                        Patterns.Add(P);
                    }
                }


                Pattern = Patterns.ToArray();

                OCBotMemory.Memory.SpamWatchdogPatterns[additionalArgs[0]] = String.Join('|', Pattern);
            } else
            {
                MHE(source, client, "[SpamWatchdog] Add specified element list for Ident '" + additionalArgs[1] + "'");

                OCBotMemory.Memory.SpamWatchdogPatterns.Add(additionalArgs[0], additionalArgs[1]);
            }

            MHE(source, client, "[SpamWatchdog] Operations completed");
            OCBotMemory.Memory.Save();
        }



        [CommandGroup("rem_spam_watch", 5, 1, "rem_spam_watch [watch_label] - Removes a spam watchdog", Destinations.DEST_AGENT | Destinations.DEST_DISCORD | Destinations.DEST_GROUP | Destinations.DEST_LOCAL )]
        public void RMwatch_for_spam(UUID client, int level, string[] additionalArgs,
                                 Destinations source,
                                UUID agentKey, string agentName)
        {
            MHE(source, client, "[SpamWatchdog] Attempting to remove the watchdog thread");
            if (OCBotMemory.Memory.SpamWatchdogPatterns.ContainsKey(additionalArgs[0]))
            {
                OCBotMemory.Memory.SpamWatchdogPatterns.Remove(additionalArgs[0]);
            }

            OCBotMemory.Memory.Save();
            MHE(source, client, "[SpamWatchdog] Operations completed");
        }


        [CommandGroup("ls_spam_watch", 2, 0, "ls_spam_watch - Lists spam watchdog", Destinations.DEST_AGENT | Destinations.DEST_DISCORD | Destinations.DEST_GROUP | Destinations.DEST_LOCAL )]
        public void lswatch_for_spam(UUID client, int level, string[] additionalArgs,
                                 Destinations source,
                                UUID agentKey, string agentName)
        {
            MHE(source, client, "[SpamWatchdog] Listing watchdogs and query patterns");

            foreach(KeyValuePair<string,string> kvp in OCBotMemory.Memory.SpamWatchdogPatterns)
            {
                MHE(source, client, "[WATCHDOG] " + kvp.Key + "\t\t-\t\t" + kvp.Value);
            }

            MHE(source, client, "[SpamWatchdog] Operations completed");
        }

        [Serializable()]
        public struct ReplacePattern
        {
            public string Triggers;
            public int Tolerance;
            public string Reply;

            public string AsString()
            {
                return "\nTriggers: " + Triggers + "\nTolerance: " + Tolerance.ToString() + "\nReply: " + Reply;
            }
        }


        [CommandGroup("reply_watch", 5, 4, "reply_watch [ReplacerLabel] [pattern:PipeDelimiter] [int:Tolerance] [replyPattern:Underscore_As_Spaces]", Destinations.DEST_AGENT | Destinations.DEST_DISCORD | Destinations.DEST_GROUP | Destinations.DEST_LOCAL )]
        public void reply_watch(UUID client, int level, string[] additionalArgs,
                                 Destinations source,
                                UUID agentKey, string agentName)
        {
            MHE(source, client, "[ReplyWatchdog] Pattern add in progress");
            if (OCBotMemory.Memory.AutoReplyWatchPatterns.ContainsKey(additionalArgs[0]))
            {
                ReplacePattern pttrn = OCBotMemory.Memory.AutoReplyWatchPatterns[additionalArgs[0]];
                additionalArgs[1] = additionalArgs[1].Replace('&', ' ');
                pttrn.Triggers = additionalArgs[1];
                pttrn.Tolerance = Convert.ToInt32(additionalArgs[2]);
                pttrn.Reply = additionalArgs[3].Replace('_', ' ');
                

                OCBotMemory.Memory.AutoReplyWatchPatterns[additionalArgs[0]] = pttrn;
            }
            else
            {
                ReplacePattern ptrn = new ReplacePattern();
                ptrn.Triggers = additionalArgs[1];
                ptrn.Tolerance = Convert.ToInt32(additionalArgs[2]);
                ptrn.Reply = additionalArgs[3].Replace('_',' ');

                OCBotMemory.Memory.AutoReplyWatchPatterns.Add(additionalArgs[0], ptrn);
            }

            OCBotMemory.Memory.Save();

            MHE(source, client, "[ReplyWatchdog] Pattern add completed");
        }


        [CommandGroup("rem_reply_watch", 5, 1, "rem_reply_watch [ReplacerLabel]", Destinations.DEST_AGENT | Destinations.DEST_DISCORD | Destinations.DEST_GROUP | Destinations.DEST_LOCAL )]
        public void RMreply_watch(UUID client, int level, string[] additionalArgs,
                                 Destinations source,
                                UUID agentKey, string agentName)
        {
            MHE(source, client, "[ReplyWatchdog] Pattern removal in progress");
            if (OCBotMemory.Memory.AutoReplyWatchPatterns.ContainsKey(additionalArgs[0]))
            {
                OCBotMemory.Memory.AutoReplyWatchPatterns.Remove(additionalArgs[0]);
            }
            OCBotMemory.Memory.Save();

            MHE(source, client, "[ReplyWatchdog] Pattern removal completed");
        }


        [CommandGroup("ls_reply_watch", 2, 0, "ls_reply_watch - Lists reply watchdog", Destinations.DEST_AGENT | Destinations.DEST_DISCORD | Destinations.DEST_GROUP | Destinations.DEST_LOCAL )]
        public void lswatch_for_reply(UUID client, int level, string[] additionalArgs,
                                 Destinations source,
                                UUID agentKey, string agentName)
        {
            MHE(source, client, "[ReplyWatchdog] Listing watchdogs");

            foreach (KeyValuePair<string, ReplacePattern> kvp in OCBotMemory.Memory.AutoReplyWatchPatterns)
            {
                MHE(source, client, "[WATCHDOG] " + kvp.Key + "\t\t-\t\t" + kvp.Value.AsString());
            }

            MHE(source, client, "[ReplyWatchdog] Operations completed");
        }
    }
}
