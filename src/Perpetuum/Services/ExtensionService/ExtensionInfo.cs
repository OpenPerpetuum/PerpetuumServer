using System.Collections.Generic;
using System.Data;
using Perpetuum.Data;
using Perpetuum.ExportedTypes;

namespace Perpetuum.Services.ExtensionService
{
    public class ExtensionInfo
    {
        private readonly int _id;
        private readonly string _name;
        private readonly int _category;
        private readonly int _rank;
        private readonly string _learningAttributePrimary;
        private readonly string _learningAttributeSecondary;
        private readonly double _bonus;
        private readonly int _price;
        private readonly string _description;
        private readonly AggregateField _aggregateField;
        private readonly bool _hidden;
        private readonly int? _freezeLimit;

        public int Id => _id;
        public string Name => _name;
        private int Category => _category;
        public int Rank => _rank;
        private string LearningAttributePrimary => _learningAttributePrimary;
        private string LearningAttributeSecondary => _learningAttributeSecondary;
        public double Bonus => _bonus;
        public int Price => _price;
        public string Description => _description;
        public AggregateField AggregateField => _aggregateField;
        public bool Hidden => _hidden;
        private int? FreezeLimit => _freezeLimit;

        public Extension[] RequiredExtensions { get; set; }

        public ExtensionInfo(IDataRecord record)
        {
            _id = record.GetValue<int>("extensionid");
            _name = record.GetValue<string>("extensionname");
            _category = record.GetValue<int>("category");
            _rank = record.GetValue<int>("rank");
            _learningAttributePrimary = record.GetValue<string>("learningattributeprimary");
            _learningAttributeSecondary = record.GetValue<string>("learningattributesecondary");
            _bonus = record.GetValue<double>("bonus");
            _price = record.GetValue<int>("price");
            _description = record.GetValue<string>("description");
            _aggregateField = (AggregateField)(record.GetValue<int?>("targetpropertyID") ?? 0);
            _hidden = record.GetValue<bool>("hidden");
            _freezeLimit = record.GetValue<int?>("freezelimit");
        }

        public override string ToString()
        {
            return $"name:{Name} id:{Id}";
        }

        public Dictionary<string,object> ToDictionary()
        {
            return new Dictionary<string, object>
                {
                    {k.extensionID, Id},
                    {k.name, Name},
                    {k.category, Category},
                    {k.rank, Rank},
                    {k.price, Price},
                    {k.bonus, Bonus},
                    {k.learningAttributePrimary, LearningAttributePrimary},
                    {k.learningAttributeSecondary, LearningAttributeSecondary},
                    {k.description, Description},
                    {k.hidden, Hidden},
                    {k.freezeLimit, FreezeLimit},
                };
        }
    }
}
