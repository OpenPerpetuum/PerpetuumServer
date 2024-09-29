using Perpetuum.Wallets;
using System.Collections.Generic;

namespace Perpetuum.Accounting
{
    public class AccountWallet : Wallet<int>, IAccountWallet
    {
        private readonly Account account;
        private readonly AccountTransactionType transactionType;

        public AccountWallet(Account account, AccountTransactionType transactionType)
        {
            this.account = account;
            this.transactionType = transactionType;
        }

        protected override void SetBalance(int value)
        {
            account.Credit = value;
        }

        protected override int GetBalance()
        {
            return account.Credit;
        }

        protected override void OnBalanceUpdating(int currentCredit, int desiredCredit)
        {
            // handles negative credit as well.
            if (desiredCredit - currentCredit < 0 && desiredCredit < 0)
            {
                throw new PerpetuumException(ErrorCodes.AccountNotEnoughMoney);
            }
        }

        protected override void OnCommited(int startBalance)
        {
            int currentCredit = GetBalance();
            int change = currentCredit - startBalance;

            Dictionary<string, object> info = new Dictionary<string, object>
                    {
                        {k.credit, currentCredit},
                        {k.change, change},
                        {k.transactionType, (int)transactionType}
                    };

            Message.Builder
                .SetCommand(Commands.AccountUpdateBalance)
                .WithData(info)
                .ToAccount(account)
                .Send();
        }
    }
}