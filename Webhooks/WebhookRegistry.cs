/*

Copyright © 2019 Tara Piccari (Aria; Tashia Redrose)
Licensed under the GPLv2

*/

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection;
using System.Text;

namespace OpenCollarBot.Webhooks
{
    public sealed class WebhookRegistry
    {
        private static WebhookRegistry _reg = null;
        private static readonly object locks = new object();
        static WebhookRegistry()
        {

        }
        public static WebhookRegistry Instance
        {
            get
            {
                lock (locks)
                {
                    if(_reg == null)
                    {
                        _reg = new WebhookRegistry();
                        _reg.LocateHooks();
                    }
                    return _reg;
                }
            }
        }


        public Dictionary<string, WebhookAttribs> hooks = new Dictionary<string, WebhookAttribs>();

        public void LocateHooks()
        {
            try
            {
                int i = 0;
                for(i = 0; i< AppDomain.CurrentDomain.GetAssemblies().Length; i++)
                {
                    // Grab Assembly
                    Assembly asm = null;
                    try
                    {
                        asm = AppDomain.CurrentDomain.GetAssemblies()[i];
                    }catch(Exception e)
                    {

                    }

                    if(asm != null)
                    {
                        int ii = 0;
                        for(ii = 0; ii<asm.GetTypes().Length; ii++)
                        {
                            Type T = null;
                            try
                            {

                                T = asm.GetTypes()[ii];
                            }catch(Exception e)
                            {

                            }
                            if(T != null)
                            {
                                // Grab the WebHook Attribute
                                if (T.IsClass)
                                {
                                    foreach(MethodInfo mi in T.GetMethods())
                                    {
                                        WebhookAttribs[] wha = (WebhookAttribs[])mi.GetCustomAttributes(typeof(WebhookAttribs), false);
                                        // 
                                        int ix = 0;
                                        for(ix=0;ix<wha.Length;ix++)
                                        {
                                            WebhookAttribs attribu = wha[ix];
                                            attribu.AssignedMethod = mi;
                                            hooks.Add(attribu.Path, attribu);
                                        }
                                    }
                                }
                            }

                        }
                    }
                }
            }catch(Exception e)
            {

            }
        }


        public HTTPResponseData RunCommand(string path, string body, NameValueCollection headers)
        {
            // Run the command then return the response string from the server
            MethodInfo fnc = null;
            foreach(KeyValuePair<string, WebhookAttribs> kvp in hooks)
            {
                if(kvp.Value.Path.ToLower() == path.ToLower())
                {
                    fnc = kvp.Value.AssignedMethod;
                }
            }
            HTTPResponseData NotFound = new HTTPResponseData();
            NotFound.ReplyString = "Not Found";
            NotFound.Status = 404;
            if (fnc == null) return NotFound;
            object obj = Activator.CreateInstance(fnc.DeclaringType);
            HTTPResponseData hrd = (HTTPResponseData)fnc.Invoke(obj, new object[] { body, headers });
            // 
            return hrd;
        }

        public struct HTTPResponseData
        {
            public int Status;
            public string ReplyString;
        }
    }
}
