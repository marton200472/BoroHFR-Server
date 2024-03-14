using Microsoft.Extensions.Configuration.Json;

namespace BoroHFR.Configuration
{
    public class AppSettingsJsonConfigurationSource : IConfigurationSource
    {
        private string _configFilePath;
        public AppSettingsJsonConfigurationSource(string filePath) {
            _configFilePath = filePath;
        }

        /// <summary>
        /// Builds the <see cref="JsonConfigurationProvider"/> for this source.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/>.</param>
        /// <returns>A <see cref="JsonConfigurationProvider"/></returns>
        /// 
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new AppSettingsJsonConfigurationProvider(_configFilePath);
        }
    }
}
