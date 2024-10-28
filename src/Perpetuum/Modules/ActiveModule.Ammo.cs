using Perpetuum.Containers;
using Perpetuum.EntityFramework;
using Perpetuum.Items.Ammos;
using Perpetuum.Players;
using Perpetuum.Zones;
using System.Linq;

namespace Perpetuum.Modules
{
    public partial class ActiveModule
    {
        private Ammo ammo;

        public bool IsAmmoable => ammoCategoryFlags > 0 && AmmoCapacity > 0;

        public int AmmoCapacity { get; private set; }

        private void InitAmmo()
        {
            AmmoCapacity = ED.Options.AmmoCapacity;
        }

        public void VisitAmmo(IEntityVisitor visitor)
        {
            Ammo ammo = GetAmmo();
            ammo?.AcceptVisitor(visitor);
        }

        [CanBeNull]
        public Ammo GetAmmo()
        {
            return !IsAmmoable ? null : ammo ?? (ammo = Children.OfType<Ammo>().FirstOrDefault());
        }

        public void SetAmmo(Ammo ammo)
        {
            if (!IsAmmoable)
            {
                return;
            }

            this.ammo = null;
            ClearChildren();

            if (ammo != null)
            {
                ammo.Owner = Owner;
                AddChild(ammo);
                ammo.Initialize();
            }

            SendAmmoUpdatePacketToPlayer();
        }

        protected void ConsumeAmmo()
        {
            if (!IsAmmoable || !ParentIsPlayer())
            {
                return;
            }

            Ammo ammo = GetAmmo();
            if (ammo == null)
            {
                return;
            }

            if (ammo.Quantity > 0)
            {
                ammo.Quantity--;
            }

            SendAmmoUpdatePacketToPlayer();
        }

        private void SendAmmoUpdatePacketToPlayer()
        {
            if (!(ParentRobot is Player player))
            {
                return;
            }

            Packet packet = new Packet(ZoneCommand.AmmoQty);
            packet.AppendLong(Eid);

            Ammo ammo = GetAmmo();
            if (ammo != null)
            {
                packet.AppendLong(ammo.Eid);
                packet.AppendInt(ammo.Definition);
                packet.AppendInt(ammo.Quantity);
            }
            else
            {
                packet.AppendLong(0L);
                packet.AppendInt(0);
                packet.AppendInt(0);
            }

            player.Session.SendPacket(packet);
        }

        public bool CheckLoadableAmmo(int ammoDefinition)
        {
            if (!IsAmmoable)
            {
                return false;
            }

            EntityDefault ammoEntityDefault = EntityDefault.GetOrThrow(ammoDefinition);

            return ammoEntityDefault.CategoryFlags.IsCategory(ammoCategoryFlags);
        }

        [CanBeNull]
        public Ammo UnequipAmmoToContainer(Container container)
        {
            Ammo ammo = GetAmmo();
            if (ammo != null)
            {
                if (ammo.Quantity > 0)
                {
                    container.AddItem(ammo, true);
                }
                else
                {
                    Repository.Delete(ammo);
                }
            }

            SetAmmo(null);

            return ammo;
        }
    }
}
