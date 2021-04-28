using Perpetuum.Data;
using Perpetuum.ExportedTypes;
using Perpetuum.Players;
using Perpetuum.Units;
using Perpetuum.Zones;
using System;

namespace Perpetuum.Services.Strongholds
{
    public interface IStrongholdPlayerStateManager
    {
        void OnPlayerEnterZone(Player player);
        void OnPlayerExitZone(Player player);
    }

    public class StrongholdPlayerStateManager : IStrongholdPlayerStateManager
    {
        private readonly TimeSpan MAX = TimeSpan.FromMinutes(60);
        private readonly TimeSpan MIN = TimeSpan.FromSeconds(3);
        private readonly IZone _zone;

        public StrongholdPlayerStateManager(IZone zone)
        {
            _zone = zone;
            MAX = TimeSpan.FromMinutes(_zone.Configuration.TimeLimitMinutes ?? 60);
        }

        public void OnPlayerEnterZone(Player player)
        {
            var effectEnd = player.DynamicProperties.GetOrAdd(k.strongholdDespawnTime, DateTime.UtcNow.Add(MAX));
            var effectDuration = (effectEnd - DateTime.UtcNow).Max(MIN);
            ApplyDespawn(player, effectDuration);
        }

        public void OnPlayerExitZone(Player player)
        {
            player.ClearStrongholdDespawn();
            player.DynamicProperties.Remove(k.strongholdDespawnTime);
        }

        private void ApplyDespawn(Player player, TimeSpan remaining)
        {
            player.DynamicProperties.Set(k.strongholdDespawnTime, DateTime.UtcNow.Add(remaining));
            player.SetStrongholdDespawn(remaining, (u) =>
            {
                using (var scope = Db.CreateTransaction())
                {
                    if (u is Player p)
                    {
                        var dockingBase = p.Character.GetHomeBaseOrCurrentBase();
                        p.DockToBase(dockingBase.Zone, dockingBase);
                        p.DynamicProperties.Remove(k.strongholdDespawnTime);
                    }
                    scope.Complete();
                }
            });
        }
    }

    public class StrongholdPlayerDespawnHelper : UnitDespawnHelper
    {
        private static readonly EffectType DespawnEffect = EffectType.effect_despawn_timer; //TODO new custom type

        private StrongholdPlayerDespawnHelper(TimeSpan despawnTime) : base(despawnTime) { }

        private bool _canceled = false;
        public void Cancel(Unit unit)
        {
            _canceled = true;
            RemoveDespawnEffect(unit);
        }

        public static bool HasEffect(Unit unit)
        {
            return unit.EffectHandler.ContainsEffect(DespawnEffect);
        }

        public override void Update(TimeSpan time, Unit unit)
        {
            _timer.Update(time).IsPassed(() =>
            {
                if (_canceled || HasEffect(unit))
                    return;

                DespawnStrategy?.Invoke(unit);
            });
        }

        private void RemoveDespawnEffect(Unit unit)
        {
            unit.EffectHandler.RemoveEffectByToken(_effectToken);
        }

        private void ApplyDespawnEffect(Unit unit)
        {
            var effectBuilder = unit.NewEffectBuilder().SetType(DespawnEffect).WithDuration(_despawnTime).WithToken(_effectToken);
            unit.ApplyEffect(effectBuilder);
        }

        public new static StrongholdPlayerDespawnHelper Create(Unit unit, TimeSpan despawnTime)
        {
            var helper = new StrongholdPlayerDespawnHelper(despawnTime);
            helper.ApplyDespawnEffect(unit);
            return helper;
        }
    }
}



