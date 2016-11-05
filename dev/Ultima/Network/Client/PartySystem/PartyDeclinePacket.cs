﻿/***************************************************************************
 *   PartyDeclinePacket.cs
 *   
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 3 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/
using UltimaXNA.Core.Network.Packets;
using UltimaXNA.Ultima.World.Entities.Mobiles;

namespace UltimaXNA.Ultima.Network.Client.PartySystem {
    public class PartyDeclinePacket : SendPacket {
        public PartyDeclinePacket(Mobile Leader) 
            : base(0xbf, "Party Join Decline") {
            Stream.Write((short)6);
            Stream.Write((byte)9);
            Stream.Write(Leader.Serial);
        }
    }
}