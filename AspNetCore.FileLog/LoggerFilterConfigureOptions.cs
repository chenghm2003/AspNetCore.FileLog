using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AspNetCore.FileLog
{
    internal class LoggerFilterConfigureOptions : IConfigureOptions<Microsoft.Extensions.Logging.LoggerFilterOptions>
    {

        public LoggerFilterConfigureOptions(IConfiguration configuration)
        {
            LoggerSettings.JsonConfiguration = (configuration as ConfigurationRoot).Providers.FirstOrDefault()
                as Microsoft.Extensions.Configuration.Json.JsonConfigurationProvider;
        }

        public void Configure(Microsoft.Extensions.Logging.LoggerFilterOptions options)
        {
            if (LoggerSettings.JsonConfiguration == null)
            {
                return;
            }
            var source = LoggerSettings.JsonConfiguration?.Source;
            var file = source?.FileProvider.GetFileInfo(source.Path);
            if (file != null && !file.IsDirectory && file.Exists)
            {
                using (var reader = new StreamReader(file.CreateReadStream()))
                {
                    var content = reader.ReadToEnd();
                    var jToken = JsonConvert.DeserializeObject<JToken>(content);
                    if (jToken != null)
                    {
                        try
                        {
                            var rules = jToken.SelectToken(LoggerSettings.RulesKey, false)
                                .ToObject<List<LoggerFilterRule>>();

                            foreach (var _rule in rules)
                            {
                                options.Rules.Add(_rule);
                            }
                            return;
                        }
                        catch
                        {

                        }
                    }

                }
                File.WriteAllText(file.PhysicalPath, LoggerSettings.LoggingJsonContent);
                var rule = new LoggerFilterRule(LoggerSettings.DefaultProviderName, LoggerSettings.DefaultName, LogLevel.Information, null);
                rule.LogType = LogType.All;
                options.Rules.Add(rule);
            }
            else
            {
                var rule = new LoggerFilterRule(LoggerSettings.DefaultProviderName, LoggerSettings.DefaultName, LogLevel.Information, null);
                rule.LogType = LogType.All;
                options.Rules.Add(rule);
            }
        }
    }
}
