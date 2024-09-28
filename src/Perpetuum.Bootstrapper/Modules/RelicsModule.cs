using Autofac;
using Perpetuum.Data;
using Perpetuum.Services.Relics;
using Perpetuum.Zones;
using System;
using System.Collections.Generic;

namespace Perpetuum.Bootstrapper.Modules
{
    internal class RelicsModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            _ = builder.RegisterType<ZoneRelicManager>().As<IRelicManager>();

            _ = builder.Register<Func<IZone, IRelicManager>>(x =>
            {
                IComponentContext ctx = x.Resolve<IComponentContext>();
                return zone =>
                {
                    int numRelicConfigs = Db.Query().CommandText("SELECT id FROM relicspawninfo WHERE zoneid = @zoneId")
                    .SetParameter("@zoneId", zone.Id)
                    .Execute().Count;
                    if (numRelicConfigs < 1)
                    {
                        return null;
                    }

                    List<System.Data.IDataRecord> zoneConfigs = Db.Query().CommandText("SELECT maxspawn FROM reliczoneconfig WHERE zoneid = @zoneId")
                    .SetParameter("@zoneId", zone.Id)
                    .Execute();
                    if (zoneConfigs.Count < 1)
                    {
                        return null;
                    }
                    System.Data.IDataRecord record = zoneConfigs[0];
                    int maxspawn = record.GetValue<int>("maxspawn");
                    if (maxspawn < 1)
                    {
                        return null;
                    }
                    //Do not register RelicManagers on zones without the necessary valid entries in reliczoneconfig and relicspawninfo
                    return ctx.Resolve<IRelicManager>(new TypedParameter(typeof(IZone), zone));
                };
            });
        }
    }
}
