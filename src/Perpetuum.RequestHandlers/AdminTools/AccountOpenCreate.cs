using Perpetuum.Accounting;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Relay;
using System.Data;

namespace Perpetuum.RequestHandlers.AdminTools
{
    public class AccountOpenCreate : IRequestHandler
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IServerInfoManager _serverInfoManager;

        public AccountOpenCreate(IAccountRepository accountRepository, IServerInfoManager serverInfoManager)
        {
            _accountRepository = accountRepository;
            _serverInfoManager = serverInfoManager;
        }

        public void HandleRequest(IRequest request)
        {
            var email = request.Data.GetOrDefault<string>(k.email);
            var password = request.Data.GetOrDefault<string>(k.password);

            //is the server open?
            var si = _serverInfoManager.GetServerInfo();
            if (!si.IsOpen)
                throw new PerpetuumException(ErrorCodes.InviteOnlyServer);

            // if an account was already created using this session, reject this creation attempt.
            if (request.Session.AccountCreatedInSession)
            {
                throw new PerpetuumException(ErrorCodes.MaxIterationsExceeded); 
            }

            var account = new Account
            {
                Email = email,
                Password = password,
                AccessLevel = AccessLevel.normal,
                CampaignId = "{\"host\":\"opencreate\"}"
            };

            //If email exists - throw error
            if (_accountRepository.Get(account.Email) != null)
            {
                Message.Builder.FromRequest(request).WithError(ErrorCodes.AccountAlreadyExists).Send();
                return;
            }

            //New Account creation procedure
            IDataRecord data = Db.Query().CommandText("opp_create_account")
                .SetParameter("@email", account.Email)
                .SetParameter("@password", account.Password)
                .SetParameter("@campaignid", account.CampaignId)
                .ExecuteSingleRow();
            data.GetInt32(0).ThrowIfEqual<int>(1, ErrorCodes.SQLInsertError);

            // if we get this far, make sure we can't sit here and make accounts.
            request.Session.AccountCreatedInSession = true;

            Message.Builder.FromRequest(request).SetData(k.account, account.ToDictionary()).Send();
        }
    }
}
