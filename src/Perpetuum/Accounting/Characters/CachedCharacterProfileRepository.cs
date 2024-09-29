using System;
using System.Collections.Generic;

namespace Perpetuum.Accounting.Characters
{
    public class CachedCharacterProfileRepository : ICharacterProfileRepository
    {
        private readonly CachedReadOnlyRepository<int, CharacterProfile> cachedReadOnlyRepository;

        public CachedCharacterProfileRepository(CachedReadOnlyRepository<int, CharacterProfile> cachedReadOnlyRepository)
        {
            this.cachedReadOnlyRepository = cachedReadOnlyRepository;
        }

        public CharacterProfile Get(int id)
        {
            return cachedReadOnlyRepository.Get(id);
        }

        public IEnumerable<CharacterProfile> GetAll()
        {
            return cachedReadOnlyRepository.GetAll();
        }

        public IEnumerable<CharacterProfile> GetAllByAccount(Account account)
        {
            throw new NotImplementedException();
        }

        public void Remove(int id)
        {
            cachedReadOnlyRepository.Remove(id);
        }
    }
}
