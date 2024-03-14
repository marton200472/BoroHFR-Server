using Microsoft.Extensions.Primitives;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace BoroHFR.Configuration
{
    public class AppSettingsJsonConfigurationProvider : IConfigurationProvider
    {
        public AppSettingsJsonConfigurationProvider(string configFilePath)
        {
            _document = new AppSettingsDocument(configFilePath);
        }

        private AppSettingsDocument _document ;
        public IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string? parentPath)
        {
            if (parentPath is null)
            {
                return earlierKeys;
            }

            var current = _document[parentPath];

            if (current is null)
            {
                return Enumerable.Empty<string>();
            }

            if (current is JsonObject o)
            {
                return o.AsEnumerable().Select(x => x.Key);
            }

            return Enumerable.Empty<string>();
        }

        public IChangeToken GetReloadToken()
        {
            //return _document.ChangeToken;
            return null;
        }

        public void Load()
        {
        }

        public void Set(string key, string? value)
        {
            _document[key] = value;
            _document.Save();
        }

        public bool TryGet(string key, out string? value)
        {
            var node = _document[key];
            if (node is not null)
            {
                value =  node.ToString();
                return true;
            }
            value = null;
            return false;
        }
    }
}
