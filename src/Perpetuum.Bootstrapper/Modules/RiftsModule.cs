using Autofac;
using Perpetuum.Data;
using Perpetuum.Services.RiftSystem;
using Perpetuum.Services.RiftSystem.StrongholdRifts;
using Perpetuum.Zones;
using System;
using System.Collections.Generic;

namespace Perpetuum.Bootstrapper.Modules
{
    internal class RiftsModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            _ = builder.Register<Func<IZone, RiftSpawnPositionFinder>>(x =>
            {
                return zone =>
                {
                    return zone.Configuration.Terraformable ? new PvpRiftSpawnPositionFinder(zone) : (RiftSpawnPositionFinder)new PveRiftSpawnPositionFinder(zone);
                };
            });

            _ = builder.RegisterType<RiftManager>();
            _ = builder.RegisterType<StrongholdRiftManager>();

            _ = builder.Register<Func<IZone, IRiftManager>>(x =>
            {
                IComponentContext ctx = x.Resolve<IComponentContext>();
                return zone =>
                {
                    if (zone is TrainingZone)
                    {
                        return null;
                    }

                    if (zone is StrongHoldZone)
                    {

                        int strongHoldExitConfigCount = Db.Query().CommandText("SELECT COUNT(*) FROM strongholdexitconfig WHERE zoneid = @zoneId;")
                        .SetParameter("@zoneId", zone.Id)
                        .ExecuteScalar<int>();
                        return strongHoldExitConfigCount < 1 ? null : (IRiftManager)ctx.Resolve<StrongholdRiftManager>(new TypedParameter(typeof(IZone), zone));
                    }


                    List<System.Data.IDataRecord> zoneConfigs = Db.Query().CommandText("SELECT maxrifts FROM zoneriftsconfig WHERE zoneid = @zoneId")
                    .SetParameter("@zoneId", zone.Id)
                    .Execute();
                    if (zoneConfigs.Count < 1)
                    {
                        return null;
                    }

                    System.Data.IDataRecord record = zoneConfigs[0];
                    int maxrifts = record.GetValue<int>("maxrifts");

                    if (maxrifts < 1)
                    {
                        return null;
                    }

                    TimeRange spawnTime = TimeRange.FromLength(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5));
                    RiftSpawnPositionFinder finder = ctx.Resolve<Func<IZone, RiftSpawnPositionFinder>>().Invoke(zone);
                    return ctx.Resolve<RiftManager>(new TypedParameter(typeof(IZone), zone), new NamedParameter("spawnTime", spawnTime), new NamedParameter("spawnPositionFinder", finder));
                };
            });
        }
    }
}
