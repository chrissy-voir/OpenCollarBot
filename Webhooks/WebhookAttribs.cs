﻿/*

Copyright © 2019 Tara Piccari (Aria; Tashia Redrose)
Licensed under the GPLv2

*/

using System;
using System.Reflection;

namespace OpenCollarBot.Webhooks
{

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class WebhookAttribs : Attribute
    {
        public string Path = "";
        public MethodInfo AssignedMethod = null;
        public WebhookAttribs(string WebPath)
        {
            Path = WebPath;
        }
    }
}
