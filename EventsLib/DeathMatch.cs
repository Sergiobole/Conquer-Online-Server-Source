using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace COServer.EventsLib
{
    public class DeathMatch : BaseEvent
    {
        public bool isChosen = false;
        public DeathMatch()
            : base(8505, "DeathMatch", 100, Game.MsgServer.MsgStaticMessage.Messages.DeathMatch)
        {
        }
        DateTime lastSent = DateTime.Now;
        List<string> score = new List<string>();
        public int WhiteTeam = 0, RedTeam = 0, BlueTeam = 0, BlackTeam = 0;
        public override void worker()
        {
            base.worker();
            score.Clear();
            score.Add("BlackTeam : " + BlackTeam);
            score.Add("BlueTeam : " + BlueTeam);
            score.Add("RedTeam : " + RedTeam);
            score.Add("WhiteTeam : " + WhiteTeam);
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
                    C.Send(new Game.MsgServer.MsgMessage("DeathMatch - Scores", "ALLUSERS", "SYSTEM", Game.MsgServer.MsgMessage.MsgColor.white, Game.MsgServer.MsgMessage.ChatMode.FirstRightCorner).GetArray(stream));
                    C.Send(new Game.MsgServer.MsgMessage("TEAM : " + C.DMTeamString(), "ALLUSERS", "SYSTEM", Game.MsgServer.MsgMessage.MsgColor.white, Game.MsgServer.MsgMessage.ChatMode.ContinueRightCorner).GetArray(stream));
                    foreach (string t in text)
                        C.Send(new Game.MsgServer.MsgMessage(t, "ALLUSERS", "SYSTEM", Game.MsgServer.MsgMessage.MsgColor.white, Game.MsgServer.MsgMessage.ChatMode.ContinueRightCorner).GetArray(stream));
                }
            }
        }
        public byte NextTeam()
        {
            int nextid = ++TeamNow;
            if (nextid % 4 == 0)
                return 4;
            else if (nextid % 3 == 0)
                return 3;
            else if (nextid % 2 == 0)
                return 2;
            else return 1;
        }
        public uint[] garments = { 181325, 181625, 181825, 181525 };
        int TeamNow = 0;
    }
}
