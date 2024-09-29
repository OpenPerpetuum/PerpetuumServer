using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Accounting.Characters
{
    public static class CharacterExtensionMethods
    {
        public static List<Character> ToCharacter(this IEnumerable<int> characterIds)
        {
            List<Character> result = new List<Character>();

            foreach (int characterId in characterIds)
            {
                Character c = Character.Get(characterId);
                if (c == Character.None)
                {
                    continue;
                }

                result.Add(c);
            }

            return result;
        }

        public static IList<int> GetCharacterIDs(this IEnumerable<Character> characters)
        {
            return characters.Select(c => c.Id).ToArray();
        }
    }
}
