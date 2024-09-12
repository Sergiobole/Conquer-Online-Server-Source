﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace COServer.Game.MsgTournaments
{
    public class ConquerPk
    {
        public const uint Map = 602;
        private ProcesType Mode;
        private DateTime FinishTimer = new DateTime();
        private string Title = "Top_Conquer";
        public uint WinnerUID = 0;
        public ConquerPk()
        {
            Mode = ProcesType.Dead;
            if (!Program.OutMap.Contains(Map))
                Program.OutMap.Add(Map);
        }
        public void Open()
        {
            if (Mode == ProcesType.Dead)
            {
                FinishTimer = DateTime.Now.AddMinutes(1);
                Mode = ProcesType.Alive;
                MsgSchedules.SendInvitation("Top_Conquer", 473, 356, 1002, 0, 60, Game.MsgServer.MsgStaticMessage.Messages.TopConquer);
                MsgSchedules.SendSysMesage("" + Title + " has started!", MsgServer.MsgMessage.ChatMode.Center, MsgServer.MsgMessage.MsgColor.red);
                if (!Program.FreePkMap.Contains(Map))
                    Program.FreePkMap.Add(Map);
            }
        }
        public void CheckUp()
        {
            if (Mode == ProcesType.Alive)
            {
                if (DateTime.Now > FinishTimer)
                {
                    Mode = ProcesType.Dead;
                }
            }
            if (DateTime.Now.Hour == 5 && DateTime.Now.Minute == 00 && DateTime.Now.Second < 2)
            {
                if (Mode == ProcesType.Dead)
                {
                    FinishTimer = DateTime.Now.AddMinutes(1);
                    Mode = ProcesType.Alive;
                    MsgSchedules.SendInvitation("Top_Conquer", 473, 356, 1002, 0, 60, Game.MsgServer.MsgStaticMessage.Messages.TopConquer);
                    MsgSchedules.SendSysMesage("" + Title + " has started!", MsgServer.MsgMessage.ChatMode.Center, MsgServer.MsgMessage.MsgColor.red);
                    if (!Program.FreePkMap.Contains(Map))
                        Program.FreePkMap.Add(Map);
                }
            }
        }
        public bool AllowJoin(Client.GameClient user, ServerSockets.Packet stream)
        {
            if (Mode == ProcesType.Alive)
            {
                ushort x = 0;
                ushort y = 0;
                Database.Server.ServerMaps[Map].GetRandCoord(ref x, ref y);
                user.Teleport(x, y, Map);
                return true;
            }
            return false;
        }
        public bool IsFinished() { return Mode == ProcesType.Dead; }
        public bool TheLastPlayer()
        {
            return Database.Server.GamePoll.Values.Where(p => p.Player.Map == Map && p.Player.Alive).Count() == 1;
        }
        public void GiveReward(Client.GameClient client, ServerSockets.Packet stream)
        {
            WinnerUID = client.Player.UID;
            client.SendSysMesage("You received a DragonBallScroll.", MsgServer.MsgMessage.ChatMode.System, MsgServer.MsgMessage.MsgColor.red);
            MsgSchedules.SendSysMesage("" + client.Player.Name + " has won " + Title + " and received a DragonBallScroll!", MsgServer.MsgMessage.ChatMode.TopLeftSystem, MsgServer.MsgMessage.MsgColor.white);
            client.Inventory.Add(stream, 720028, 1, 0, 0, 0, 0, 0, false); // Atualizado para adicionar DragonBallScroll
            client.Player.HitPoints = (int)client.Status.MaxHitpoints;
            client.Teleport(301, 278, 1002);
        }
        public void AddTop(Client.GameClient client)
        {
            if (WinnerUID == client.Player.UID)
                client.EffectStatus.Add("topconquer", DateTime.Now.AddHours(12));
            foreach (var user in Database.Server.GamePoll.Values)
            {
                if (user.Player.UID != WinnerUID)
                    client.EffectStatus.Remove("topconquer");
            }
        }
    }
}
