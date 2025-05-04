using Perpetuum.Accounting.Characters;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Players;
using Perpetuum.Robots;
using Perpetuum.Zones.Blobs.BlobEmitters;
using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Zones.ProximityProbes
{
    public class ProximityProbe : ProximityDeviceBase, IBlobEmitter
    {
        protected internal override void UpdatePlayerVisibility(Player player)
        {
            UpdateVisibility(player);
        }

        public override List<Robot> GetNoticedUnits()
        {
            return GetVisibleUnits()
                .Select(v => v.Target)
                .OfType<Player>()
                .Cast<Robot>()
                .ToList();
        }

        protected override bool IsActive
        {
            get
            {
                double coreRatio = Core.Ratio(CoreMax);
                return coreRatio > 0.98;
            }
        }

        public double BlobEmission
        {
            get
            {
                Items.ItemPropertyModifier blobEmission = GetPropertyModifier(AggregateField.blob_emission);
                return blobEmission.Value;
            }
        }

        public double BlobEmissionRadius
        {
            get
            {
                Items.ItemPropertyModifier blobEmissionRadius = GetPropertyModifier(AggregateField.blob_emission_radius);
                return blobEmissionRadius.Value;
            }
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
            {
                base.AcceptVisitor(visitor);
            }
        }

        public Dictionary<string, object> CreateInfoDictionaryForProximityProbe(List<Player> unitsFound)
        {
            Dictionary<string, object> infoDict = GetProbeInfo(false);

            Dictionary<string, object> unitsInfo = unitsFound.ToDictionary("c", p =>
            {
                return new Dictionary<string, object>
                {
                    {k.characterID, p.Character.Id},
                    {k.x, p.CurrentPosition.X},
                    {k.y, p.CurrentPosition.Y}
                };
            });

            infoDict.Add(k.units, unitsInfo);

            return infoDict;
        }

        public override void OnUnitsFound(List<Player> unitsFound)
        {
            //itt lehet mindenfele, pl most kuldunk egy kommandot amire a kliens terkepet frissit

            if (unitsFound.Count <= 0)
            {
                return;
            }

            Character[] registerdCharacters = GetRegisteredCharacters();

            if (registerdCharacters.Length <= 0)
            {
                return;
            }

            Dictionary<string, object> infoDict = CreateInfoDictionaryForProximityProbe(unitsFound);

            Message.Builder.SetCommand(Commands.ProximityProbeInfo).WithData(infoDict).ToCharacters(registerdCharacters).Send();
        }

        public override void OnUnitsFound(List<Robot> unitsFound)
        {
            throw new System.NotImplementedException();
        }
    }
}