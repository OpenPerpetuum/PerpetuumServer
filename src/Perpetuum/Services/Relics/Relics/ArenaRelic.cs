using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.Items;
using Perpetuum.Players;
using Perpetuum.Robots;
using System.Linq;
using System.Threading.Tasks;

namespace Perpetuum.Services.Relics.Relics
{
    public class ArenaRelic : AbstractRelic
    {
        public override void PopRelic(Player player)
        {
            //Set flag on relic for removal
            SetAlive(false);

            //Compute loots
            if (_loots == null)
            {
                return;
            }

            //Fork task to make the lootcan and log the ep
            Task.Run(() =>
            {
                using (System.Transactions.TransactionScope scope = Db.CreateTransaction())
                {
                    Entity item = Entity.Factory.CreateWithRandomEID(_loots.LootItems.First().ItemInfo.Definition);
                    if (item is Robot robot)
                    {
                        Robot currentRobot = player.Character.GetActiveRobot();
                        robot.Unpack();
                        player.Character.SetActiveRobot(robot);
                        currentRobot.RemoveFromZone();
                    }
                }
            });
        }
    }
}
