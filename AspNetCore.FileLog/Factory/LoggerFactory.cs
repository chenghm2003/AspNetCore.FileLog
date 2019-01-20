using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;

namespace AspNetCore.FileLog
{
    internal class LoggerFactory : ILoggerFactory, IDisposable
    {
        internal LoggerExternalScopeProvider ScopeProvider { get; private set; }

        internal static IServiceCollection ServiceCollection { get; set; }

        internal static IServiceProvider ServiceProvider { get; set; }

        public LoggerFactory(IHostingEnvironment environment, IEnumerable<ILoggerProvider> providers, IHttpContextAccessor httpContextAccessor, IServiceProvider serviceProvider, ILogAdapter logAdapter, IOptionsMonitor<LoggerFilterOptions> filterOption)
        {
            LoggerFactory.ServiceProvider = serviceProvider;
            this._httpContextAccessor = httpContextAccessor;
            this._environment = environment;
            bool flag = string.IsNullOrEmpty(LoggerSettings.LogDirectory);
            if (flag)
            {
                LoggerSettings.LogDirectory = Path.Combine(this._environment.ContentRootPath ?? AppContext.BaseDirectory, ".Logs");
            }
            bool flag2 = !Directory.Exists(LoggerSettings.LogDirectory);
            if (flag2)
            {
                Directory.CreateDirectory(LoggerSettings.LogDirectory);
            }
            foreach (ILoggerProvider provider in providers)
            {
                this.AddProviderRegistration(provider, false);
            }
            this.RefreshFilters(filterOption.CurrentValue, string.Empty);
            this._changeTokenRegistration = filterOption.OnChange(new Action<LoggerFilterOptions, string>(this.RefreshFilters));
            this._logAdapter = logAdapter;
        }

        private void RefreshFilters(LoggerFilterOptions filterOptions, string value)
        {
            object sync = LoggerFactory._sync;
            lock (sync)
            {
                LoggerFactory._filterOptions = new LoggerFilterOptions(filterOptions);
                foreach (KeyValuePair<string, Logger> logger in LoggerFactory._loggers)
                {
                    LoggerInformation[] loggerInformation = logger.Value.Loggers;
                    string categoryName = logger.Key;
                    this.ApplyRules(loggerInformation, categoryName, 0, loggerInformation.Length);
                }
            }
        }
        

        public void AddProvider(ILoggerProvider provider)
        {
            bool flag = this.CheckDisposed();
            if (flag)
            {
                throw new ObjectDisposedException("LoggerFactory");
            }
            this.AddProviderRegistration(provider, true);
            object sync = LoggerFactory._sync;
            lock (sync)
            {
                foreach (KeyValuePair<string, Logger> logger in LoggerFactory._loggers)
                {
                    LoggerInformation[] loggerInformation = logger.Value.Loggers;
                    string categoryName = logger.Key;
                    Array.Resize<LoggerInformation>(ref loggerInformation, loggerInformation.Length + 1);
                    int newLoggerIndex = loggerInformation.Length - 1;
                    this.SetLoggerInformation(ref loggerInformation[newLoggerIndex], provider, categoryName);
                    this.ApplyRules(loggerInformation, categoryName, newLoggerIndex, 1);
                    logger.Value.Loggers = loggerInformation;
                }
            }
        }

        public ILogger CreateLogger(string categoryName)
        {
            bool flag = this.CheckDisposed();
            if (flag)
            {
                throw new ObjectDisposedException("LoggerFactory");
            }
            object sync = LoggerFactory._sync;
            ILogger result;
            lock (sync)
            {
                Logger logger;
                bool flag3 = !LoggerFactory._loggers.TryGetValue(categoryName, out logger);
                if (flag3)
                {
                    logger = new Logger(this, this._httpContextAccessor, this._logAdapter)
                    {
                        Loggers = this.CreateLoggers(categoryName)
                    };
                    LoggerFactory._loggers[categoryName] = logger;
                }
                result = logger;
            }
            return result;
        }
        private void AddProviderRegistration(ILoggerProvider provider, bool dispose)
        {
            this._providerRegistrations.Add(new LoggerFactory.ProviderRegistration
            {
                Provider = provider,
                ShouldDispose = dispose
            });
            ISupportExternalScope supportsExternalScope;
            bool flag = (supportsExternalScope = (provider as ISupportExternalScope)) != null;
            if (flag)
            {
                bool flag2 = this.ScopeProvider == null;
                if (flag2)
                {
                    this.ScopeProvider = new LoggerExternalScopeProvider();
                }
                supportsExternalScope.SetScopeProvider(this.ScopeProvider);
            }
        }

        private void SetLoggerInformation(ref LoggerInformation loggerInformation, ILoggerProvider provider, string categoryName)
        {
            loggerInformation.Logger = provider.CreateLogger(categoryName);
            loggerInformation.ProviderType = provider.GetType();
            loggerInformation.ExternalScope = (provider is ISupportExternalScope);
        }

        private LoggerInformation[] CreateLoggers(string categoryName)
        {
            LoggerInformation[] loggers = new LoggerInformation[this._providerRegistrations.Count];
            for (int i = 0; i < this._providerRegistrations.Count; i++)
            {
                this.SetLoggerInformation(ref loggers[i], this._providerRegistrations[i].Provider, categoryName);
            }
            this.ApplyRules(loggers, categoryName, 0, loggers.Length);
            return loggers;
        }

        private void ApplyRules(LoggerInformation[] loggers, string categoryName, int start, int count)
        {
            for (int index = start; index < start + count; index++)
            {
                ref LoggerInformation loggerInformation = ref loggers[index];
                LogType logType;
                LogLevel? minLevel;
                int traceCount;
                Func<string, string, LogLevel, bool> filter;
                LoggerFactory.RuleSelector.Select(LoggerFactory._filterOptions, loggerInformation.ProviderType, categoryName, out logType, out minLevel, out traceCount, out filter);
                loggerInformation.Rule = new LoggerFilterRule("Logging", categoryName, minLevel, filter);
                loggerInformation.Rule.LogType = logType;
                loggerInformation.Filter = filter;
                loggerInformation.Rule.TraceCount = traceCount;
            }
        }

        protected virtual bool CheckDisposed()
        {
            return this._disposed;
        }

        public void Dispose()
        {
            bool flag = !this._disposed;
            if (flag)
            {
                this._disposed = true;
                IDisposable changeTokenRegistration = this._changeTokenRegistration;
                if (changeTokenRegistration != null)
                {
                    changeTokenRegistration.Dispose();
                }
                foreach (LoggerFactory.ProviderRegistration registration in this._providerRegistrations)
                {
                    try
                    {
                        bool shouldDispose = registration.ShouldDispose;
                        if (shouldDispose)
                        {
                            registration.Provider.Dispose();
                        }
                    }
                    catch
                    {
                    }
                }
            }
        }
        
        private static readonly LoggerRuleSelector RuleSelector = new LoggerRuleSelector();
        
        internal static readonly Dictionary<string, Logger> _loggers = new Dictionary<string, Logger>(StringComparer.Ordinal);
        
        private readonly List<LoggerFactory.ProviderRegistration> _providerRegistrations = new List<LoggerFactory.ProviderRegistration>();
        
        private static readonly object _sync = new object();
        
        private volatile bool _disposed;
        
        private IDisposable _changeTokenRegistration;
        
        internal static LoggerFilterOptions _filterOptions;
        
        private IHostingEnvironment _environment;
        
        private IHttpContextAccessor _httpContextAccessor;

        private ILogAdapter _logAdapter;
        
        private struct ProviderRegistration
        {
            public ILoggerProvider Provider;
            public bool ShouldDispose;
        }
    }
}
