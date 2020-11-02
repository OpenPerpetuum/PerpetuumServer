using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Host.Requests;
using Perpetuum.Services.MissionEngine.MissionDataCacheObjects;
using Perpetuum.Services.MissionEngine.MissionProcessorObjects;
using Perpetuum.Units.FieldTerminals;

namespace Perpetuum.RequestHandlers.Missions
{
    public class MissionGetOptions : IRequestHandler
    {
        private readonly MissionProcessor _missionProcessor;
        private readonly MissionDataCache _missionDataCache;
        private readonly IEntityRepository _entityRepository;

        public MissionGetOptions(MissionProcessor missionProcessor,MissionDataCache missionDataCache, IEntityRepository entityRepository)
        {
            _missionProcessor = missionProcessor;
            _missionDataCache = missionDataCache;
            _entityRepository = entityRepository;
        }

        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            long locationEid;

            FieldTerminal terminal = null;

            if (character.IsDocked)
            {
                //the base we the player is docked
                locationEid = character.CurrentDockingBaseEid;

            }
            else
            {
                //use the optional parameter
                locationEid = request.Data.GetOrDefault<long>(k.eid);

                // Load Entity to see if it's a custom field terminal
                terminal = (FieldTerminal)_entityRepository.Load(locationEid).ThrowIfNull(ErrorCodes.TargetNotFound);
            }

            // Don't show missions for rally terminals
            if (terminal != null && terminal.ED.Name == DefinitionNames.FIELD_TERMINAL_RALLY) {
                throw PerpetuumException.Create(ErrorCodes.MissionNotAvailable);
            } else {
                var location = _missionDataCache.GetLocationByEid(locationEid).ThrowIfNull(ErrorCodes.ItemNotFound);
                _missionProcessor.GetOptionsByRequest(character, location);
            }
            
        }
    }
}