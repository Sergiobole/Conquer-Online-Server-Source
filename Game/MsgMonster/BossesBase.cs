using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using COServer.Game.MsgTournaments;

namespace COServer.Game.MsgMonster
{
    public class BossesBase
    {
        public static void BossesTimer()
        {
            int Minute = DateTime.Now.Minute;
            if (DateTime.Now.Hour == 00 && DateTime.Now.Minute == 01 && DateTime.Now.Second == 2 || DateTime.Now.Hour == 02 && DateTime.Now.Minute == 01 && DateTime.Now.Second == 2 ||
                DateTime.Now.Hour == 04 && DateTime.Now.Minute == 01 && DateTime.Now.Second == 2 || DateTime.Now.Hour == 06 && DateTime.Now.Minute == 01 && DateTime.Now.Second == 2 ||
                DateTime.Now.Hour == 08 && DateTime.Now.Minute == 01 && DateTime.Now.Second == 2 || DateTime.Now.Hour == 10 && DateTime.Now.Minute == 01 && DateTime.Now.Second == 2 ||
                DateTime.Now.Hour == 12 && DateTime.Now.Minute == 01 && DateTime.Now.Second == 2 || DateTime.Now.Hour == 14 && DateTime.Now.Minute == 01 && DateTime.Now.Second == 2 ||
                DateTime.Now.Hour == 16 && DateTime.Now.Minute == 01 && DateTime.Now.Second == 2 || DateTime.Now.Hour == 18 && DateTime.Now.Minute == 01 && DateTime.Now.Second == 2 ||
                DateTime.Now.Hour == 20 && DateTime.Now.Minute == 01 && DateTime.Now.Second == 2 || DateTime.Now.Hour == 22 && DateTime.Now.Minute == 01 && DateTime.Now.Second == 2)
            {
                Random R = new Random();
                int Nr = R.Next(1, 8);
                if (Nr == 1) SpawnHandler(1015, 199, 193, 20070, "Snow Banshee", "will appear at " + DateTime.Now.Hour + ":30! Get ready to fight! You only have 5 minutes!", " has spawned in " +
                    Database.Server.MapName[1015] + " (199,193)!", MsgServer.MsgStaticMessage.Messages.SnowBanshee);
                if (Nr == 2) SpawnHandler(1002, 564, 792, 20060, "TeratoDragon ", "will appear at " + DateTime.Now.Hour + ":30! Get ready to fight! You only have 5 minutes!", " has spawned in " +
                    Database.Server.MapName[1002] + " (564,792)!", MsgServer.MsgStaticMessage.Messages.TeratoDragon);
                if (Nr == 3) SpawnHandler(1012, 118, 110, 20160, "ThrillingSpook ", "will appear at " + DateTime.Now.Hour + ":30! Get ready to fight! You only have 5 minutes!", " has spawned in" +
                    Database.Server.MapName[1012] + " (118,110)!", MsgServer.MsgStaticMessage.Messages.ThrillingSpook);
                if (Nr == 4) SpawnHandler(1016, 041, 034, 3821, "Capricorn ", "will appear at " + DateTime.Now.Hour + ":30! Get ready to fight! You only have 5 minutes!", " has spawned in " +
                    Database.Server.MapName[1016] + " (41,34)!", MsgServer.MsgStaticMessage.Messages.Capricorn);
                if (Nr == 5) SpawnHandler(1076, 221, 176, 3822, "Raikou", "will appear at " + DateTime.Now.Hour + ":30! Get ready to fight! You only have 5 minutes!", " has spawned in " +
                    Database.Server.MapName[1076] + " (221,176)!", MsgServer.MsgStaticMessage.Messages.Raikou);
                if (Nr == 6) SpawnHandler(1000, 292, 451, 6643, "SwordMaster ", "will appear at " + DateTime.Now.Hour + ":30! Get ready to fight! You only have 5 minutes!", " has spawned in " +
                    Database.Server.MapName[1000] + " (292,451)!", MsgServer.MsgStaticMessage.Messages.SwordMaster);
                if (Nr == 7) SpawnHandler(1011, 791, 480, 20300, "NemesisTyrant ", "will appear at " + DateTime.Now.Hour + ":30! Get ready to fight! You only have 5 minutes!", " has spawned in " +
                    Database.Server.MapName[1011] + " (791,480)!", MsgServer.MsgStaticMessage.Messages.NemesisTyrant);
                if (Nr == 8) SpawnHandler(1105, 93, 54, 20055, "LavaBeast ", "will appear at " + DateTime.Now.Hour + ":30! Get ready to fight! You only have 5 minutes!", " has spawned in " +
                    Database.Server.MapName[1105] + " (93,54)!", MsgServer.MsgStaticMessage.Messages.LavaBeast);
            }
        }
        public static void SpawnHandler(uint MapID, ushort X, ushort Y, uint MobID, string MonsterName, string PrepareMsg, string Msg, MsgServer.MsgStaticMessage.Messages idmsg = MsgServer.MsgStaticMessage.Messages.None)
        {
            var Map = Database.Server.ServerMaps[MapID];
            if (!Map.ContainMobID(MobID))
            {
                using (var rec = new ServerSockets.RecycledPacket())
                {
                    var stream = rec.GetStream();
                    Program.SendGlobalPackets.Enqueue(new MsgServer.MsgMessage(MonsterName + Msg, "ALLUSERS", "Server", MsgServer.MsgMessage.MsgColor.white, MsgServer.MsgMessage.ChatMode.Center).GetArray(stream));
                    Database.Server.AddMapMonster(stream, Map, MobID, X, Y, 1, 1, 1);
                }
            }
            else
            {
                using (var rec = new ServerSockets.RecycledPacket())
                {
                    var stream = rec.GetStream();
                    Program.SendGlobalPackets.Enqueue(new MsgServer.MsgMessage(MonsterName + Msg, "ALLUSERS", "Server", MsgServer.MsgMessage.MsgColor.white, MsgServer.MsgMessage.ChatMode.Center).GetArray(stream));
                }
            }

        }
    }
}