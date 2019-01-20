using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace AspNetCore.FileLog
{
    internal class LoggerFilterOptions: Microsoft.Extensions.Logging.LoggerFilterOptions
    {
        public LoggerFilterOptions(LoggerFilterOptions options)
        {
            base.MinLevel = LogLevel.Information;
            this.MiniType = LogType.None;
            foreach (LoggerFilterRule loggerFilterRule in options.Rules)
            {
                LoggerFilterRule rule = (LoggerFilterRule)loggerFilterRule;
                bool flag = rule.CategoryName.Equals("Default");
                if (flag)
                {
                    this.MiniType = rule.LogType;
                    base.MinLevel = (rule.LogLevel ?? LogLevel.Information);
                    this.TraceCount = rule.TraceCount;
                }
                base.Rules.Add(rule);
            }
        }
        public LogType MiniType { get; set; }
        public int TraceCount { get; set; }
    }
}
