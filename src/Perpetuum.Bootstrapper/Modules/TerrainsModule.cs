using Autofac;
using Perpetuum.Services.EventServices;
using Perpetuum.Services.EventServices.EventProcessors.NpcSpawnEventHandlers;
using Perpetuum.Threading.Process;
using Perpetuum.Zones;
using Perpetuum.Zones.NpcSystem.Reinforcements;
using Perpetuum.Zones.Scanning.Scanners;
using Perpetuum.Zones.Terrains;
using Perpetuum.Zones.Terrains.Materials;
using Perpetuum.Zones.Terrains.Materials.Minerals;
using Perpetuum.Zones.Terrains.Materials.Minerals.Generators;
using Perpetuum.Zones.Terrains.Materials.Plants;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Bootstrapper.Modules
{
    internal class TerrainsModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            _ = builder.Register<Func<IZone, IEnumerable<IMaterialLayer>>>(x =>
            {
                IComponentContext ctx = x.Resolve<IComponentContext>();
                return zone =>
                {
                    IMineralConfigurationReader reader = ctx.Resolve<IMineralConfigurationReader>();
                    OreNpcSpawner listener = new OreNpcSpawner(zone, ctx.Resolve<INpcReinforcementsRepository>(), reader);
                    EventListenerService eventListenerService = ctx.Resolve<EventListenerService>();
                    eventListenerService.AttachListener(listener);
                    if (zone is TrainingZone)
                    {
                        GravelRepository repo = ctx.Resolve<GravelRepository>();
                        GravelConfiguration config = new GravelConfiguration(zone);
                        GravelLayer layer = new GravelLayer(zone.Size.Width, zone.Size.Height, config, repo);
                        layer.LoadMineralNodes();

                        return new[] { layer };
                    }

                    MineralNodeGeneratorFactory nodeGeneratorFactory = new MineralNodeGeneratorFactory(zone);

                    List<IMaterialLayer> materialLayers = new List<IMaterialLayer>();

                    foreach (IMineralConfiguration configuration in reader.ReadAll().Where(c => c.ZoneId == zone.Id))
                    {
                        MineralNodeRepository repo = new MineralNodeRepository(zone, configuration.Type);
                        switch (configuration.ExtractionType)
                        {
                            case MineralExtractionType.Solid:
                                {
                                    OreLayer layer = new OreLayer(zone.Size.Width, zone.Size.Height, configuration, repo, nodeGeneratorFactory, eventListenerService);
                                    layer.LoadMineralNodes();
                                    materialLayers.Add(layer);

                                    break;
                                }
                            case MineralExtractionType.Liquid:
                                {
                                    LiquidLayer layer = new LiquidLayer(zone.Size.Width, zone.Size.Height, configuration, repo, nodeGeneratorFactory, eventListenerService);
                                    layer.LoadMineralNodes();
                                    materialLayers.Add(layer);

                                    break;
                                }
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }

                    return materialLayers;
                };
            });

            _ = builder.RegisterType<Scanner>();
            _ = builder.RegisterType<MaterialHelper>().SingleInstance();

            _ = builder.RegisterType<GravelRepository>();
            _ = builder.RegisterType<LayerFileIO>().As<ILayerFileIO>();
            _ = builder.RegisterType<Terrain>();
            _ = builder.RegisterGeneric(typeof(IntervalLayerSaver<>)).InstancePerDependency();

            _ = builder.Register<TerrainFactory>(x =>
            {
                IComponentContext ctx = x.Resolve<IComponentContext>();
                return zone =>
                {
                    Terrain terrain = ctx.Resolve<Terrain>();

                    System.Drawing.Size size = zone.Configuration.Size;

                    ILayerFileIO loader = ctx.Resolve<ILayerFileIO>();

                    BlockingInfo[] blocks = loader.Load<BlockingInfo>(zone, LayerType.Blocks);
                    terrain.Blocks = new Layer<BlockingInfo>(LayerType.Blocks, blocks, size.Width, size.Height);

                    TerrainControlInfo[] controls = loader.Load<TerrainControlInfo>(zone, LayerType.Control);
                    terrain.Controls = new Layer<TerrainControlInfo>(LayerType.Control, controls, size.Width, size.Height);

                    PlantInfo[] plants = loader.Load<PlantInfo>(zone, LayerType.Plants);
                    terrain.Plants = new Layer<PlantInfo>(LayerType.Plants, plants, size.Width, size.Height);

                    ushort[] altitude = loader.Load<ushort>(zone, LayerType.Altitude);
                    AltitudeLayer altitudeLayer;

                    if (zone.Configuration.Terraformable)
                    {
                        Layer<ushort> originalAltitude = new Layer<ushort>(LayerType.OriginalAltitude, altitude, size.Width, size.Height);
                        ushort[] blend = loader.LoadLayerData<ushort>(zone, "altitude_blend");
                        Layer<ushort> blendLayer = new Layer<ushort>(LayerType.Blend, blend, size.Width, size.Height);
                        altitudeLayer = new TerraformableAltitude(originalAltitude, blendLayer, altitude);
                    }
                    else
                    {
                        altitudeLayer = new AltitudeLayer(altitude, size.Width, size.Height);
                    }

                    terrain.Altitude = altitudeLayer;
                    terrain.Slope = new SlopeLayer(altitudeLayer);

                    if (!zone.Configuration.Terraformable)
                    {
                        PassableMapBuilder b = new PassableMapBuilder(terrain.Blocks, terrain.Slope, zone.GetPassablePositionFromDb());
                        terrain.Passable = b.Build();
                    }

                    terrain.Materials = ctx.Resolve<Func<IZone, IEnumerable<IMaterialLayer>>>().Invoke(zone).ToDictionary(m => m.Type);

                    CompositeProcess layerSavers = new CompositeProcess();
                    layerSavers.AddProcess(ctx.Resolve<IntervalLayerSaver<BlockingInfo>.Factory>().Invoke(terrain.Blocks, zone));
                    layerSavers.AddProcess(ctx.Resolve<IntervalLayerSaver<TerrainControlInfo>.Factory>().Invoke(terrain.Controls, zone));
                    layerSavers.AddProcess(ctx.Resolve<IntervalLayerSaver<PlantInfo>.Factory>().Invoke(terrain.Plants, zone));
                    layerSavers.AddProcess(ctx.Resolve<IntervalLayerSaver<ushort>.Factory>().Invoke(terrain.Altitude, zone));

                    ctx.Resolve<IProcessManager>().AddProcess(layerSavers.ToAsync().AsTimed(TimeSpan.FromHours(2)));
                    ctx.Resolve<IProcessManager>().AddProcess(terrain.Materials.Values.OfType<IProcess>().ToCompositeProcess().ToAsync().AsTimed(TimeSpan.FromMinutes(2)));

                    return terrain;
                };
            });
        }
    }
}
