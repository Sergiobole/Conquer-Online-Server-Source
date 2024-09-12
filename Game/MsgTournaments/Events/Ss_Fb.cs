using COServer.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace COServer.Game.MsgTournaments
{
    public class Ss_Fb
    {
        public const int FinishMinutes = 9;
        public const uint Map = 1005;
        private ProcesType Mode;
        private DateTime FinishTimer = new DateTime();
        private string Title = "Ss_Fb";
        public uint WinnerUID = 0;
        DateTime lastSent = DateTime.Now;
        List<string> score = new List<string>();
        public Ss_Fb()
        {
            Mode = ProcesType.Dead;
            if (!Program.OutMap.Contains(Map))
                Program.OutMap.Add(Map);
            if (!Program.FreePkMap.Contains(Map))
                Program.FreePkMap.Add(Map);
            if (!Program.NoDrugMap.Contains(Map))
                Program.NoDrugMap.Add(Map);
            if (!Program.SsFbMap.Contains(Map))
                Program.SsFbMap.Add(Map);
        }
        public void Open()
        {
            if (Mode == ProcesType.Dead)
            {
                Mode = ProcesType.Alive;
                MsgSchedules.SendInvitation("Ss_Fb", 442, 355, 1002, 0, 60, Game.MsgServer.MsgStaticMessage.Messages.Ss_Fb);
                MsgSchedules.SendSysMesage("" + Title + " has started!", MsgServer.MsgMessage.ChatMode.Center, MsgServer.MsgMessage.MsgColor.red);

                FinishTimer = DateTime.Now.AddMinutes(FinishMinutes);
            }
        }
        public bool AllowJoin(Client.GameClient user, ServerSockets.Packet stream)
        {
            if (Mode == ProcesType.Alive)
            {
                ushort x = 0;
                ushort y = 0;
                Server.ServerMaps[Map].GetRandCoord(ref x, ref y);
                user.Teleport(x, y, Map, 9999);
                user.Player.HitPoints = 1;
                return true;
            }
            return false;
        }
        public void CheckUp()
        {
            if (Mode == ProcesType.Alive)
            {
                score.Clear();
                int rank = 0;
                foreach (var player in Server.GamePoll.Values
                    .Where(e => e.Player.Map == Map && e.Player.DynamicID == 9999)
                    .OrderByDescending(e => e.Player.Ss_Fb_Hits).Take(5))
                {
                    if (!player.Player.Alive && DateTime.Now > player.DeathHit.AddSeconds(5))
                    {
                        using (var rec = new ServerSockets.RecycledPacket())
                        {
                            var stream = rec.GetStream();
                            if (!MsgSchedules._Ss_Fb.AllowJoin(player, stream))
                            {
                                player.Player.Revive(stream);
                                player.Teleport(428, 378, 1002);
                            }
                        }
                    }
                    rank++;
                    score.Add(player.Player.Name + " : " + player.Player.Ss_Fb_Hits + " hits.");
                    if (DateTime.Now > FinishTimer)
                    {
                        if (rank == 1)
                        {
                            using (var rec = new ServerSockets.RecycledPacket())
                            {
                                var stream = rec.GetStream();
                                GiveReward(player, stream);
                            }
                        }
                    }
                }
                if (DateTime.Now > lastSent.AddSeconds(2))
                {
                    SendScore(score);
                    lastSent = DateTime.Now;
                }
                if (DateTime.Now > FinishTimer)
                {
                    Mode = ProcesType.Dead;
                }
            }
            if (DateTime.Now.Minute == 41 && DateTime.Now.Second < 2)
            {
                Open();
            }
        }
        public bool IsFinished() { return Mode == ProcesType.Dead; }
        public bool TheLastPlayer()
        {
            return Server.GamePoll.Values.Where(p => p.Player.Map == Map && p.Player.DynamicID == 9999 && p.Player.Alive).Count() == 1;
        }
        public void GiveReward(Client.GameClient client, ServerSockets.Packet stream)
        {
            client.SendSysMesage("You received a DragonBallScroll.", MsgServer.MsgMessage.ChatMode.System, MsgServer.MsgMessage.MsgColor.red);
            MsgSchedules.SendSysMesage("" + client.Player.Name + " has won " + Title + " and received a DragonBallScroll!", MsgServer.MsgMessage.ChatMode.TopLeftSystem, MsgServer.MsgMessage.MsgColor.white);
            client.Inventory.Add(stream, 720028, 1, 0, 0, 0, 0, 0, false); // Atualizado para adicionar DragonBallScroll
            client.Player.HitPoints = (int)client.Status.MaxHitpoints;
            client.Teleport(428, 378, 1002);
        }
        public void SendScore(List<string> text)
        {
            using (var rec = new ServerSockets.RecycledPacket())
            {
                var stream = rec.GetStream();
                foreach (var C in Server.GamePoll.Values.Where(e => e.Player.Map == Map && e.Player.DynamicID == 9999))
                {
                    C.Send(new Game.MsgServer.MsgMessage("Ss_Fb - Hits", "ALLUSERS", "SYSTEM", Game.MsgServer.MsgMessage.MsgColor.red, Game.MsgServer.MsgMessage.ChatMode.FirstRightCorner).GetArray(stream));
                    C.Send(new Game.MsgServer.MsgMessage("My Hits : " + C.Player.Ss_Fb_Hits, "ALLUSERS", "SYSTEM", Game.MsgServer.MsgMessage.MsgColor.red, Game.MsgServer.MsgMessage.ChatMode.ContinueRightCorner).GetArray(stream));
                    foreach (string t in text)
                        C.Send(new Game.MsgServer.MsgMessage(t, "ALLUSERS", "SYSTEM", Game.MsgServer.MsgMessage.MsgColor.red, Game.MsgServer.MsgMessage.ChatMode.ContinueRightCorner).GetArray(stream));
                }
            }
        }
    }
}
