using System.Transactions;
using Perpetuum.Data;

namespace Perpetuum.Wallets
{
    public abstract class Wallet<TCredit> : IWallet<TCredit>
    {
        private TCredit _startBalance;
        private bool _first = true;

        protected Wallet()
        {
            if (Transaction.Current != null)
                Transaction.Current.OnCommited(() => OnCommited(_startBalance));
        }

        protected virtual void OnCommited(TCredit startBalance)
        {

        }

        public TCredit Balance
        {
            get
            {
                var balance = GetBalance();

                if (_first)
                {
                    _first = false;
                    _startBalance = balance;
                }

                return balance;
            }
            set
            {
                var current = GetBalance();

                if (Equals(current, value))
                    return;

                OnBalanceUpdating(current, value);

                SetBalance(value);
            }
        }

        protected abstract void SetBalance(TCredit value);
        protected abstract TCredit GetBalance();

        protected abstract void OnBalanceUpdating(TCredit currentCredit, TCredit desiredCredit);


        public override string ToString()
        {
            return $"Balance: {GetBalance()}";
        }
    }
}