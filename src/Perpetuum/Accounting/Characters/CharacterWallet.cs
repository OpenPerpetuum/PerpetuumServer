using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Wallets;
using System.Collections.Generic;

namespace Perpetuum.Accounting.Characters
{
    public class CharacterWallet : Wallet<double>, ICharacterWallet
    {
        public CharacterWallet(Character character, ICharacterCreditService creditService, TransactionType transactionType)
        {
            this.character = character;
            this.creditService = creditService;
            this.transactionType = transactionType;
        }

        private readonly Character character;
        private readonly ICharacterCreditService creditService;
        private readonly TransactionType transactionType;

        protected override double GetBalance()
        {
            return creditService.GetCredit(character.Id);
        }

        protected override void SetBalance(double value)
        {
            creditService.SetCredit(character.Id, value);
        }

        protected override void OnBalanceUpdating(double currentCredit, double desiredCredit)
        {
            desiredCredit.ThrowIfLess(0, ErrorCodes.CharacterNotEnoughMoney);
        }

        protected override void OnCommited(double startBalance)
        {
            double currentCredit = GetBalance();
            double change = currentCredit - startBalance;

            Dictionary<string, object> info = new Dictionary<string, object>
            {
                { k.credit, (long)currentCredit},
                { k.amount,change},
                { k.transactionType, (int)transactionType },
            };

            Message.Builder
                .SetCommand(Commands.CharacterUpdateBalance)
                .WithData(info)
                .ToCharacter(character)
                .Send();
        }
    }
}