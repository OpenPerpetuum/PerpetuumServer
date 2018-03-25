using System.Collections.Generic;

namespace Perpetuum.Accounting
{
    public interface IAccountRepository : IRepository<int,Account>
    {
        AccessLevel GetAccessLevel(int accountId);

        [CanBeNull]
        Account Get(int accountId, string steamId);

        [CanBeNull]
        Account Get(string email, string password);

        [CanBeNull]
        Account Get(string email);

        IEnumerable<Account> GetBySteamId(string steamId);
      
    }
}