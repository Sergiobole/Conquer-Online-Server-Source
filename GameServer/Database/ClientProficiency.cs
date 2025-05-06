﻿namespace COServer.Database
{
    public class ClientProficiency
    {
        public struct DBProf
        {
            public uint ID;
            public uint Level;
            public byte PreviouseLevel;
            public uint Experience;
            public uint UID;

            public DBProf GetDBSpell(Game.MsgServer.MsgProficiency prof)
            {
                ID = prof.ID;
                Level = prof.Level;
                PreviouseLevel = prof.PreviouseLevel;
                Experience = prof.Experience;
                UID = prof.UID;
                return this;
            }
            public Game.MsgServer.MsgProficiency GetClientProf()
            {
                Game.MsgServer.MsgProficiency prof = new Game.MsgServer.MsgProficiency();
                prof.ID = ID;
                prof.Level = Level;
                prof.PreviouseLevel = PreviouseLevel;
                prof.Experience = Experience;
                prof.UID = UID;
                return prof;
            }

        }
    }
}
