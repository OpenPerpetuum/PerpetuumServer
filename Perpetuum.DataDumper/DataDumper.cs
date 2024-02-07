using Autofac;
using Perpetuum.Bootstrapper;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Modules;
using Perpetuum.Modules.Weapons;
using Perpetuum.Robots;
using Perpetuum.Services.ExtensionService;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Perpetuum.Items.Ammos;
using Perpetuum.Services.ProductionEngine;
using Perpetuum.DataDumper.Views;
using System.Windows.Forms;
using System.CodeDom;
using Perpetuum.Services.Sparks;
using Perpetuum.Groups.Alliances;
using System.Reflection;
using NPOI.SS.UserModel;

namespace Perpetuum.DataDumper {
    public partial class DataDumper
    {
        private string serverRoot;
        private string dictionaryPath;

        public IExtensionReader ExtensionReader;

        private IContainer container;
        EntityFactory entityFactory;
        IEntityServices entityServices;
        IEntityDefaultReader defaultReader;
        Dictionary<string, string> itemNames = new Dictionary<string, string>();
        ISparkRepository sparkRepository;

        // Some static lists for helpers
        public static SlotFlags[] SLOT_SIZE_FLAGS = new SlotFlags[] { SlotFlags.small, SlotFlags.medium, SlotFlags.large };
        public static SlotFlags[] SLOT_TYPE_FLAGS = new SlotFlags[] { SlotFlags.turret, SlotFlags.missile, SlotFlags.melee, SlotFlags.industrial, SlotFlags.ew_and_engineering };
        public static SlotFlags[] SLOT_LOCATION_FLAGS = new SlotFlags[] { SlotFlags.head, SlotFlags.chassis, SlotFlags.leg };
        public static List<CategoryFlags> CATEGORIES_AMMO_WEAPON = new List<CategoryFlags> {
                                                                CategoryFlags.cf_railgun_ammo,
                                                                CategoryFlags.cf_laser_ammo,
                                                                CategoryFlags.cf_missile_ammo,
                                                                CategoryFlags.cf_projectile_ammo };

        public DataDumper(IContainer container, string serverRoot, string dictionaryPath)
        {
            this.serverRoot = serverRoot;

            this.dictionaryPath = dictionaryPath;

            this.container = container;

            entityFactory = container.Resolve<EntityFactory>(); 

            ExtensionReader = container.Resolve<IExtensionReader>();

            defaultReader = container.Resolve<IEntityDefaultReader>();

            var productionDataReader = container.Resolve<IProductionDataAccess>();

            sparkRepository = container.Resolve<ISparkRepository>();

            entityServices = container.Resolve<IEntityServices>();

            // Testing for new data dumps
            //
            // var getEd = defaultReader.GetByName("def_named1_small_armor_repairer");
            // var getCats = String.Join(";", getEd.CategoryFlags.GetCategoryFlagsTree().Where(x => x.ToString() != "undefined").Select(x => x.ToString()).ToList());
            // var testMissile = productionDataReader.ProductionComponents[64];
            // var testRobot = productionDataReader.ProductionComponents[193]; 
            // var testRobot2 = productionDataReader.ProductionComponents[208];

            var dataLines = System.IO.File.ReadAllText(dictionaryPath);

            var dictionaryText = GenXY.GenxyConverter.Deserialize(dataLines);

            if (dictionaryText.ContainsKey("dictionary")) {
                var sourceDict = (Dictionary<string, object>)dictionaryText["dictionary"];

                foreach (var item in sourceDict) {
                    itemNames.Add(item.Key, item.Value.ToString().Trim());
                }

            } else {
                throw new Exception("Dictionary file is invalid");
            }

            // Now read the names from JSON files
            string dictionaryLocation = System.IO.Path.Combine(serverRoot, @"customDictionary\0.json");

            var jsonData = JsonConvert.DeserializeObject<IDictionary<string, string>>(System.IO.File.ReadAllText(dictionaryLocation));

            foreach (var item in jsonData) {
                if (itemNames.ContainsKey(item.Key)) {
                    itemNames[item.Key] = item.Value;
                } else {
                    itemNames.Add(item.Key, item.Value);
                }
            }

            Console.WriteLine($"{dataLines.Length} dictionary names loaded");
        }

        public string GetLocalizedName(string itemKey)
        {
            if (itemNames.ContainsKey(itemKey))
            {
                return itemNames[itemKey];
            } else
            {
                return itemKey;
            }
        }

        public static string GenerateCargoDefinition(Type viewType, string tableName, string listDelimiter = ";") {
            string header = "<noinclude>\n{{#cargo_declare:_table="+tableName+"\n";
            string body = "";
            string footer = "}}\n</noinclude>";
            var allProperties = viewType.GetProperties().OrderBy(x=> x.Name).ToList();

            foreach (var property in allProperties) {
                var cargoAttribute = property.GetCustomAttribute<CargoTypeAttribute>();

                string cargoType = "String"; // <- This is the default in Cargo

                if (cargoAttribute != null) {
                    cargoType = cargoAttribute.Type;
                } else {
                    // Let's set some defaults by property type
                    if (property.PropertyType == typeof(double)) {
                        cargoType = "Float";
                    } else if (property.PropertyType == typeof(int)) {
                        cargoType = "Integer";
                    } else if (property.PropertyType == typeof(bool)) {
                        cargoType = "Boolean";
                    } else if(property.PropertyType == typeof(List<string>)) {
                        cargoType = $"List ({listDelimiter}) of String";
                    } else if (property.PropertyType == typeof(List<int>)) {
                        cargoType = $"List ({listDelimiter}) of Integer";
                    } else if (property.PropertyType == typeof(List<double>)) {
                        cargoType = $"List ({listDelimiter}) of Float";
                    }
                }

                body += $"|{property.Name}={cargoType}\n";
            }

            return header + body + footer;

        }

        public void InitItemView(ItemDataView view, Entity entity) {
            view.ItemName = GetLocalizedName(entity.ED.Name);
            view.ItemKey = entity.ED.Name;
            view.ItemCategories = entity.ED.CategoryFlags.GetCategoryFlagsTree().Where(x=> x.ToString() != "undefined").Select(x => x.ToString()).ToList();
            view.ItemMass = entity.Mass;
            view.ItemVolumePacked = entity.ED.CalculateVolume(true, 1);
            view.ItemVolume = entity.ED.CalculateVolume(false, 1);
        }

        public void InitModuleView(ModuleDataView view, Modules.Module module) {
            InitItemView(view, module);

            view.ModuleTier = module.ED.GameTierString();
            view.ModuleCpu = module.CpuUsage;
            view.ModuleReactor = module.PowerGridUsage;

            view.ExtensionsRequired = new List<string>();

            foreach (var extension in module.ED.EnablerExtensions.Keys) {
                view.ExtensionsRequired.Add(GetLocalizedName(ExtensionReader.GetExtensionName(extension.id)) + "(" + extension.level + ")");
            }
        }

        public void InitActiveModuleView(ActiveModuleDataView view, ActiveModule module) {
            InitModuleView(view, module);

            view.ModuleAccumulator = module.CoreUsage;
            view.ModuleCycle = module.CycleTime.TotalSeconds;

            var range = module.GetBasePropertyModifier(AggregateField.optimal_range);

            if (range.HasValue) {
                view.ModuleOptimalRange = range.Value * 10;
            }
        }

        public static string GetModifierString(ItemPropertyModifier mod) {
            var returnValue = "";
            if (mod.HasValue) {
                if (mod.ToString().Contains("Formula: Add")) {
                    returnValue =  mod.Value * 100 + "%";

                    if (mod.Value > 0) {
                        returnValue = "+" + returnValue;
                    }
                } else {
                    returnValue = ((mod.Value - 1) * 100) + "%";

                    if (mod.Value - 1 > 0) {
                        returnValue = "+" + returnValue;
                    }
                }
                
            }

            return returnValue;
        }        

        public void WriteDataView(List<List<string>> dataRows, string sheetName, ISheet worksheet, ref int currentDataRow) {
            // Deal with the header
            // If we are writing later than the first row in our sheet then we will skip
            // the first row in our data because it will contain the header
            int skipRow = 0;

            if (currentDataRow > 0) {
                skipRow = 1;
            }

            foreach (var dataRow in dataRows.Skip(skipRow).ToList()) {
                int currentColumn = 0;
                var currentRow = worksheet.CreateRow(currentDataRow);
                foreach (var dataValue in dataRow) {
                    currentRow.CreateCell(currentColumn).SetCellValue(dataValue);
                    currentColumn++;
                }
                currentDataRow++;
            }
        }

        /// <summary>
        /// This will write the properties as headers and values as rows
        /// </summary>
        public List<List<string>> ComposeDataView<T>(List<T> data, string wikiTableName = "wikitable") {
            var returnData = new List<List<string>>();

            if (data is null || data.Count == 0) {
                return returnData; // Do nothing
            }

            var headers = data.First().GetType().GetProperties().Select(i => i.Name).ToList();

            returnData.Add(headers.Concat(new List<string> { "wiki" }).ToList());

            foreach (var item in data)
            {
                var currentProperties = new List<string>();

                // Add the wiki markup at the end
                // {{#cargo_store:_table=WeaponStats|module_name=Niani medium EM-gun|module_key=def_artifact_a_longrange_medium_railgun|module_categories=cf_medium_railguns;cf_railguns;cf_weapons;cf_robot_equipment|cpu=45|reactor=205|ammo_type=Medium slugs|slot_status=Active|ammo_capacity=50|module_mass=650|module_volume_packed=0.25|module_tier=T3+|module_volume=0.5|module_accumulator=32|module_cycle=10|module_damage=250|module_falloff=60|module_hit_dispersion=14|module_optimal_range=300|module_extensions_required=Advanced magnetostatics(2);|slot_type=turret|slot_size=medium|slot_location=chassis}}
                string wikiData = $"{{{{#cargo_store:_table={wikiTableName}";

                foreach (string prop in headers)
                {
                    var currentProp = item.GetType().GetProperty(prop);
                    var currentValue = "";

                    try
                    {
                        if (currentProp == null)
                        {
                            currentValue = "Error: Prop not found";
                        }
                        else
                        {
                            if (currentProp.PropertyType.Name.Contains("List")) {
                                currentValue = String.Join(";", (currentProp.GetValue(item) as IEnumerable<object>));
                            } else {
                                currentValue = currentProp.GetValue(item)?.ToString().Replace(",",";") ?? "";
                            }
                        }
                    }
                    catch (Exception)
                    {
                        currentValue = "EXCEPTION";
                    }

                    currentProperties.Add(currentValue);

                    wikiData += $"|{prop}={currentValue}";

                }

                wikiData += "}}";

                currentProperties.Add(wikiData);

                returnData.Add(currentProperties);

            }

            return returnData;

        }
        
        // TODO: Refactor this to use the built-in function instead of SQL
        private List<IDataRecord> GetDataByItemCategoryName(string categoryName)
        {
            var itemData = Db.Query().CommandText(
                    $@"DECLARE @lookupFlag bigint;
                    SET @lookupFlag = (select value from dbo.categoryFlags where name='{categoryName}');
                    SELECT * from entitydefaults where (categoryflags & CAST(dbo.GetCFMask(@lookupFlag)as BIGINT) = @lookupFlag)
                    AND enabled=1 AND hidden=0 AND purchasable=1;"
                ).Execute();

            return itemData;
        }

        public List<EntityDataView> DumpDataView(DataExportMapping mapping) {
            var returnItems = new List<EntityDataView>();

            // Handle sparks first, refactor later
            if (mapping.ViewType == typeof(SparkDataView)) {
                var sparkData = sparkRepository.GetAll();

                foreach (var spark in sparkData) {
                    try {
                        var currentView = new SparkDataView(spark, this);

                        returnItems.Add(currentView);
                    } catch (Exception ex) {
                        Console.WriteLine(ex.Message);
                    }
                }

                return returnItems;

            }

            if(mapping.ViewType == typeof(RobotDataView)) {
                var robotDefinitions = defaultReader.GetAll().GetByCategoryFlags(CategoryFlags.cf_robots);

                foreach (var robotDefinition in robotDefinitions) {
                    object currentObject = entityFactory.Create(robotDefinition.Name, EntityIDGenerator.Random);

                    ((Robot)currentObject).Unpack();

                    dynamic currentView = Activator.CreateInstance(mapping.ViewType, currentObject, this);

                    returnItems.Add(currentView);
                }

                return returnItems;
            }

            var categoryData = GetDataByItemCategoryName(mapping.Category);

            foreach (var categoryItem in categoryData) {
                string itemName = categoryItem["definitionname"].ToString();
                int itemId = (int)categoryItem["definition"];

                try {
                    object currentObject = entityFactory.Create(itemName, EntityIDGenerator.Random);

                    dynamic currentView = Activator.CreateInstance(mapping.ViewType, currentObject, this);

                    returnItems.Add(currentView);
                } catch (Exception ex) {
                    Console.WriteLine(ex.Message);
                }

            }
            return returnItems;

        }

        public object GenerateItem(string itemName) {
            var result = entityFactory.Create(itemName, EntityIDGenerator.Random);

            return result;
        }

    }
}
