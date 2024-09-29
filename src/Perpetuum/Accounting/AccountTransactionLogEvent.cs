using Perpetuum.Log;
using System;
using System.Collections.Generic;

namespace Perpetuum.Accounting
{
    public class AccountTransactionLogEvent : ILogEvent
    {
        public AccountTransactionLogEvent(Account account, AccountTransactionType transactionType)
        {
            Account = account;
            TransactionType = transactionType;
        }

        public AccountTransactionType TransactionType { get; private set; }

        public Account Account { get; set; }

        public int? Definition { get; set; }

        public int? Quantity { get; set; }

        public long? Eid { get; set; }

        public int Credit { get; set; }

        public int CreditChange { get; set; }

        public DateTime Created { get; set; } = DateTime.Now;

        public IDictionary<string, object> ToDictionary()
        {
            Dictionary<string, object> d = new Dictionary<string, object>
            {
                {k.transactionType, (int) TransactionType},
                {k.definition, Definition},
                {k.quantity, Quantity},
                {k.credit, Credit},
                {k.creditChange, CreditChange},
                {k.created, Created}
            };

            return d;
        }

    }
}