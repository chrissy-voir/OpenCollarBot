/*

Copyright © 2019 Tara Piccari (Aria; Tashia Redrose)
Licensed under the GPLv2

*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Bot;
using OpenMetaverse;



namespace OpenCollarBot
{
    [Serializable()]
    public sealed class OCBotMemory : IConfig
    {
        /*
         * ============================
         * SINGLETON PATTERN START
         * ============================
         */
        [NonSerialized()]
        private static OCBotMemory _instance = null;

        private static OCBotMemory instance
        {
            get { return _instance; }
            set
            {
                _instance = value;
                _instance.Save();
            }
        }
        private static readonly object lockHandle = new object();

        OCBotMemory()
        {

        }

        public static OCBotMemory Memory
        {
            get
            {
                lock (lockHandle)
                {
                    if (instance == null)
                    {
                        instance = OCBotMemory.Reload();
                        instance.CheckIntegrity();
                        instance.SingletonInitialized = true;
                    }
                    return instance;
                }
            }
        }

        /*
         * =============================
         * SINGLETON PATTERN END
         * Usage: OCBotMemory memoryObject = OCBotMemory.Memory;
         * =============================
         */

        [NonSerialized()]
        private SysOut Log = null;
        public float ConfigVersion
        {
            get
            {
                return 1.8f;

            }
            set { }
        }
        public string ConfigFor
        {
            get
            {
                return "OpenCollarBot";
            }
            set { }
        }
        public List<string> Data { get; set; }

        public Dictionary<UUID, int> BotAdmins { get; set; }

        public string DefaultRegion { get; set; }

        public Vector3 DefaultLocation { get; set; }

        public UUID sit_cube { get; set; }


        public DateTime InviteLastSent { get; set; }

        public string gitrepo { get; set; }

        public string gitowner { get; set; }

        [Serializable()]
        public struct ZInventoryItem
        {
            public UUID InventoryID;
            public string InventoryName;
            public InventoryItem assocItem;
        }
        public Dictionary<string, ZInventoryItem> DevelopmentBuildInventory { get; set; }
        public UUID DevelopmentBuildMaster { get; set; }

        [Serializable()]
        public struct RateData
        {
            public UUID User;
            public DateTime Reset_At;
            public int SubmitCount;
        }

        [Serializable()]
        public struct Notices
        {
            public string InternalName;
            public string NoticeSummary;
            public string NoticeDescription;
            public UUID NoticeAttachment;
            public bool Repeats;
            public UUID GroupKey;
            public bool HasAttachment;
            public DateTime LastSent;
        }

        [Serializable()]
        public struct NoticeCreationSessions
        {
            public int State;
            public Notices TemporaryNotice;
            public UUID SessionAv;
        }

        public Dictionary<string, Notices> NoticeLists { get; set; }

        public Dictionary<UUID, NoticeCreationSessions> NoticeSessions { get; set; }
        // Specifically meant for indicating active sessions

        public int HardLimit { get { return 5; } }

        public Dictionary<UUID, RateData> RateLimiter { get; set; }


        public bool SubmitNewRateUsage(UUID ID, MessageHandler.MessageHandleEvent MHE)
        {
            if (RateLimiter.ContainsKey(ID))
            {
                RateData RD = RateLimiter[ID];
                if (RD.SubmitCount < HardLimit)
                {
                    RD.SubmitCount++;

                    if (RD.Reset_At < DateTime.Now)
                    {
                        RD.Reset_At = DateTime.Now.AddDays(7);
                        RD.SubmitCount = 1;
                    }

                    RateLimiter[ID] = RD;
                    Save();
                    return true;
                }
                else
                {
                    if (RD.Reset_At < DateTime.Now)
                    {
                        RateLimiter.Remove(ID);
                        return SubmitNewRateUsage(ID, MHE);
                    }
                    else
                    {
                        //MHE(MessageHandler.Destinations.DEST_LOCAL, UUID.Zero, "submitcount is greater or equal to hardlimit. reset_at is greater than current date");
                        return false;
                    }
                }
            }
            else
            {
                RateData RD = new RateData();
                RD.User = ID;
                RD.Reset_At = DateTime.Now.AddDays(7);
                RD.SubmitCount++;
                RateLimiter.Add(ID, RD);
                Save();
                return true;
            }
        }


        [Serializable()]
        public struct ReportData
        {
            public int ReportStage;
            public string ReportTitle;
            public string ReportBody;
            public string ReportExtraData;
            public int TicketNumber;
        }


        public UUID GroupKey { get; set; }

        // UUID user, int report stage
        public Dictionary<UUID, ReportData> ActiveReportSessions { get; set; }

        public Dictionary<UUID, ReportData> ActiveFeatureSessions { get; set; }

        public Dictionary<UUID, ReportData> ActiveCommentSessions { get; set; }

        public List<string> AuthedGithubUsers { get; set; }

        public UUID AlertGroup { get; set; }


        public UUID StaffGroup { get; set; }


        public bool Authed(string GHLogin)
        {
            if (AuthedGithubUsers.Contains(GHLogin)) return true;
            else return false;
        }

        [Serializable()]
        public struct MailListMember
        {
            public UUID MemberID;
            public bool Informed; // Only used if optout=false
            public bool OptOut; // Overridden by blacklist

            public void setInformed() { Informed = true; }
            public void FlipOpt()
            {
                if (OptOut) OptOut = false;
                else OptOut = true;
            }
        }

        [Serializable()]
        public struct MailList
        {
            // This creates the mailing list
            public UUID MailListOwner;
            public string ListName;
            public List<MailListMember> Members;
            public bool AllowOptOut; // This is overridden by the blacklist
            public void FlipOpt()
            {
                if (AllowOptOut) AllowOptOut = false;
                else AllowOptOut = true;
            }

            // Prepared Data Segment
            // Should ALWAYS be blank or new if not preparing to send a message
            public int PrepState;
            public string PrepMsg;
            public UUID PrepFrom;
        }


        public List<UUID> BlacklistMailingList { get; set; } // for those who wish to permanently opt out (they can opt back in later..)

        public Dictionary<string, MailList> MailingLists { get; set; }


        public List<string> LinkedDLLs { get; set; }

        [NonSerialized()]
        public string status = "";

        public UUID UserNameSearchSession { get; set; }

        [NonSerialized()]
        public bool SingletonInitialized = false;

        [NonSerialized()]
        public bool iHaveBeenTeleported = false;


        public void Save(string CustomName = "OpenCollarBot")
        {
            //if (!File.Exists("OpenCollarBot.bdf")) return;
            SerialManager sm = new SerialManager();
            sm.Write<OCBotMemory>(CustomName, this);
            sm = null;
        }

        public static OCBotMemory Reload(string CustomName = "OpenCollarBot")
        {
            if (!File.Exists(CustomName + ".json")) return new OCBotMemory(); // We Must comply with the Singleton Pattern
            try
            {
                if (instance != null)
                {
                    if (instance.SingletonInitialized)
                        return Memory;
                }

            }
            catch (Exception e)
            {

            }
            SerialManager sm = new SerialManager();
            OCBotMemory ocb = sm.Read<OCBotMemory>(CustomName);
            ocb.CheckIntegrity();
            return ocb;
        }


        public void CheckIntegrity()  // NEEDED TO ENSURE WE DO NOT RUN INTO NULL VALUES DURING RUNTIME OPERATION
        {
            if (Data == null) Data = new List<string>();
            if (BotAdmins == null) BotAdmins = new Dictionary<UUID, int>();
            if (DefaultLocation == null) DefaultLocation = Vector3.Zero;
            if (DefaultRegion == null) DefaultRegion = "";
            if (sit_cube == null) sit_cube = UUID.Zero;
            if (GroupKey == null) GroupKey = UUID.Zero;
            if (InviteLastSent == null) InviteLastSent = DateTime.Now;
            if (ActiveReportSessions == null) ActiveReportSessions = new Dictionary<UUID, ReportData>();
            if (gitrepo == null) gitrepo = "";
            if (gitowner == null) gitowner = "";
            if (RateLimiter == null) RateLimiter = new Dictionary<UUID, RateData>();
            if (ActiveFeatureSessions == null) ActiveFeatureSessions = new Dictionary<UUID, ReportData>();
            if (AlertGroup == null) AlertGroup = UUID.Zero;
            if (AuthedGithubUsers == null) AuthedGithubUsers = new List<string>();
            if (ActiveCommentSessions == null) ActiveCommentSessions = new Dictionary<UUID, ReportData>();
            if (BlacklistMailingList == null) BlacklistMailingList = new List<UUID>();
            if (MailingLists == null) MailingLists = new Dictionary<string, MailList>();
            if (LinkedDLLs == null) LinkedDLLs = new List<string>();
            if (UserNameSearchSession == null) UserNameSearchSession = UUID.Zero;
            if (NoticeLists == null) NoticeLists = new Dictionary<string, Notices>();
            if (NoticeSessions == null) NoticeSessions = new Dictionary<UUID, NoticeCreationSessions>();
            if (DevelopmentBuildInventory == null) DevelopmentBuildInventory = new Dictionary<string, ZInventoryItem>();
            if (DevelopmentBuildMaster == null) DevelopmentBuildMaster = new UUID();
            if (StaffGroup == null) StaffGroup = new UUID();
        }
    }
}
