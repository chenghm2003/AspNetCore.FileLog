using Microsoft.Extensions.Logging;
using System;

namespace AspNetCore.FileLog
{
    /// <summary>
    /// 日志过滤规则
    /// </summary>
    internal class LoggerFilterRule: Microsoft.Extensions.Logging.LoggerFilterRule
    {
        public static LoggerFilterRule Default { get; } = LoggerFilterRule.CreateDefault();

        public static LoggerFilterRule CreateDefault()
        {
            return new LoggerFilterRule("Logging", "Default", new Microsoft.Extensions.Logging.LogLevel?(Microsoft.Extensions.Logging.LogLevel.Information), null);
        }

        public LoggerFilterRule(string providerName, string categoryName, LogLevel? logLevel, Func<string, string, LogLevel, bool> filter) : base(providerName, categoryName, logLevel, filter)
        {
        }

        public LogType LogType { get; set; }

        public int TraceCount { get; set; } = 5;

        public override string ToString()
        {
            return string.Format("{0}: '{1}', {2}: '{3}', {4}: '{5}', {6}: '{7}', {8}: '{9}'", new object[]
            {
                "ProviderName",
                base.ProviderName,
                "CategoryName",
                base.CategoryName,
                "LogLevel",
                base.LogLevel,
                "LogType",
                this.LogType,
                "Filter",
                base.Filter
            });
        }
    }
}
