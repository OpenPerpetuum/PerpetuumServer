using System.Collections.Generic;
using System.Linq;
using Perpetuum.ExportedTypes;

namespace Perpetuum.Services.ExtensionService
{
    public static class ExtensionReaderExtensions
    {
        public static IEnumerable<Extension> GetPrerequiredExtensionsOf(this IExtensionReader reader,int extensionId)
        {
            foreach (var info in reader.GetExtensions().Values)
            {
                foreach (var requiredExtension in info.RequiredExtensions)
                {
                    if (requiredExtension.id == extensionId)
                    {
                        //yes, this extension requires the incoming extension on level
                        yield return new Extension(info.Id, requiredExtension.level);
                    }
                }
            }
        }

        public static IEnumerable<int> GetExtensionPrerequireTree(this IExtensionReader reader,int extensionId)
        {
            var info = reader.GetExtensionByID(extensionId);
            if (info == null)
            {
                yield break;
            }

            yield return info.Id;

            foreach (var extension in info.RequiredExtensions)
            {
                foreach (var i in reader.GetExtensionPrerequireTree(extension.id))
                {
                    yield return i;
                }
            }
        }

        public static IEnumerable<Extension> GetRequiredExtensions(this IExtensionReader reader,int extensionId)
        {
            var info = reader.GetExtensionByID(extensionId);
            if (info == null)
            {
                yield break;
            }

            foreach (var requiredExtension in info.RequiredExtensions)
            {
                yield return requiredExtension;

                foreach (var extension in reader.GetRequiredExtensions(requiredExtension.id))
                {
                    yield return extension;
                }
            }
        }

        public static IEnumerable<int> GetExtensionIDsByName(this IExtensionReader reader, IEnumerable<string> extensionNames)
        {
            var extensions = reader.GetExtensions();

            var enumerable = extensionNames as string[] ?? extensionNames.ToArray();

            foreach (var info in extensions.Values)
            {
                if (enumerable.Contains(info.Name))
                {
                    yield return info.Id;
                }
            }
        }

        public static int GetExtensionIDByName(this IExtensionReader reader, string extensionName)
        {
            var x = reader.GetExtensions().Select(kvp => kvp.Value).FirstOrDefault(e => e.Name == extensionName);
            if (x == null)
            {
                return 0;
            }

            return x.Id;
        }

        [CanBeNull]
        public static ExtensionInfo GetExtensionByName(this IExtensionReader reader, string extensionName)
        {
            return reader.GetExtensions().Select(kvp => kvp.Value).FirstOrDefault(e => e.Name == extensionName);
        }

        public static string GetExtensionName(this IExtensionReader reader, int extensionID)
        {
            var extensions = reader.GetExtensions();
            var info = extensions.GetOrDefault(extensionID);
            if (info == null)
            {
                return string.Empty;
            }

            return info.Name;
        }

        [CanBeNull]
        public static ExtensionInfo GetExtensionByID(this IExtensionReader reader, int extensionID)
        {
            return reader.GetExtensions().GetOrDefault(extensionID);
        }

        public static ExtensionInfo[] GetExtensionsByAggregateField(this IExtensionReader extensionReader,AggregateField field)
        {
            return extensionReader.GetExtensions().Values.Where(e => e.AggregateField == field).ToArray();
        }
    }
}
