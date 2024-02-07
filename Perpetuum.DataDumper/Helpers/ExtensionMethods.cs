using Perpetuum.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Perpetuum.DataDumper
{
    public static class ExtensionMethods
    {
        public static string GameTierString(this EntityDefault info)
        {
            string levelText = $"T{info.Tier.level}";

            if (info.Tier.type == ExportedTypes.TierType.Prototype) {
                levelText += "P";
            } else if (info.Tier.type == ExportedTypes.TierType.Special) {
                // We nede to read from options to differentiate special
                string optionTier = info.Options.GetOption<string>("tier");
                optionTier = optionTier.Replace("level_", "");

                if (optionTier.Contains("a")) {
                    levelText += "-";
                } else if (optionTier.Contains("+")) {
                    levelText += "+";
                } else if (info.Tier.level == 0) {
                    levelText = "S";
                }
            }

            return levelText;
        }
    }
}
