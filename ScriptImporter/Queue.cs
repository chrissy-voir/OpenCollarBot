/*

Copyright © 2019 Tara Piccari (Aria; Tashia Redrose)
Licensed under the GPLv2

*/

using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using Bot;
using Bot.Assemble;
using Bot.CommandSystem;
using System.Linq;
using System.Threading;

namespace OpenCollarBot.ScriptImporter
{
    public sealed class Queue
    {
        private static Queue _i = null;
        private static readonly object locks = new object();
        static Queue() { }
        public static Queue Instance
        {
            get
            {
                lock (locks)
                {
                    if (_i == null)
                    {
                        _i = new Queue();
                    }
                    return _i;
                }
            }
        }

        public struct QueueType
        {
            public string Name;
            public string Text;
            public string Hash;
            public string GitOwner;
            public string GitBranch;
            public string Container;
            public string ItemType;
            public string FileExt; // used for notecard
        }

        //        public List<QueueType> QueuedUpdates = new List<QueueType>();
        // Dictionary will hold the script path, and a import flag object
        public AutoResetEvent ARE = new AutoResetEvent(false);
        public InventoryItem invItem = null;
        public bool UploadSuccess;
        public bool CompileSuccess;
        public List<string> compileMessages;
        public Dictionary<UUID, List<QueueType>> ActualQueue = new Dictionary<UUID,List<QueueType>>();
    }

    public class QueueRunner
    {
        // This class executes 1 queue item when called upon
        
        public static void run()
        {
            Queue X = Queue.Instance;
            // Get first queued item

            Queue.Instance.invItem = null;

            if (X.ActualQueue.Count == 0) return;
            KeyValuePair<UUID, List<Queue.QueueType>> kvp = X.ActualQueue.First();

            InventoryManager inv = BotSession.Instance.grid.Inventory;
            UUID GitOwnerFolder = UUID.Zero;
            UUID Branch = UUID.Zero;
            UUID Recipient = kvp.Key;
            UUID Container = UUID.Zero;

            UUID ParentFolder = inv.FindFolderForType(AssetType.Folder);


            Queue.QueueType QT = kvp.Value.First();

            InventoryBase GitOwnerFolderBase = null;

            try
            {

                GitOwnerFolderBase = inv.LocalFind(ParentFolder, new[] { QT.GitOwner }, 0, false).First();

                GitOwnerFolder = GitOwnerFolderBase.UUID;
            } catch(Exception e)
            {
                
            }
            
            if(GitOwnerFolder == UUID.Zero || GitOwnerFolder == ParentFolder)
            {
                GitOwnerFolder = inv.CreateFolder(ParentFolder, QT.GitOwner);
            }

            InventoryBase GitBranchBase = null;
            try
            {
                GitBranchBase = inv.LocalFind(GitOwnerFolder, new[] { QT.GitBranch }, 0, false).First();

                Branch = GitBranchBase.UUID;
            }
            catch (Exception e) { }
            if(Branch == UUID.Zero || Branch == ParentFolder)
            {
                Branch = inv.CreateFolder(GitOwnerFolder, QT.GitBranch);
            }

            InventoryBase GitContainer = null;
            try
            {
                GitContainer=inv.LocalFind(Branch, new[] { QT.Container },0, false).First();

                Container = GitContainer.UUID;
            }catch(Exception e) { }
            if(Container == UUID.Zero || Container == ParentFolder)
            {
                Container = inv.CreateFolder(Branch, QT.Container);
            }

            Console.WriteLine("Git Owner Folder: "+GitOwnerFolder.ToString());
            Console.WriteLine("Branch: "+Branch.ToString());
            Console.WriteLine("Container: " + Container.ToString());

            // Locate the Script or Notecard
            InventoryBase importedItem = null;
            UUID ItemID = UUID.Zero;
            try
            {
                if (QT.ItemType == "script")
                    importedItem = inv.LocalFind(Container, new[] { QT.Name }, 0, true).First();
                else if (QT.ItemType == "notecard")
                    importedItem = inv.LocalFind(Container, new[] { QT.Name + QT.FileExt }, 0, false).First();
                ItemID = importedItem.UUID;
            }
            catch (Exception e) { }

            if(ItemID == UUID.Zero || ItemID == null)
            {
                Queue.Instance.invItem = null;
            }
            else
            {

                Queue.Instance.ARE = new AutoResetEvent(false);
                inv.ItemReceived += Inv_ItemReceived;
                inv.RequestFetchInventory(ItemID, importedItem.OwnerID);

                Queue.Instance.ARE.WaitOne(TimeSpan.FromSeconds(15));
            }

            // Check that invItem is instantiated

            AssetType itemType = AssetType.Folder;
            InventoryType InvType = InventoryType.Animation;

            if (QT.ItemType == "notecard")
            {
                InvType = InventoryType.Notecard;
                itemType = AssetType.Notecard;
            }
            else if (QT.ItemType == "script")
            {
                InvType = InventoryType.LSL;
                itemType = AssetType.LSLText;
            }

            if (Queue.Instance.invItem == null)
            {
                // We can safely create the inventory item now

                string FinalName = QT.Name;
                if (InvType == InventoryType.Notecard) FinalName += QT.FileExt;
                UUID Transaction = UUID.Random();
                inv.RequestCreateItem(Container, FinalName, QT.Hash, itemType, Transaction, InvType, PermissionMask.All, delegate (bool success, InventoryItem ii)
                 {
                     if (success)
                     {
                         Queue.Instance.invItem = ii;
                         Queue.Instance.ARE.Set();
                     }
                 });

                Queue.Instance.ARE.WaitOne(TimeSpan.FromSeconds(10));
                InventoryItem actualItem = Queue.Instance.invItem;
                if(itemType == AssetType.LSLText)
                {
                    inv.RequestUpdateScriptAgentInventory(EncodeScript(QT.Text), actualItem.UUID, true, delegate (bool uploaded, string uploadstatus, bool compileSuccess, List<string> msgs, UUID itemID, UUID assetID)
                    {
                        Queue.Instance.compileMessages = msgs;
                        Queue.Instance.CompileSuccess = compileSuccess;
                        Queue.Instance.UploadSuccess = uploaded;
                        Queue.Instance.ARE.Set();
                    });

                    Queue.Instance.ARE.WaitOne(TimeSpan.FromSeconds(20));

                    if (Queue.Instance.UploadSuccess)
                    {
                        if (Queue.Instance.CompileSuccess)
                        {
                            // Remove this from future queues
                            kvp.Value.Remove(QT);
                            // quickly check whether KVP.Value is empty
                            if (kvp.Value.Count == 0)
                            {
                                // Send the container folder, and remove this from queue
                                //inv.GiveFolder(Branch, GitBranchBase.Name, Recipient, false);
                                List<InventoryBase> folders = inv.FolderContents(Branch, BotSession.Instance.grid.Self.AgentID, true, false, InventorySortOrder.ByName, TimeSpan.FromSeconds(30).Milliseconds);

                                foreach (InventoryBase bas in folders)
                                {
                                    inv.GiveFolder(bas.UUID, bas.Name, Recipient, false);
                                    BotSession.Instance.MHE(MessageHandler.Destinations.DEST_AGENT, Recipient, "Sending : " + bas.Name);
                                }

                                X.ActualQueue.Remove(Recipient);
                            }
                            else
                            {
                                X.ActualQueue[Recipient] = kvp.Value;
                            }
                            BotSession.Instance.MHE(MessageHandler.Destinations.DEST_AGENT, Recipient, "Item '" + QT.Name + "' finished processing");
                        }
                    }else
                    {

                        BotSession.Instance.MHE(MessageHandler.Destinations.DEST_AGENT, Recipient, "Item: " + QT.Name + QT.FileExt + " failed!");
                    }
                } else if(itemType == AssetType.Notecard)
                {
                    inv.RequestUploadNotecardAsset(Encoding.UTF8.GetBytes(QT.Text), actualItem.UUID, delegate (bool success, string status, UUID itemID, UUID assetID)
                    {
                        if (success)
                        {
                            Queue.Instance.UploadSuccess = success;
                            Queue.Instance.ARE.Set();
                        }
                    });

                    if (Queue.Instance.UploadSuccess)
                    {
                        kvp.Value.Remove(QT);
                        if (kvp.Value.Count == 0)
                        {
                            X.ActualQueue.Remove(Recipient);
//                            inv.GiveFolder(Branch, GitBranchBase.Name, Recipient, false);
                            List<InventoryBase> folders = inv.FolderContents(Branch, BotSession.Instance.grid.Self.AgentID, true, false, InventorySortOrder.ByName, TimeSpan.FromSeconds(30).Milliseconds);

                            foreach(InventoryBase bas in folders){
                                inv.GiveFolder(bas.UUID, bas.Name, Recipient, false);
                                BotSession.Instance.MHE(MessageHandler.Destinations.DEST_AGENT, Recipient, "Sending : " + bas.Name);
                            }
                        }
                        else
                        {
                            X.ActualQueue[Recipient] = kvp.Value;
                        }
                        BotSession.Instance.MHE(MessageHandler.Destinations.DEST_AGENT, Recipient, "Item '" + QT.Name + "' finished processing");
                    }else
                    {

                        BotSession.Instance.MHE(MessageHandler.Destinations.DEST_AGENT, Recipient, "Item: " + QT.Name + QT.FileExt + " failed!");
                    }
                }
            } else
            {
                // Item exists, verify hash data!
                if(itemType == AssetType.LSLText)
                {
                    if(Queue.Instance.invItem.Description == QT.Hash)
                    {
                        // remove from queue
                        kvp.Value.Remove(QT);
                        if (kvp.Value.Count == 0)
                        {
                            List<InventoryBase> folders = inv.FolderContents(Branch, BotSession.Instance.grid.Self.AgentID, true, false, InventorySortOrder.ByName, TimeSpan.FromSeconds(30).Milliseconds);

                            foreach (InventoryBase bas in folders)
                            {
                                inv.GiveFolder(bas.UUID, bas.Name, Recipient, false);
                                BotSession.Instance.MHE(MessageHandler.Destinations.DEST_AGENT, Recipient, "Sending : " + bas.Name);
                            }
                            X.ActualQueue.Remove(Recipient);
                            //inv.GiveFolder(Branch, GitBranchBase.Name, Recipient, false);
                        }else
                        {
                            X.ActualQueue[Recipient] = kvp.Value;
                        }
                        BotSession.Instance.MHE(MessageHandler.Destinations.DEST_AGENT, Recipient, "Item '" + QT.Name + "' finished processing - no changes needed");

                    } else
                    {
                        // update the script now
                        inv.RequestUpdateScriptAgentInventory(EncodeScript(QT.Text), Queue.Instance.invItem.UUID, true, delegate (bool uploaded, string uploadstatus, bool compileSuccess, List<string> msgs, UUID itemID, UUID assetID)
                         {
                             Queue.Instance.compileMessages = msgs;
                             Queue.Instance.CompileSuccess = compileSuccess;
                             Queue.Instance.UploadSuccess = uploaded;
                             Queue.Instance.ARE.Set();
                         });

                        Queue.Instance.ARE.WaitOne(TimeSpan.FromSeconds(15));

                        if (Queue.Instance.UploadSuccess)
                        {
                            if (Queue.Instance.CompileSuccess)
                            {
                                X.invItem.Description = QT.Hash;
                                kvp.Value.Remove(QT);
                                if(kvp.Value.Count == 0)
                                {
                                    List<InventoryBase> folders = inv.FolderContents(Branch, BotSession.Instance.grid.Self.AgentID, true, false, InventorySortOrder.ByName, TimeSpan.FromSeconds(30).Milliseconds);

                                    foreach (InventoryBase bas in folders)
                                    {
                                        inv.GiveFolder(bas.UUID, bas.Name, Recipient, false);
                                        BotSession.Instance.MHE(MessageHandler.Destinations.DEST_AGENT, Recipient, "Sending : " + bas.Name);
                                    }
                                    X.ActualQueue.Remove(Recipient);
                                    //inv.GiveFolder(Branch, GitBranchBase.Name, Recipient, false);
                                }
                                else
                                {
                                    X.ActualQueue[Recipient] = kvp.Value;
                                }
                                BotSession.Instance.MHE(MessageHandler.Destinations.DEST_AGENT, Recipient, "Item '" + QT.Name + "' finished processing");
                            }
                        } else
                        {

                            BotSession.Instance.MHE(MessageHandler.Destinations.DEST_AGENT, Recipient, "Item: " + QT.Name + QT.FileExt + " failed!");
                        }
                        

                    }
                } else if(itemType == AssetType.Notecard)
                {
                    if(Queue.Instance.invItem.Description == QT.Hash)
                    {
                        kvp.Value.Remove(QT);
                        if (kvp.Value.Count == 0)
                        {
                            List<InventoryBase> folders = inv.FolderContents(Branch, BotSession.Instance.grid.Self.AgentID, true, false, InventorySortOrder.ByName, TimeSpan.FromSeconds(30).Milliseconds);

                            foreach (InventoryBase bas in folders)
                            {
                                inv.GiveFolder(bas.UUID, bas.Name, Recipient, false);
                                BotSession.Instance.MHE(MessageHandler.Destinations.DEST_AGENT, Recipient, "Sending : " + bas.Name);
                            }
                            X.ActualQueue.Remove(Recipient);
                            //inv.GiveFolder(Branch, GitBranchBase.Name, Recipient, false);
                        }
                        else
                        {
                            X.ActualQueue[Recipient] = kvp.Value;
                        }

                        BotSession.Instance.MHE(MessageHandler.Destinations.DEST_AGENT, Recipient, "Item '" + QT.Name + "' finished processing - no changes needed");
                    }
                    else
                    {
                        inv.RequestUploadNotecardAsset(EncodeScript(QT.Text), Queue.Instance.invItem.UUID, delegate (bool success, string status, UUID itemID, UUID assetID)
                        {
                            Queue.Instance.UploadSuccess = success;
                            Queue.Instance.ARE.Set();

                        });

                        Queue.Instance.ARE.WaitOne(TimeSpan.FromSeconds(15));

                        if (Queue.Instance.UploadSuccess)
                        {
                            // Notecard updated
                            kvp.Value.Remove(QT);
                            if (kvp.Value.Count == 0)
                            {
                                List<InventoryBase> folders = inv.FolderContents(Branch, BotSession.Instance.grid.Self.AgentID, true, false, InventorySortOrder.ByName, TimeSpan.FromSeconds(30).Milliseconds);

                                foreach (InventoryBase bas in folders)
                                {
                                    inv.GiveFolder(bas.UUID, bas.Name, Recipient, false);
                                    BotSession.Instance.MHE(MessageHandler.Destinations.DEST_AGENT, Recipient, "Sending : " + bas.Name);
                                }
                                X.ActualQueue.Remove(Recipient);
                                //inv.GiveFolder(Branch, GitBranchBase.Name, Recipient, false);
                            }
                            else
                            {
                                X.ActualQueue[Recipient] = kvp.Value;
                            }
                            BotSession.Instance.MHE(MessageHandler.Destinations.DEST_AGENT, Recipient, "Item '" + QT.Name + "' finished processing");
                            
                        } else
                        {
                            BotSession.Instance.MHE(MessageHandler.Destinations.DEST_AGENT, Recipient, "Item: " + QT.Name + QT.FileExt + " failed!");
                        }
                    }
                }
            }

            X.ARE = new AutoResetEvent(false);
            X.compileMessages = new List<string>();
            X.CompileSuccess = false;
            X.invItem = null;
            X.UploadSuccess = false;
        }

        private static byte[] EncodeScript(string raw)
        {
            byte[] strByte = Encoding.UTF8.GetBytes(raw);
            byte[] assetData = new byte[strByte.Length];
            Array.Copy(strByte, 0, assetData, 0, strByte.Length);
            return assetData;
        }

        private static void Inv_ItemReceived(object sender, ItemReceivedEventArgs e)
        {
            Queue.Instance.invItem = e.Item;
            Queue.Instance.ARE.Set();
            BotSession.Instance.grid.Inventory.ItemReceived -= Inv_ItemReceived;
        }
    }
}
