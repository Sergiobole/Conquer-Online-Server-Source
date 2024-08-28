using COServer.Client;
using System.Collections.Generic;
using System.Linq;

namespace COServer.Game.MsgServer
{
    public class SurpriseBox
    {
        static List<uint> High = new List<uint>()
        {
            720611,723863,723860,723864,723865,720028,720027,722057,711083,723094,1200000,723727,
            181955, 182635, 191305,200005,754999,753999,753003,753001,751999,751001,751003,730006
        };
        static List<uint> Mid = new List<uint>()
        {
            720049,723342,723342,753999,753099,754999,780001,720027,722057,711083,723094,1200000,1200001,1200002,1100009,1100006,200312,200311,200310,
            181955, 182635, 191305,200005,754999,753999,753003,753001,751999,751001,751003,730006
        };
       
        public static void GetReward(GameClient client, ServerSockets.Packet stream)
        {
            if (Role.MyMath.Success(10))
            {
                var reward = Mid[Role.Core.Random.Next(0, Mid.Count)];
                client.Inventory.Add(stream, reward, 1);
                client.SendSysMesage("You've received a nice reward, check your inventory!");
            }
            else
            {
                var reward = High[Role.Core.Random.Next(0, High.Count)];
                client.Inventory.Add(stream, reward, 1);
                client.SendSysMesage("You've received a nice reward, check your inventory!");
            }
        }
    }
}
