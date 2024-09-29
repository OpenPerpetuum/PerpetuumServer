using Perpetuum.Data;
using Perpetuum.Log;
using Perpetuum.Services.Insurance;
using Perpetuum.Services.MarketEngine;
using Perpetuum.Services.MissionEngine.AdministratorObjects;
using Perpetuum.Services.MissionEngine.MissionProcessorObjects;
using Perpetuum.Services.ProductionEngine;
using Perpetuum.Services.Sparks;
using Perpetuum.Zones.Scanning.Results;
using System.Transactions;

namespace Perpetuum.Accounting.Characters
{
    public class CharacterCleaner
    {
        public CharacterCleaner(
            MarketHelper marketHelper,
            SparkHelper sparkHelper,
            ProductionManager productionManager,
            MissionProcessor missionProcessor)
        {
            this.marketHelper = marketHelper;
            this.sparkHelper = sparkHelper;
            this.productionManager = productionManager;
            this.missionProcessor = missionProcessor;
        }

        private readonly MarketHelper marketHelper;
        private readonly SparkHelper sparkHelper;
        private readonly ProductionManager productionManager;
        private readonly MissionProcessor missionProcessor;

        public void CleanUp(Character character)
        {
            //reset extensions
            character.ResetAllExtensions();

            //reset credit
            character.Credit = 0;

            //reset sparks
            sparkHelper.ResetSparks(character);

            //remove scanresults
            MineralScanResultRepository repo = new MineralScanResultRepository(character);
            repo.DeleteAll();

            //remove insurance
            InsuranceHelper.RemoveAll(character);

            //remove market orders
            marketHelper.RemoveAll(character);

            Db.Query()
                .CommandText("delete charactertransactions where characterid=@characterID")
                .SetParameter("@characterID", character.Id)
                .ExecuteNonQuery();

            Db.Query()
                .CommandText("delete productionlog where characterid=@characterID")
                .SetParameter("@characterID", character.Id)
                .ExecuteNonQuery();

            Db.Query().CommandText("delete techtreeunlockednodes where owner=@eid").SetParameter("@eid", character.Eid).ExecuteNonQuery();

            Db.Query()
                .CommandText("delete techtreelog where character=@characterID")
                .SetParameter("@characterID", character.Id)
                .ExecuteNonQuery();

            Db.Query().CommandText("delete techtreepoints where owner=@eid").SetParameter("@eid", character.Eid).ExecuteNonQuery();

            character.HomeBaseEid = null;

            //delete all items
            Db.Query()
                .CommandText("delete entities where owner=@rootEid")
                .SetParameter("@rootEid", character.Eid)
                .ExecuteNonQuery();

            Transaction.Current.OnCommited(() =>
            {
                //stop productions
                ProductionAbort(character);
                //stop all missions
                MissionForceAbort(character);
            });

            //do/finish character wizard
        }

        private void ProductionAbort(Character character)
        {
            productionManager.ProductionProcessor.AbortProductionsForOneCharacter(character);
        }

        private void MissionForceAbort(Character character)
        {
            if (missionProcessor.MissionAdministrator.GetMissionInProgressCollector(character, out MissionInProgressCollector collector))
            {
                collector.Reset();
            }

            Logger.Info("all missions aborted for " + this);
        }
    }
}