using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COServer.EventsLib
{
    public class GuildsDeathMatch : BaseEvent
    {
        public static Dictionary<string, int> PrizesClaimed
             = new Dictionary<string, int>();
        public GuildsDeathMatch()
            : base(8509, "Guilds Death Match", 100, Game.MsgServer.MsgStaticMessage.Messages.GuildsDeathMatch) //prize doesn't count here
        {
        }
        DateTime lastSent = DateTime.Now;
        public Dictionary<string, int> GuildScores
             = new Dictionary<string, int>();
        List<string> score = new List<string>();
        public override void worker()
        {
            base.worker();
            score.Clear();
            GuildScores.Clear();
            foreach (var c in Database.Server.GamePoll.Values.Where(e => e.Player.Map == map && e.Player.Alive))
            {
                if (c.Player.MyGuild == null)
                {
                    c.Teleport( 430, 329,1002);
                    continue;
                }
                if (!GuildScores.ContainsKey(c.Player.MyGuild.GuildName))
                    GuildScores.Add(c.Player.MyGuild.GuildName, 0);
                GuildScores[c.Player.MyGuild.GuildName]++;
            }
           
            foreach (var p in GuildScores.OrderByDescending(e => e.Value).Take(10))
                score.Add(p.Key + " : " + p.Value + ".");
            if (GuildScores.Count <= 1)
            {
                foreach (var c in Database.Server.GamePoll.Values.Where(e => e.Player.Map == map && e.Player.Alive))
                    c.Teleport(428, 378, 1002);
                string winnerG = GuildScores.Take(1).SingleOrDefault().Key;
                if (winnerG != null
                    && PrizesClaimed != null)
                {
                    if (!PrizesClaimed.ContainsKey(winnerG))
                        PrizesClaimed.Add(winnerG, 0);
                    PrizesClaimed[winnerG]++;
                    using (var rec = new ServerSockets.RecycledPacket())
                    {
                        var stream = rec.GetStream();
                        Program.SendGlobalPackets.Enqueue(
                     new Game.MsgServer.MsgMessage(winnerG + " has won the Guilds Death Match, and won 100 CPs!", "ALLUSERS", "[EVENT]", Game.MsgServer.MsgMessage.MsgColor.white, (Game.MsgServer.MsgMessage.ChatMode)2500).GetArray(stream));
                        // End Event
                    }
                }
            }
            if (DateTime.Now > lastSent.AddSeconds(5))
            {
                SendScore(score);
                lastSent = DateTime.Now;
            }
        }
        public void SendScore(List<string> text)
        {
            using (var rec = new ServerSockets.RecycledPacket())
            {
                var stream = rec.GetStream();
                foreach (var C in Database.Server.GamePoll.Values.Where(e => e.Player.Map == map))
                {
                    C.Send(new Game.MsgServer.MsgMessage("Guilds - Information", "ALLUSERS", "SYSTEM", Game.MsgServer.MsgMessage.MsgColor.white, Game.MsgServer.MsgMessage.ChatMode.FirstRightCorner).GetArray(stream));
                    C.Send(new Game.MsgServer.MsgMessage("Total players of guilds in map : " + GuildScores.Count, "ALLUSERS", "SYSTEM", Game.MsgServer.MsgMessage.MsgColor.white, Game.MsgServer.MsgMessage.ChatMode.ContinueRightCorner).GetArray(stream));
                    foreach (string t in text)
                        C.Send(new Game.MsgServer.MsgMessage(t, "ALLUSERS", "SYSTEM", Game.MsgServer.MsgMessage.MsgColor.white, Game.MsgServer.MsgMessage.ChatMode.ContinueRightCorner).GetArray(stream));
                }
            }
        }
    }
}
