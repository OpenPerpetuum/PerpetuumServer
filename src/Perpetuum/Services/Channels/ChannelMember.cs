using Perpetuum.Accounting.Characters;
using System.Collections.Generic;

namespace Perpetuum.Services.Channels
{
    public class ChannelMember
    {
        public readonly Character character;
        public readonly ChannelMemberRole role;

        public ChannelMember(Character character, ChannelMemberRole role)
        {
            this.character = character;
            this.role = role;
        }

        public ChannelMember WithRole(ChannelMemberRole newRole)
        {
            return newRole == role ? this : new ChannelMember(character, newRole);
        }

        public bool CanTalk => !character.GlobalMuted;

        public bool HasRole(ChannelMemberRole r)
        {
            return role.HasFlag(r);
        }

        public IDictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
                       {
                               {k.memberID, character.Id},
                               {k.role, (int) role}
                       };
        }

        public override string ToString()
        {
            return $"Member: {character}, Role: {role}";
        }
    }
}