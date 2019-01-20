using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace AspNetCore.FileLog
{
    /// <summary>
    /// 日志规则选择器
    /// </summary>
    internal class LoggerRuleSelector
    {
        public void Select(LoggerFilterOptions options, Type providerType, string category, out LogType logType, out LogLevel? minLevel, out int traceCount, out Func<string, string, LogLevel, bool> filter)
        {
            filter = null;
            minLevel = new Microsoft.Extensions.Logging.LogLevel?(options.MinLevel);
            logType = options.MiniType;
            traceCount = options.TraceCount;
            string providerAlias = LoggerProviderAliasUtilities.GetAlias(providerType);
            LoggerFilterRule current = null;
            foreach (LoggerFilterRule loggerFilterRule in options.Rules)
            {
                LoggerFilterRule rule = (LoggerFilterRule)loggerFilterRule;
                bool flag = LoggerRuleSelector.IsBetter(rule, current, providerType.FullName, category) || (!string.IsNullOrEmpty(providerAlias) && LoggerRuleSelector.IsBetter(rule, current, providerAlias, category));
                if (flag)
                {
                    current = rule;
                }
            }
            bool flag2 = current != null;
            if (flag2)
            {
                filter = current.Filter;
                minLevel = current.LogLevel;
                logType = current.LogType;
                traceCount = current.TraceCount;
            }
        }
        
        private static bool IsBetter(LoggerFilterRule rule, LoggerFilterRule current, string logger, string category)
        {
            bool flag = rule.ProviderName != null && rule.ProviderName != logger;
            bool result;
            if (flag)
            {
                result = false;
            }
            else
            {
                bool flag2 = rule.CategoryName != null && !category.StartsWith(rule.CategoryName, StringComparison.OrdinalIgnoreCase);
                if (flag2)
                {
                    result = false;
                }
                else
                {
                    bool flag3 = ((current != null) ? current.ProviderName : null) != null;
                    if (flag3)
                    {
                        bool flag4 = rule.ProviderName == null;
                        if (flag4)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        bool flag5 = rule.ProviderName != null;
                        if (flag5)
                        {
                            return true;
                        }
                    }
                    bool flag6 = ((current != null) ? current.CategoryName : null) != null;
                    if (flag6)
                    {
                        bool flag7 = rule.CategoryName == null;
                        if (flag7)
                        {
                            return false;
                        }
                        bool flag8 = current.CategoryName.Length > rule.CategoryName.Length;
                        if (flag8)
                        {
                            return false;
                        }
                    }
                    result = true;
                }
            }
            return result;
        }
    }
}
