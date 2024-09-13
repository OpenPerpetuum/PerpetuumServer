using Perpetuum.Data;
using Perpetuum.ExportedTypes;
using Perpetuum.Host.Requests;
using Perpetuum.Items;
using Perpetuum.Players;
using Perpetuum.Services.MissionEngine;
using Perpetuum.Services.MissionEngine.MissionProcessorObjects;
using Perpetuum.Zones;
using Perpetuum.Zones.Teleporting;
using Perpetuum.Zones.Teleporting.Strategies;
using System.Linq;
using System.Transactions;

namespace Perpetuum.RequestHandlers.Zone
{
    public class TeleportUse : IRequestHandler<IZoneRequest>
    {
        private readonly ITeleportStrategyFactories _teleportStrategyFactories;
        private readonly MissionProcessor _missionProcessor;

        public TeleportUse(ITeleportStrategyFactories teleportStrategyFactories, MissionProcessor missionProcessor)
        {
            _teleportStrategyFactories = teleportStrategyFactories;
            _missionProcessor = missionProcessor;
        }

        public void HandleRequest(IZoneRequest request)
        {
            using (TransactionScope scope = Db.CreateTransaction())
            {
                Teleport teleport = GetTeleport(request);
                Accounting.Characters.Character character = request.Session.Character;
                Player player = request.Zone.GetPlayerOrThrow(character);

                ValidatePlayer(player, teleport);
                Position playerPosition = player.CurrentPosition;

                TeleportDescription description = GetTeleportDescription(request, teleport);
                ITeleportStrategy strategy = CreateTeleportStrategy(request, description);
                strategy.DoTeleport(player);

                Transaction.Current.OnCommited(() =>
                {
                    _ = _missionProcessor.EnqueueMissionTargetAsync(character, MissionTargetType.teleport, d =>
                    {
                        d.Add(k.channel, description.id);
                        d.Add(k.zoneID, request.Zone.Id);
                        d.Add(k.position, playerPosition);
                    });

                    switch (teleport)
                    {
                        case MobileStrongholdTeleport mobileStrongholdTeleport:
                            {
                                mobileStrongholdTeleport.ApplyTeleportCooldownEffect();
                                mobileStrongholdTeleport.Activate(player, description);
                                break;
                            }
                        case MobileWorldTeleport mobileWorldTeleport:
                            {
                                mobileWorldTeleport.ApplyTeleportCooldownEffect();
                                mobileWorldTeleport.Activate(player, description);
                                break;
                            }
                        case MobileTeleport mobileTeleport:
                            {
                                mobileTeleport.ApplyTeleportCooldownEffect();
                                break;
                            }

                    }
                });

                scope.Complete();
            }
        }

        private static TeleportDescription GetTeleportDescription(IRequest request, Teleport teleport)
        {
            int descriptionId = request.Data.GetOrDefault<int>(k.ID);
            TeleportDescription description = teleport.GetTeleportDescriptions().FirstOrDefault(d => d.id == descriptionId).ThrowIfNull(ErrorCodes.TeleportDescriptionNotFound);
            description.active.ThrowIfFalse(ErrorCodes.TeleportChannelInactive);
            description.IsValid().ThrowIfFalse(ErrorCodes.InvalidTeleportChannel);
            return description;
        }

        private static void ValidatePlayer(Player player, Teleport teleport)
        {
            TeleportPlayerValidator validator = new TeleportPlayerValidator(player);
            teleport.AcceptVisitor(validator);
        }

        private Teleport GetTeleport(IZoneRequest request)
        {
            long teleportEid = request.Data.GetOrDefault<long>(k.eid);
            Teleport teleport = (Teleport)request.Zone.GetUnitOrThrow(teleportEid);
            teleport.IsEnabled.ThrowIfFalse(ErrorCodes.TeleportDisabled);
            return teleport;
        }

        [CanBeNull]
        private ITeleportStrategy CreateTeleportStrategy(IRequest request, TeleportDescription description)
        {
            switch (description.descriptionType)
            {
                case TeleportDescriptionType.WithinZone:
                    {
                        TeleportWithinZone t = _teleportStrategyFactories.TeleportWithinZoneFactory();
                        t.TargetPosition = description.GetRandomTargetPosition();
                        return t;
                    }
                case TeleportDescriptionType.AnotherZone:
                    {
                        TeleportToAnotherZone toAnotherZone = _teleportStrategyFactories.TeleportToAnotherZoneFactory(description.TargetZone);
                        toAnotherZone.TargetPosition = description.GetRandomTargetPosition();
                        return toAnotherZone;
                    }
                case TeleportDescriptionType.TrainingExit:
                    {
                        TrainingExitStrategy exitStrategy = _teleportStrategyFactories.TrainingExitStrategyFactory(description);
                        exitStrategy.TrainingRewardLevel = request.Data.GetOrDefault<int>(k.rewardLevel);
                        return exitStrategy;
                    }
            }

            return null;
        }


        private class TeleportPlayerValidator : TeleportVisitor
        {
            private readonly Player _player;

            public TeleportPlayerValidator(Player player)
            {
                _player = player;
            }

            public override void VisitTeleport(Teleport teleport)
            {
                _player.HasTeleportSicknessEffect.ThrowIfTrue(ErrorCodes.TeleportTimerStillRunning);
                _player.HasNoxTeleportNegationEffect.ThrowIfTrue(ErrorCodes.NoxTeleportForbidden);
                (_player.HasPvpEffect && _player.HasNoTeleportWhilePVP).ThrowIfTrue(ErrorCodes.CantBeUsedInPvp);
                _player.CurrentPosition.IsInRangeOf3D(teleport.CurrentPosition, Teleport.TeleportRange).ThrowIfFalse(ErrorCodes.TeleportOutOfRange);
                base.VisitTeleport(teleport);
            }

            public override void VisitMobileTeleport(MobileTeleport teleport)
            {
                if (!_player.Session.AccessLevel.IsAdminOrGm())
                {
                    _player.HasPvpEffect.ThrowIfTrue(ErrorCodes.CantBeUsedInPvp);
                    teleport.EffectHandler.ContainsEffect(EffectType.effect_teleport_cooldown).ThrowIfTrue(ErrorCodes.TeleportSourceNotUsable);
                }

                Accounting.Characters.Character ownerCharacter = teleport.GetOwnerAsCharacter();

                if (_player.Character != ownerCharacter)
                {
                    Groups.Gangs.Gang playerGang = _player.Gang;
                    _ = playerGang.ThrowIfNull(ErrorCodes.CharacterNotInGang);
                    playerGang.IsMember(ownerCharacter).ThrowIfFalse(ErrorCodes.CharacterNotInTheOwnerGang);
                }

                VisitTeleport(teleport);
            }

            public override void VisitMobileWorldTeleport(MobileWorldTeleport teleport)
            {
                VisitMobileTeleport(teleport);
            }

            public override void VisitMobileStrongholdTeleport(MobileStrongholdTeleport teleport)
            {
                VisitMobileTeleport(teleport);
            }
        }
    }
}