using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Text;

namespace AspNetCore.FileLog
{
    [DebuggerDisplay("{Rule.CategoryName}")]
    internal struct LoggerInformation
    {
        public ILogger Logger { get; set; }
        
        public LoggerFilterRule Rule { get; set; }
        
        public Type ProviderType { get; set; }
        
        public Func<string, string, LogLevel, bool> Filter { get; set; }
        
        public bool ExternalScope { get; set; }
        
        public bool IsEnabled(LogLevel level)
        {
            bool flag;
            if (this.Rule.LogLevel != null)
            {
                LogLevel? logLevel = this.Rule.LogLevel;
                flag = (level < logLevel.GetValueOrDefault() & logLevel != null);
            }
            else
            {
                flag = false;
            }
            bool flag2 = flag;
            bool result;
            if (flag2)
            {
                result = false;
            }
            else
            {
                bool flag3 = this.Filter != null;
                result = (!flag3 || this.Filter(this.ProviderType.FullName, this.Rule.CategoryName, level));
            }
            return result;
        }
    }
}
