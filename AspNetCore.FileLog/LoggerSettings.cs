using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetCore.FileLog
{
    internal class LoggerSettings
    {
        internal static string LogRequestPath { get; set; }

        internal static string SettingsPath { get; set; }

        internal static string LogDirectory { get; set; }

        internal static Format Format { get; set; }

        internal static JsonConfigurationProvider JsonConfiguration { get; set; }

        static LoggerSettings()
        {
            bool flag = string.IsNullOrEmpty(LoggerSettings.html);
            if (flag)
            {
                using (StreamReader sr = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("AspNetCore.FileLog.Source.settingsPage.html")))
                {
                    LoggerSettings.html = sr.ReadToEnd();
                }
            }
        }

        public LoggerSettings(RequestDelegate next)
        {
            this._next = next;
            this.SavePath = LoggerSettings.SettingsPath + "/Save";
            this.SavePath = this.SavePath.Replace("//", "/");
        }

        public async Task Invoke(HttpContext context)
        {
            LoggerFilterOptions _filterOption = LoggerFactory._filterOptions;
            bool flag = context.Request.Path.StartsWithSegments(this.SavePath);
            if (flag)
            {
                string logFilePath = null;
                bool flag2 = LoggerSettings.JsonConfiguration != null;
                if (flag2)
                {
                    IFileInfo fi = LoggerSettings.JsonConfiguration.Source.FileProvider.GetFileInfo(LoggerSettings.JsonConfiguration.Source.Path);
                    bool flag3 = !fi.IsDirectory;
                    if (flag3)
                    {
                        logFilePath = fi.PhysicalPath;
                    }
                    FileInfo fileInfo = new FileInfo(logFilePath);
                    bool flag4 = !fileInfo.Directory.Exists;
                    if (flag4)
                    {
                        fileInfo.Directory.Create();
                    }
                    fi = null;
                    fileInfo = null;
                }
                bool flag5 = string.IsNullOrEmpty(logFilePath);
                if (flag5)
                {
                    logFilePath = Path.Combine(LoggerSettings.LogDirectory, "_logging.json");
                }
                string json = string.Empty;
                using (StreamReader _sr = new StreamReader(context.Request.Body))
                {
                    json = _sr.ReadToEnd();
                }
                File.WriteAllText(logFilePath, json);
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{message:'ok',status:200}", default(CancellationToken));
                logFilePath = null;
                json = null;
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                string[] levels = Enum.GetNames(typeof(LogLevel));
                string[] types = Enum.GetNames(typeof(LogType));
                sb.AppendLine("<header><h1><a href=\"" + LoggerSettings.LogRequestPath + "\">Logs</a></h1><br>Set Default : <select id='defaltLevel' onchange='_setDefault(this,\"_level\")'><option>--Log Level--</option>");
                sb.Append(string.Join("", from t in levels
                                          select "<option>" + t + "</option>"));
                sb.Append("</select> &nbsp;&nbsp; Default Type: <select id='defaltType' onchange='_setDefault(this,\"_type\")'><option>--Log Type--</option>");
                sb.Append(string.Join("", from t in types
                                          select "<option>" + t + "</option>"));
                sb.Append("</select>");
                sb.Append(" &nbsp;&nbsp; TraceCount: <input type=text id='traceCount' value='' onchange='_setDefault(this,\"_count\")'/>");
                sb.Append(" &nbsp;<button type='button' onclick='_save()'>Save</button></header>");
                SortedDictionary<string, LoggerFilterRule> rules = new SortedDictionary<string, LoggerFilterRule>();
                SortedDictionary<string, LogLevel> levelValues = new SortedDictionary<string, LogLevel>();
                SortedDictionary<string, LogType> typeValues = new SortedDictionary<string, LogType>();
                foreach (LoggerFilterRule loggerFilterRule in _filterOption.Rules)
                {
                    LoggerFilterRule rule = (LoggerFilterRule)loggerFilterRule;
                    rules[rule.CategoryName ?? "Default"] = rule;
                    rule = null;
                }
                foreach (KeyValuePair<string, Logger> log in from t in LoggerFactory._loggers
                                                             orderby t.Key
                                                             select t)
                {
                    if (log.Value.Loggers.Length != 0)
                    {
                        rules[log.Key] = log.Value.Loggers[0].Rule;
                    }
                }
                sb.AppendLine();
                var objs = from k in rules
                           select k.Value into t
                           select new
                           {
                               Name = t.CategoryName,
                               LogType = t.LogType.ToString(),
                               LogLevel = t.LogLevel.ToString(),
                               TraceCount = t.TraceCount
                           };
                sb.AppendLine("<script>var rules=" + objs.ToJson() + ";</script>");
                await context.Response.WriteAsync(LoggerSettings.html.Replace("{{url}}", this.SavePath).Replace("{{body}}", sb.ToString()), default(CancellationToken));
                sb = null;
                levels = null;
                types = null;
                rules = null;
                objs = null;
            }
        }

        public const int TraceCount = 5;
        
        private const string ResourceName = "AspNetCore.FileLog.Source.settingsPage.html";

        public const string LogJsonFileName = "_logging.json";

        public const string RulesKey = "Rules";
        
        public const string DefaultName = "Default";

        internal const string DefaultProviderName = "Logging";

        private string SavePath = "/_Settings_/Save";

        private readonly RequestDelegate _next;
        
        private static readonly string html;

        internal const string LoggingJsonContent = "{\"Rules\":[{\"CategoryName\":\"Default\",\"LogLevel\":\"Information\",\"LogType\":\"All\",\"TraceCount\":5}]}";
    }
}
