using Perpetuum.Data;
using Perpetuum.Log;
using Perpetuum.Threading.Process;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Transactions;

namespace Perpetuum.Accounting
{
    public class AccountCreditHandler : IProcess
    {
        private readonly IAccountManager accountManager;
        private readonly IAccountRepository accountRepository;
        private int workInProgress;

        public AccountCreditHandler(IAccountManager accountManager, IAccountRepository accountRepository)
        {
            this.accountManager = accountManager;
            this.accountRepository = accountRepository;
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }

        public void Update(TimeSpan time)
        {
            ProcessCreditPayments();
        }

        private void ProcessCreditPayments()
        {
            if (Interlocked.CompareExchange(ref workInProgress, 1, 0) == 1)
            {
                return;
            }

            try
            {
                ProcessCreditQueue();
            }
            finally
            {
                workInProgress = 0;
            }
        }

        /// <summary>
        /// processes the account credit queue
        /// writes log and informs online affected clients 
        /// </summary>
        private void ProcessCreditQueue()
        {
            List<IDataRecord> records = Db.Query().CommandText("select * from accountcreditqueue").Execute();

            if (records.Count == 0)
            {
                Logger.DebugInfo("no new account credit record was found");

                return;
            }

            Logger.Info(records.Count + " new account credit records were found");

            //process all new records 
            foreach (IDataRecord record in records)
            {
                try
                {
                    using (TransactionScope scope = Db.CreateTransaction())
                    {
                        int accountId = record.GetValue<int>("accountid");

                        Logger.Info("processing account credit queue for accountId:" + accountId);

                        Account account = accountRepository.Get(accountId);
                        if (account == null)
                        {
                            continue;
                        }

                        int credit = record.GetValue<int>("credit");
                        int id = record.GetValue<int>("id");

                        IAccountWallet wallet = accountManager.GetWallet(account, AccountTransactionType.Purchase);

                        Logger.Info("accountId:" + accountId + " pre balance:" + wallet.Balance);

                        wallet.Balance += credit;

                        AccountTransactionLogEvent e = new AccountTransactionLogEvent(account, AccountTransactionType.Purchase)
                        {
                            Credit = wallet.Balance,
                            CreditChange = credit
                        };
                        accountManager.LogTransaction(e);

                        Db.Query()
                            .CommandText("delete accountcreditqueue where id = @id")
                            .SetParameter("@id", id)
                            .ExecuteNonQuery()
                            .ThrowIfEqual(0, ErrorCodes.SQLDeleteError);

                        accountRepository.Update(account);

                        Logger.Info(credit + " credits processed for account " + account.Email + " id:" + account.Id);

                        scope.Complete();
                        Logger.Info(credit + " account got transfered to accountId:" + accountId + " resulting balance:" + wallet.Balance);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex);
                }
            }
        }
    }
}
