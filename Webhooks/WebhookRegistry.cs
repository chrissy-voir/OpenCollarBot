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


        public HTTPResponseData RunCommand(string path, string body, NameValueCollection headers, string method)
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
            NotFound.ReplyString = "More water is required!";
            NotFound.Status = 418;
            if (fnc == null) return NotFound;
            object obj = Activator.CreateInstance(fnc.DeclaringType);
            //HTTPResponseData hrd = (HTTPResponseData)fnc.Invoke(obj, new object[] { body, headers });
            // 
            HTTPResponseData hrd = NotFound;

            foreach (WebhookAttribs zAPIPath in hooks.Values)
            {
                // compare strings; If a % symbol is located, then skip that so long as the inbound string matches totally.
                // Append the value of % in the inbound request to the array passed to the function
                List<string> arguments = new List<string>();
                string sCheck = zAPIPath.Path;
                bool Found = true; // Default to true
                if (method != zAPIPath.HTTPMethod) Found = false;

                string[] aCheck = sCheck.Split(new[] { '/' });
                string[] actualRequest = path.Split(new[] { '/', '?' }); // if it contains a ?, we'll put that into the GETBody
                string theArgs = "";

                if (path.Contains('?'))
                {
                    // continue
                    string[] tmp1 = path.Split(new[] { '?' });
                    theArgs = tmp1[1];
                    actualRequest = tmp1[0].Split(new[] { '/' });

                }
                if (actualRequest.Length == aCheck.Length)
                {

                    int i = 0;

                    for (i = 0; i < aCheck.Length; i++)
                    {
                        // TODO: CHANGE THIS SLOPPY MESS TO REGEX.. FOR NOW IT WORKS!
                        if (aCheck[i] == "%")
                        {
                            arguments.Add(actualRequest[i]);
                        }
                        else
                        {

                            if (aCheck[i] == actualRequest[i])
                            {
                                // we're good!

                            }
                            else
                            {
                                // check other path hooks before returning 404!
                                Found = false;
                            }
                        }
                    }
                }
                else Found = false;

                arguments.Add(theArgs);

                if (Found)
                {
                    // Run the method
                    Console.WriteLine("Running: " + zAPIPath.Path + "; " + zAPIPath.AssignedMethod.Name + "; For inbound: " + path);
                    object _method = Activator.CreateInstance(zAPIPath.AssignedMethod.DeclaringType);
                    _ReplyData = (ReplyData)zAPIPath.AssignedMethod.Invoke(_method, new object[] { arguments, body, method, headers });

                    Console.WriteLine("====> " + _ReplyData.Body);

                    return _ReplyData;
                }
            }
            // an API Path wasn't found
            // check the filesystem
            string[] noArgPath = rawURL.Split(new[] { '?' });
            if (File.Exists($"htdocs/{noArgPath[0]}"))  // This will provide a way to display HTML to the user. If the server must process data internally, please use a method & attribute. Nothing is stopping you from also loading in a HTML/js file and returning a stylized response.
            {
                _ReplyData.Status = 200;
                _ReplyData.Body = File.ReadAllText($"htdocs/{noArgPath[0]}");
                Dictionary<string, string> customHeaders = null; // This is mainly going to be used in instances where the domain-server needs a document but CORS isnt set
                if (File.Exists($"htdocs/{noArgPath[0]}.headers"))
                    customHeaders = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText($"htdocs/{noArgPath[0]}.headers"));

                if (customHeaders != null)
                    _ReplyData.CustomOutputHeaders = customHeaders;


            }
            Console.WriteLine(consoleoutput); // <--- We only echo on a not_found as this could get messy otherwise... 
            return _ReplyData;
        }

        public struct HTTPResponseData
        {
            public int Status;
            public string ReplyString;
        }
    }
}
