/*

Copyright © 2019 Tara Piccari (Aria; Tashia Redrose)
Licensed under the AGPLv3

*/

using System;
using System.Collections.Generic;
using System.Text;

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



        // This file is for volatile information. 
        // If we do not care about it persisting, it will be stored here!
        public Dictionary<int, Program.ScriptDialogSession> ScriptSessions = new Dictionary<int, Program.ScriptDialogSession>();
    }
}
