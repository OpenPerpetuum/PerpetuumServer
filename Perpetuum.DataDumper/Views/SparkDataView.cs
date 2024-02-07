using NPOI.POIFS.Storage;
using Perpetuum.Groups.Alliances;
using Perpetuum.Items.Ammos;
using Perpetuum.Services.ExtensionService;
using Perpetuum.Services.Sparks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Perpetuum.DataDumper.DataDumper;

namespace Perpetuum.DataDumper.Views {
    public class SparkDataView : EntityDataView {
        public double? price_purchase { get; set; }
        public double? price_equip { get; set; }
        public double? required_standing { get; set; }
        public int sequence { get; set; }
        public string icon { get; set; }
        public string extensions { get; set; }
        public string alliance { get; set; }
        public string energy_prop { get; set; }

        public SparkDataView(Spark spark, DataDumper dumper) {

            ItemName = dumper.GetLocalizedName(spark.sparkName);
            ItemKey = spark.sparkName;
            ItemCategories = new List<string>(); // No categories
            price_equip = spark.changePrice;
            price_purchase = spark.unlockPrice;
            required_standing = spark.standingLimit * 100;
            sequence = spark.displayOrder;
            icon = spark.icon;
            energy_prop = spark.energyCredit.ToString();

            if (spark.allianceEid.HasValue) {
                // For some reason the Alliance object doesn't pull over the name
                // so we need to pull it as an eneity
                // var currentAlliance = Alliance.Repository.Load(spark.allianceEid);

                // Trying as an entity doesn't work either
                // var currentAlliance = entityServices.Repository.Load(.Value);


                var allianceData = AllianceHelper.GetAllianceInfo();

                foreach (var allianceInfo in allianceData) {
                    var childDict = (Dictionary<string, object>)allianceInfo.Value;
                    long allianceEid = (long)childDict["allianceEID"];
                    string allianceName = (string)childDict["name"];

                    if (allianceEid == spark.allianceEid) {
                        alliance = dumper.GetLocalizedName(allianceName);
                        break;
                    }
                }
            }

            var extensionsDict = new Dictionary<string, int>();
            var extensionStrings = new List<string>();

            foreach (Extension extension in spark.RelatedExtensions) {
                var extensionInfo = dumper.ExtensionReader.GetExtensionByID(extension.id);

                extensionsDict.Add(extensionInfo.name, extension.level);

                string localName = SparkBonusString(extensionInfo.name, extension.level, dumper);

                extensionStrings.Add(localName);

            }

            this.extensions = String.Join(";", extensionStrings);
        }

        private string SparkBonusString(string extensionName, int value, DataDumper dumper) {
            var formulaStrings = new List<string> { "{%BONUS100%}", "{%BONUS%}" };

            // First check if the localized name exists
            var localName = dumper.GetLocalizedName(extensionName);

            if (localName == extensionName) {
                // This means the desc wasn't found
                localName = dumper.GetLocalizedName(extensionName + "_desc");

                foreach (var formula in formulaStrings) {
                    localName = localName.Replace(formula, value.ToString());
                }

            }

            return localName;
        }
    }
}
