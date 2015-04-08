﻿/***************************************************************************
 *   ContextMenu.cs
 *   
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 3 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using UltimaXNA.UltimaData;

#endregion

namespace UltimaXNA.UltimaWorld.Controllers
{
    public class ContextMenuItem
    {
        private readonly string m_caption;
        private readonly int m_responseCode;

        public ContextMenuItem(int nResponseCode, int iStringID, int iFlags, int iHue)
        {
            m_caption = StringData.Entry(iStringID);
            m_responseCode = nResponseCode;
        }

        public int ResponseCode
        {
            get { return m_responseCode; }
        }

        public string Caption
        {
            get { return m_caption; }
        }
    }

    public class ContextMenu
    {
        private readonly List<ContextMenuItem> _entries = new List<ContextMenuItem>();
        private readonly Serial _serial;

        public bool CanBuy = false;
        public bool CanSell = false;

        // public bool HasContext_Bank = false;
        // public bool HasContext_Trainer = false;
        // public bool HasContext_Stable = false;
        // public bool HasContext_QuestGiver = false;
        // public bool HasContext_PaperDoll = false;
        // public bool HasContext_Inventory = false;

        public ContextMenu(Serial serial)
        {
            _serial = serial;
        }

        public Serial Serial
        {
            get { return _serial; }
        }

        public bool HasContextMenu
        {
            get { return IsMerchant; }
        }

        public bool IsMerchant
        {
            get { return ((CanBuy) || (CanSell)); }
        }

        public ContextMenuItem GetContextEntry(string caption)
        {
            return _entries.FirstOrDefault(i => i.Caption == caption);
        }

        // Add a new context menu entry.
        internal void AddItem(int nResponseCode, int nStringID, int nFlags, int nHue)
        {
            _entries.Add(new ContextMenuItem(nResponseCode, nStringID, nFlags, nHue));
        }

        // Sets up which options are available to the new client.
        internal void FinalizeMenu()
        {
            foreach(var i in _entries)
            {
                if((i.Caption.Length >= 5) && (i.Caption.Substring(0, 5) == "Train"))
                {
                    // train option
                }
                else
                {
                    switch(i.Caption)
                    {
                        case "Buy":
                            CanBuy = true;
                            break;
                        case "Sell":
                            CanSell = true;
                            break;
                        case "Open Paperdoll":
                        case "Open Backpack":
                        case "Quest Conversation":
                        case "Cancel Quest":
                        case "Toggle Item Insurance":
                        case "Auto Renew Inventory Insurance":
                        case "Toggle Monster Title Display":
                        case "Tame":
                        case "Command: Guard":
                        case "Command: Follow":
                        case "Command: Drop":
                        case "Command: Kill":
                        case "Command: Stop":
                        case "Command: Stay":
                        case "Add Friend":
                        case "Remove Friend":
                        case "Transfer":
                        case "Release":
                        case "Ask Destination":
                        case "Abandon Escort":
                        case "Open Bankbox":
                        case "Add Party Member":
                            // unhandled
                            break;
                        default:
                            throw new Exception("Unknown context item: " + i.Caption);
                    }
                }
            }
        }
    }
}