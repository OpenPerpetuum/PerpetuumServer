using Perpetuum.Host.Requests;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers
{
    public class ZoneSaveLayer : IRequestHandler
    {
        private readonly IZoneManager _zoneManager;
        private readonly ILayerFileIO _layerFileIO;

        public ZoneSaveLayer(IZoneManager zoneManager, ILayerFileIO layerFileIO)
        {
            _zoneManager = zoneManager;
            _layerFileIO = layerFileIO;
        }

        public void HandleRequest(IRequest request)
        {
            if (request.Data.ContainsKey(k.zoneID))
            {
                var zoneToSave = request.Data.GetOrDefault<int>(k.zoneID);
                _zoneManager.ContainsZone(zoneToSave).ThrowIfFalse(ErrorCodes.ZoneNotFound);
                var zone = _zoneManager.GetZone(zoneToSave);
                _layerFileIO.SaveLayerToDisk(zone, zone.Terrain.Altitude);
                _layerFileIO.SaveLayerToDisk(zone, zone.Terrain.Blocks);
                _layerFileIO.SaveLayerToDisk(zone, zone.Terrain.Controls);
                _layerFileIO.SaveLayerToDisk(zone, zone.Terrain.Plants);
            }
            else
            {
                foreach (var zone in _zoneManager.Zones)
                {
                    _layerFileIO.SaveLayerToDisk(zone, zone.Terrain.Altitude);
                    _layerFileIO.SaveLayerToDisk(zone, zone.Terrain.Blocks);
                    _layerFileIO.SaveLayerToDisk(zone, zone.Terrain.Controls);
                    _layerFileIO.SaveLayerToDisk(zone, zone.Terrain.Plants);
                }
            }
            Message.Builder.FromRequest(request).WithOk().Send();
        }
    }
}