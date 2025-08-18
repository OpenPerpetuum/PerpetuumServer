using Perpetuum.Data;
using Perpetuum.Threading.Process;
using Perpetuum.Timers;
using System;
using System.Transactions;

namespace Perpetuum.Services.MarketEngine
{
    public class MarketAutoOrdersManager : IProcess
    {
        private readonly TimerList _timers = new TimerList();

        public void Start()
        {
            RecalculatePricesAndRenewOrders();
            Init();
        }

        public void Stop()
        {
        }

        public void Update(TimeSpan time)
        {
            _timers.Update(time);
        }

        private void Init()
        {
            _timers.Add(new TimerAction(ConsolidateStatistics, TimeSpan.FromMinutes(15)));
            _timers.Add(new TimerAction(RecalculatePricesAndRenewOrders, TimeSpan.FromDays(1)));

            // Debug purposes, do not uncomment
            //_timers.Add(new TimerAction(ConsolidateStatistics, TimeSpan.FromMinutes(1)));
            //_timers.Add(new TimerAction(RecalculatePricesAndRenewOrders, TimeSpan.FromMinutes(1)));
        }

        private void ConsolidateStatistics()
        {
            using (TransactionScope scope = Db.CreateTransaction())
            {
                _ = Db.Query()
                    .CommandText("exec consolidate_statistics")
                    .ExecuteNonQuery();

                scope.Complete();
            }
        }

        private void RecalculatePricesAndRenewOrders()
        {
            using (TransactionScope scope = Db.CreateTransaction())
            {
                _ = Db.Query()
                    .CommandText("exec recalculate_raw_material_prices")
                    .ExecuteNonQuery();
                _ = Db.Query()
                    .CommandText("exec usp_RefreshAutoMarketOrders")
                    .ExecuteNonQuery();

                scope.Complete();
            }
        }
    }
}
