using Perpetuum.Accounting.Characters;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Groups.Corporations;
using Perpetuum.Log;
using Perpetuum.Players;
using Perpetuum.Robots;
using Perpetuum.Timers;
using Perpetuum.Units;
using Perpetuum.Units.DockingBases;
using Perpetuum.Zones.PBS;
using Perpetuum.Zones.Teleporting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Zones.ProximityProbes
{
    public interface ICharactersRegistered
    {
        Character[] GetRegisteredCharacters();
        void ReloadRegistration();
        int GetMaxRegisteredCount();
    }

    //ez van kinn a terepen
    public abstract class ProximityDeviceBase : Unit, ICharactersRegistered
    {
        private readonly CharactersRegisterHelper<ProximityDeviceBase> _charactersRegisterHelper;
        private IntervalTimer _probingInterval = new IntervalTimer(TimeSpan.FromSeconds(10));
        private UnitDespawnHelper _despawnHelper;

        protected ProximityDeviceBase()
        {
            _charactersRegisterHelper = new CharactersRegisterHelper<ProximityDeviceBase>(this);
        }

        public ICorporationManager CorporationManager { get; set; }

        public virtual void CheckDeploymentAndThrow(IZone zone, Position spawnPosition)
        {
            zone.Units.OfType<DockingBase>().WithinRange(spawnPosition, DistanceConstants.PROXIMITY_PROBE_DEPLOY_RANGE_FROM_BASE).Any().ThrowIfTrue(ErrorCodes.NotDeployableNearObject);
            zone.Units.OfType<Teleport>().WithinRange(spawnPosition, DistanceConstants.PROXIMITY_PROBE_DEPLOY_RANGE_FROM_TELEPORT).Any().ThrowIfTrue(ErrorCodes.TeleportIsInRange);
            zone.Units.OfType<ProximityDeviceBase>().WithinRange(spawnPosition, DistanceConstants.PROXIMITY_PROBE_DEPLOY_RANGE_FROM_PROBE).Any().ThrowIfTrue(ErrorCodes.TooCloseToOtherDevice);
        }

        public void SetDespawnTime(TimeSpan despawnTime)
        {
            _despawnHelper = UnitDespawnHelper.Create(this, despawnTime);
            _despawnHelper.DespawnStrategy = Kill;
        }

        // TODO: have to find better solution
        public UnitDespawnHelper GetDespawnHelper()
        {
            return _despawnHelper;
        }

        protected internal override void UpdatePlayerVisibility(Player player)
        {
            UpdateVisibility(player);
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
            {
                base.AcceptVisitor(visitor);
            }
        }

        protected override void OnUpdate(TimeSpan time)
        {
            base.OnUpdate(time);

            _ = _probingInterval.Update(time);

            if (!_probingInterval.Passed)
            {
                return;
            }

            _probingInterval.Reset();

            if (IsActive)
            {
                //detect
                List<Robot> robotsNearMe = GetNoticedUnits();

                //do something
                OnUnitsFound(robotsNearMe);
            }

            if (_despawnHelper == null)
            {
                Items.ItemPropertyModifier m = GetPropertyModifier(AggregateField.despawn_time);
                TimeSpan timespan = TimeSpan.FromMilliseconds((int)m.Value);
                SetDespawnTime(timespan);
            }

            _despawnHelper.Update(time, this);
        }

        protected virtual bool IsActive => true;

        #region registration 

        //ezt kell hivogatni requestbol, ha valtozott
        public void ReloadRegistration()
        {
            _charactersRegisterHelper.ReloadRegistration();
        }

        public Character[] GetRegisteredCharacters()
        {
            return _charactersRegisterHelper.GetRegisteredCharacters();
        }

        public int GetMaxRegisteredCount()
        {
            return _charactersRegisterHelper.GetMaxRegisteredCount();
        }

        #endregion

        #region probe functions

        // egy adott pillanatban kiket lat
        [CanBeNull]
        public abstract List<Robot> GetNoticedUnits();

        protected override void OnEnterZone(IZone zone, ZoneEnterType enterType)
        {
            base.OnEnterZone(zone, enterType);
            _probingInterval = new IntervalTimer(GetProximityCheckInterval(), true);
        }

        public virtual void OnDeviceDead()
        {
            //uccso info a regisztraltaknak
            SendDeviceDead();
            PBSRegisterHelper.ClearMembersFromSql(Eid);
            Zone.UnitService.RemoveUserUnit(this);
            Logger.Info("probe got deleted " + Eid);
        }

        public virtual void OnDeviceCreated()
        {
            //elso info arrol hogy letrejott

            Logger.Info("probe created " + Eid);

            SendDeviceCreated();

        }


        public abstract void OnUnitsFound(List<Robot> unitsFound);

        public abstract void OnUnitsFound(List<Player> unitsFound);
        #endregion

        protected override void OnDead(Unit killer)
        {
            OnDeviceDead();
            base.OnDead(killer);
        }

        public Dictionary<string, object> GetProbeInfo(bool includeRegistered = true)
        {
            Dictionary<string, object> info = BaseInfoToDictionary();

            Dictionary<string, object> probeDict = new Dictionary<string, object>();

            if (includeRegistered)
            {
                probeDict.Add(k.registered, GetRegisteredCharacters().GetCharacterIDs().ToArray());
            }

            probeDict.Add(k.zoneID, Zone.Id);
            probeDict.Add(k.x, CurrentPosition.X);
            probeDict.Add(k.y, CurrentPosition.Y);
            info.Add("probe", probeDict);
            return info;
        }

        /// <summary>
        /// All info included
        /// </summary>
        /// <returns></returns>
        public override Dictionary<string, object> ToDictionary()
        {
            return GetProbeInfo();
        }

        public void SendDeviceCreated()
        {
            IEnumerable<Character> membersToInfom = GetAllPossibleMembersToInfom();
            Message.Builder.SetCommand(Commands.ProximityProbeCreated).WithData(ToDictionary()).ToCharacters(membersToInfom).Send();
        }

        public void SendUpdateToAllPossibleMembers()
        {
            IEnumerable<Character> members = GetAllPossibleMembersToInfom();

            Message.Builder.SetCommand(Commands.ProximityProbeUpdate).WithData(ToDictionary()).ToCharacters(members).Send();
        }

        private void SendDeviceDead()
        {
            IEnumerable<Character> membersToInfom = GetAllPossibleMembersToInfom();

            Message.Builder.SetCommand(Commands.ProximityProbeDead).WithData(ToDictionary()).ToCharacters(membersToInfom).Send();
        }

        public IEnumerable<Character> GetAllPossibleMembersToInfom()
        {
            return GetRegisteredCharacters().Concat(GetProximityBoard(Owner)).Distinct();
        }

        private IEnumerable<Character> GetProximityBoard(long corporationEid)
        {
            const CorporationRole roleMask = CorporationRole.CEO | CorporationRole.DeputyCEO | CorporationRole.Accountant;
            return CorporationManager.LoadCorporationMembersWithAnyRole(corporationEid, roleMask);
        }

        public void Init(IEnumerable<Character> summonerCharacters)
        {
            PBSRegisterHelper.WriteRegistersToDb(Eid, summonerCharacters);
            _probingInterval.Interval = TimeSpan.FromMilliseconds(GetProximityCheckInterval());
        }

        public virtual int GetProximityCheckInterval()
        {
            DefinitionConfig config = EntityDefault.Get(Definition).Config;

            if (config.cycle_time == null)
            {
                Logger.Error("consistency error in proximity device. interval not defined. " + Definition + " " + ED.Name);
                return 150000;
            }

            return ((int)config.cycle_time) + FastRandom.NextInt(0, 250);
        }

        public bool IsRegistered(Character character)
        {
            return GetRegisteredCharacters().Contains(character);
        }

        public ErrorCodes HasAccess(Character character)
        {
            if (IsRegistered(character))
            {
                return ErrorCodes.NoError;
            }

            long corporationEid = character.CorporationEid;

            if (corporationEid != Owner)
            {
                return ErrorCodes.AccessDenied;
            }

            CorporationRole role = Corporation.GetRoleFromSql(character);

            return IsAllProbesVisible(role) ? ErrorCodes.NoError : ErrorCodes.AccessDenied;
        }

        public Dictionary<string, object> GetProbeRegistrationInfo()
        {
            Corporation ownerCorporation = Corporation.GetOrThrow(Owner);
            int maxRegistered = ownerCorporation.GetMaximumRegisteredProbesAmount();
            int currentRegistered = GetRegisteredCharacters().Length;
            int boardMembers = ownerCorporation.GetBoardMembersCount();

            Dictionary<string, object> result = new Dictionary<string, object>
            {
                {k.eid, Eid },
                {"maxRegistered", maxRegistered},
                {"freeSlots", maxRegistered - (currentRegistered - boardMembers).Clamp(0, int.MaxValue)},
                {"currentlyRegistered", currentRegistered},
                {"boardMembers", boardMembers},
            };

            return result;
        }

        public static bool IsAllProbesVisible(CorporationRole role)
        {
            return role.IsAnyRole(CorporationRole.CEO, CorporationRole.DeputyCEO);
        }

        public override void OnInsertToDb()
        {
            DynamicProperties.Update(k.currentCore, Core);
            base.OnInsertToDb();
        }

        public override void OnUpdateToDb()
        {
            DynamicProperties.Update(k.currentCore, Core);
            base.OnUpdateToDb();
        }
    }
}
