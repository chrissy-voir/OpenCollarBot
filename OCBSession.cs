/*

Copyright © 2019 Tara Piccari (Aria; Tashia Redrose)
Licensed under the AGPLv3

*/

using OpenMetaverse;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace OpenCollarBot
{
    public sealed class OCBSession
    {
        private static OCBSession _i = null;
        private static readonly object locks = new object();
        static OCBSession() { }
        public static OCBSession Instance
        {
            get
            {
                lock (locks)
                {
                    if(_i == null)
                    {
                        _i = new OCBSession();
                    }
                    return _i;
                }
            }
        }



        public List<string> MonitoredRegions = new List<string>();
        // This file is for volatile information. 
        // If we do not care about it persisting, it will be stored here!
        public Dictionary<int, Program.ScriptDialogSession> ScriptSessions = new Dictionary<int, Program.ScriptDialogSession>();

        public ManualResetEvent MemberLookupRE = new ManualResetEvent(false);
        public Dictionary<UUID, GroupMember> GroupMembers = new Dictionary<UUID, GroupMember>();
        public UUID MemberLookupRequest = UUID.Zero;
        public List<UUID> RemoveReplyHandle = new List<UUID>();
        internal DateTime NextTeleportAttempt;
        internal bool RestartTriggered;


        public List<UUID> CookieList { get; set; } = new List<UUID>();
    }
}
