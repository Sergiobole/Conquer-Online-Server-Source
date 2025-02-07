using COServer.Game.MsgServer;
using System;
using System.Collections.Concurrent;

namespace COServer.Role.Instance
{
    public class Vendor
    {
        public static Counter VendorCounter = new Counter(100000);

        public const byte MaxItems = 20;

        public class VendorItem
        {
            public Game.MsgServer.MsgItemView.ActionMode CostType;
            public Game.MsgServer.MsgGameItem DataItem;
            public uint AmountCost;


        }
        public Client.GameClient Owner;
        public ConcurrentDictionary<uint, VendorItem> Items;
        public Game.MsgServer.MsgMessage HalkMeesaje = null;
        public SobNpc VendorNpc;
        public uint VendorUID;

        public bool InVending;

        public Vendor(Client.GameClient client)
        {
            Items = new ConcurrentDictionary<uint, VendorItem>();
            Owner = client;
        }
        public unsafe void CreateVendor(ServerSockets.Packet stream)
        {
            if (InVending) return;

            VendorUID = VendorCounter.Next;

            VendorNpc = new SobNpc();
            VendorNpc.ObjType = MapObjectType.SobNpc;
            VendorNpc.OwnerVendor = Owner;
            VendorNpc.Name = Owner.Player.Name;
            VendorNpc.UID = VendorUID;
            VendorNpc.Mesh = SobNpc.StaticMesh.Vendor;
            VendorNpc.Type = Flags.NpcType.Booth;
            VendorNpc.Map = Owner.Player.Map;
            VendorNpc.X = (ushort)(Owner.Player.X + 1);
            VendorNpc.Y = Owner.Player.Y;

            Owner.Map.View.EnterMap<Role.IMapObj>(VendorNpc);

            foreach (var IObj in Owner.Player.View.Roles(MapObjectType.Player))
            {
                Role.Player screenObj = IObj as Role.Player;
                screenObj.View.CanAdd(VendorNpc, true, stream);
            }
            Owner.Player.Send(VendorNpc.GetArray(stream, false));
            InVending = true;
        }
        public unsafe void StopVending(ServerSockets.Packet stream)
        {
            if (InVending)
            {

                ActionQuery actione = new ActionQuery()
                {
                    ObjId = VendorUID,
                    Type = ActionType.RemoveEntity
                };
                Owner.Player.View.SendView(stream.ActionCreate(&actione), true);

                Items.Clear();
                Owner.Map.View.LeaveMap<Role.IMapObj>(VendorNpc);
                InVending = false;
                Owner.MyVendor = null;
            }
        }
        public bool AddItem(Game.MsgServer.MsgGameItem DataItem, Game.MsgServer.MsgItemView.ActionMode CostType, uint Amout)
        {
            if (DataItem.Bound == 1 || DataItem.Locked != 0 || DataItem.ITEM_ID == 750000)
                return false;

            if (Items.Count == MaxItems)
                return false;
            if (!Items.ContainsKey(DataItem.UID))
            {
               
                VendorItem VItem = new VendorItem();
                VItem.DataItem = DataItem;
                VItem.CostType = CostType;
                VItem.AmountCost = Amout;
                Items.TryAdd(DataItem.UID, VItem);

                string itemName = Database.Server.ItemsBase.GetItemName(DataItem.ITEM_ID);
                VendorDiscordNotifier.AddItem(Owner.Player.Name, itemName, Amout, DataItem.Plus);

                return true;
            }
            return false;
        }
    }
}
