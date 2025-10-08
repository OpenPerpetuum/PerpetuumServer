using System;
using Perpetuum.Accounting.Characters;

namespace Perpetuum.Groups.Gangs
{
    public interface IGangManager
    {
        [CanBeNull]
        Gang GetGang(Guid gangID);

        [CanBeNull]
        Gang GetGangByMember(Character member);

        Gang CreateGang(string gangName,Character leader);
        void DisbandGang(Gang gang);

        void JoinMember(Gang gang, Character member,bool joinChannel);
        void RemoveMember(Gang gang, Character member,bool isKick);

        void ChangeLeader(Gang gang, Character newLeader);
        void SetRole(Gang gang, Character member, GangRole newRole);

        /// <summary>
        /// An action performed after a gang is created
        /// </summary>
        event Action<Gang,Character /* leader */> GangCreate;
        
        event Action<Gang,Character /* member */> GangMemberJoined;
        event Action<Gang,Character /* member */> GangMemberRemoved;
        event Action<Gang> GangDisbanded;
        event Action<Gang> GangLeaderChanged;
    }
}