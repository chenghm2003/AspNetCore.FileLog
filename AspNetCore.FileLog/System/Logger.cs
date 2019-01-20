using AspNetCore.FileLog;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using LoggerExtensions = Microsoft.Extensions.Logging.LoggerExtensions;
using LoggerFactory = AspNetCore.FileLog.LoggerFactory;

namespace System
{
    /// <summary>
    /// Logger
    /// </summary>
    /// <summary>
    /// Logger
    /// </summary>
    public class Logger : ILogger
    {
        internal Logger(LoggerFactory loggerFactory, IHttpContextAccessor httpContextAccessor, ILogAdapter logAdapter)
        {
            this._loggerFactory = loggerFactory;
            this._httpContextAccessor = httpContextAccessor;
            this._logAdapter = logAdapter;
        }        
        internal LoggerInformation[] Loggers
        {
            get
            {
                return this._loggers;
            }
            set
            {
                int scopeSize = 0;
                for (int i = 0; i < value.Length; i++)
                {
                    LoggerInformation loggerInformation = value[i];
                    bool flag = !loggerInformation.ExternalScope;
                    if (flag)
                    {
                        scopeSize++;
                    }
                }
                this._scopeCount = scopeSize;
                this._loggers = value;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        /// <param name="logLevel"></param>
        /// <param name="eventId"></param>
        /// <param name="state"></param>
        /// <param name="exception"></param>
        /// <param name="formatter"></param>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            LoggerInformation[] loggers = this.Loggers;
            bool flag = loggers == null;
            if (!flag)
            {
                List<Exception> exceptions = null;
                bool loged = false;
                foreach (LoggerInformation loggerInfo in loggers)
                {
                    bool flag2 = !loggerInfo.IsEnabled(logLevel);
                    if (!flag2)
                    {
                        try
                        {
                            loggerInfo.Logger.Log<TState>(logLevel, eventId, state, exception, formatter);
                            bool flag3 = !loged;
                            if (flag3)
                            {
                                loged = true;
                                AspNetCore.FileLog.LoggerFilterRule rule = loggerInfo.Rule;
                                bool flag4 = exception == null && rule.LogType.HasFlag(LogType.Trace);
                                StackFrame[] stackFrames;
                                if (flag4)
                                {
                                    stackFrames = new StackTrace(false).GetFrames(rule.TraceCount, new Func<StackFrame, MethodBase, bool>(this.CanSkip));
                                }
                                else
                                {
                                    stackFrames = Array.Empty<StackFrame>();
                                }
                                HttpContext context = null;
                                bool flag5 = rule.LogType.HasFlag(LogType.HttpContext);
                                if (flag5)
                                {
                                    IHttpContextAccessor httpContextAccessor = this._httpContextAccessor;
                                    context = ((httpContextAccessor != null) ? httpContextAccessor.HttpContext : null);
                                }
                                this._logAdapter.Log(rule.CategoryName, eventId.Name, logLevel, LoggerSettings.Format, formatter(state, exception), stackFrames, exception, context);
                            }
                        }
                        catch (Exception ex)
                        {
                            bool flag6 = exceptions == null;
                            if (flag6)
                            {
                                exceptions = new List<Exception>();
                            }
                            exceptions.Add(ex);
                        }
                    }
                }
                bool flag7 = exceptions != null && exceptions.Count > 0;
                if (flag7)
                {
                    throw new AggregateException("An error occurred while writing to logger(s).", exceptions);
                }
            }
        }
        
        private bool CanSkip(StackFrame frame, MethodBase method)
        {
            bool canSkip = Logger.IgnoreTypes.Contains(method.DeclaringType);
            bool flag = !canSkip;
            if (flag)
            {
                Type declaringType = method.DeclaringType;
                Type type = (declaringType != null) ? declaringType.ReflectedType : null;
                bool flag2 = type != null;
                if (flag2)
                {
                    canSkip = Logger.IgnoreTypes.Contains(type);
                }
            }
            return canSkip;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="logLevel"></param>
        /// <returns></returns>
        // Token: 0x0600003A RID: 58 RVA: 0x00003098 File Offset: 0x00001298
        public bool IsEnabled(LogLevel logLevel)
        {
            LoggerInformation[] loggers = this.Loggers;
            bool flag = loggers == null;
            bool result;
            if (flag)
            {
                result = false;
            }
            else
            {
                List<Exception> exceptions = null;
                foreach (LoggerInformation loggerInfo in loggers)
                {
                    bool flag2 = !loggerInfo.IsEnabled(logLevel);
                    if (!flag2)
                    {
                        try
                        {
                            bool flag3 = loggerInfo.Logger.IsEnabled(logLevel);
                            if (flag3)
                            {
                                return true;
                            }
                        }
                        catch (Exception ex)
                        {
                            bool flag4 = exceptions == null;
                            if (flag4)
                            {
                                exceptions = new List<Exception>();
                            }
                            exceptions.Add(ex);
                        }
                    }
                }
                bool flag5 = exceptions != null && exceptions.Count > 0;
                if (flag5)
                {
                    throw new AggregateException("An error occurred while writing to logger(s).", exceptions);
                }
                result = false;
            }
            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        /// <param name="state"></param>
        /// <returns></returns>
        public IDisposable BeginScope<TState>(TState state)
        {
            LoggerInformation[] loggers = this.Loggers;
            bool flag = loggers == null;
            IDisposable result;
            if (flag)
            {
                result = NullScope.Instance;
            }
            else
            {
                LoggerExternalScopeProvider scopeProvider = this._loggerFactory.ScopeProvider;
                int scopeCount = this._scopeCount;
                bool flag2 = scopeProvider != null;
                if (flag2)
                {
                    bool flag3 = scopeCount == 0;
                    if (flag3)
                    {
                        return scopeProvider.Push(state);
                    }
                    scopeCount++;
                }
                Logger.Scope scope = new Logger.Scope(scopeCount);
                List<Exception> exceptions = null;
                foreach (LoggerInformation loggerInformation in loggers)
                {
                    bool externalScope = loggerInformation.ExternalScope;
                    if (!externalScope)
                    {
                        try
                        {
                            scopeCount--;
                            bool flag4 = scopeCount >= 0;
                            if (flag4)
                            {
                                IDisposable disposable = loggerInformation.Logger.BeginScope<TState>(state);
                                scope.SetDisposable(scopeCount, disposable);
                            }
                        }
                        catch (Exception ex)
                        {
                            bool flag5 = exceptions == null;
                            if (flag5)
                            {
                                exceptions = new List<Exception>();
                            }
                            exceptions.Add(ex);
                        }
                    }
                }
                bool flag6 = scopeProvider != null;
                if (flag6)
                {
                    scope.SetDisposable(0, scopeProvider.Push(state));
                }
                bool flag7 = exceptions != null && exceptions.Count > 0;
                if (flag7)
                {
                    throw new AggregateException("An error occurred while writing to logger(s).", exceptions);
                }
                result = scope;
            }
            return result;
        }
        
        private static void WriteLog(string categoryName, EventId eventId, LogLevel level, string message, Exception exception)
        {
            bool flag = string.IsNullOrEmpty(categoryName);
            if (flag)
            {
                throw new ArgumentNullException("categoryName");
            }
            bool flag2 = LoggerFactory.ServiceProvider == null;
            ILoggerFactory factory;
            if (flag2)
            {
                IServiceCollection services = LoggerFactory.ServiceCollection ?? new ServiceCollection();
                services.AddFileLog(null);
                factory = ServiceCollectionContainerBuilderExtensions.BuildServiceProvider(services).GetService<ILoggerFactory>();
            }
            else
            {
                factory = LoggerFactory.ServiceProvider.GetService<ILoggerFactory>();
            }
            ILogger logger = factory.CreateLogger(categoryName);
            logger.Log(level, eventId, exception, message, Array.Empty<object>());            
        }

        /// <summary>
        /// writes a trace log message.
        /// <para>Level: 1</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message">message</param>
        /// <param name="exception"><see cref="T:System.Exception" /></param>
        /// <param name="eventId"><see cref="T:Microsoft.Extensions.Logging.EventId" /></param>
        public static void Trace<T>(string message = null, Exception exception = null, EventId eventId = default(EventId)) where T : class
        {
            Logger.WriteLog(typeof(T).FullName, eventId, LogLevel.Trace, message, exception);
        }

        /// <summary>
        /// writes a trace log message.
        /// <para>Level: 1</para>
        /// </summary>
        /// <param name="categoryName">log category name</param>
        /// <param name="message">message</param>
        /// <param name="exception"><see cref="T:System.Exception" /></param>
        /// <param name="eventId"><see cref="T:Microsoft.Extensions.Logging.EventId" /></param>
        public static void Trace(string categoryName, string message = null, Exception exception = null, EventId eventId = default(EventId))
        {
            Logger.WriteLog(categoryName, eventId, LogLevel.Trace, message, exception);
        }

        /// <summary>
        /// Formats and writes a debug log message.
        /// <para>Level: 1</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message">message</param>
        /// <param name="exception"><see cref="T:System.Exception" /></param>
        /// <param name="eventId"><see cref="T:Microsoft.Extensions.Logging.EventId" /></param>
        public static void Debug<T>(string message = null, Exception exception = null, EventId eventId = default(EventId)) where T : class
        {
            Logger.WriteLog(typeof(T).FullName, eventId, LogLevel.Debug, message, exception);
        }

        /// <summary>
        /// Formats and writes a debug log message.
        /// <para>Level: 1</para>
        /// </summary>
        /// <param name="categoryName"></param>
        /// <param name="message">message</param>
        /// <param name="exception"><see cref="T:System.Exception" /></param>
        /// <param name="eventId"><see cref="T:Microsoft.Extensions.Logging.EventId" /></param>
        public static void Debug(string categoryName, string message = null, Exception exception = null, EventId eventId = default(EventId))
        {
            Logger.WriteLog(categoryName, eventId, LogLevel.Debug, message, exception);
        }

        /// <summary>
        /// writes an informational log message.
        /// <para>Level: 2</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message">message</param>
        /// <param name="exception"><see cref="T:System.Exception" /></param>
        /// <param name="eventId"><see cref="T:Microsoft.Extensions.Logging.EventId" /></param>
        public static void Information<T>(string message = null, Exception exception = null, EventId eventId = default(EventId)) where T : class
        {
            Logger.WriteLog(typeof(T).FullName, eventId, LogLevel.Information, message, exception);
        }

        /// <summary>
        /// writes an informational log message.
        /// <para>Level: 2</para>
        /// </summary>
        /// <param name="categoryName"></param>
        /// <param name="message">message</param>
        /// <param name="exception"><see cref="T:System.Exception" /></param>
        /// <param name="eventId"><see cref="T:Microsoft.Extensions.Logging.EventId" /></param>
        public static void Information(string categoryName, string message = null, Exception exception = null, EventId eventId = default(EventId))
        {
            Logger.WriteLog(categoryName, eventId, LogLevel.Information, message, exception);
        }

        /// <summary>
        /// writes a warning log message.
        /// <para>Level: 3</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message">message</param>
        /// <param name="exception"><see cref="T:System.Exception" /></param>
        /// <param name="eventId"><see cref="T:Microsoft.Extensions.Logging.EventId" /></param>
        public static void Warning<T>(string message = null, Exception exception = null, EventId eventId = default(EventId)) where T : class
        {
            Logger.WriteLog(typeof(T).FullName, eventId, LogLevel.Warning, message, exception);
        }

        /// <summary>
        /// writes a warning log message.
        /// <para>Level: 3</para>
        /// </summary>
        /// <param name="categoryName"></param>
        /// <param name="message">message</param>
        /// <param name="exception"><see cref="T:System.Exception" /></param>
        /// <param name="eventId"><see cref="T:Microsoft.Extensions.Logging.EventId" /></param>
        public static void Warning(string categoryName, string message = null, Exception exception = null, EventId eventId = default(EventId))
        {
            Logger.WriteLog(categoryName, eventId, LogLevel.Warning, message, exception);
        }

        /// <summary>
        /// writes an error log message.
        /// <para>Level: 4</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message">message</param>
        /// <param name="exception"><see cref="T:System.Exception" /></param>
        /// <param name="eventId"><see cref="T:Microsoft.Extensions.Logging.EventId" /></param>
        public static void Error<T>(string message = null, Exception exception = null, EventId eventId = default(EventId)) where T : class
        {
            Logger.WriteLog(typeof(T).FullName, eventId, LogLevel.Error, message, exception);
        }

        /// <summary>
        /// writes an error log message.
        /// <para>Level: 4</para>
        /// </summary>
        /// <param name="categoryName"></param>
        /// <param name="message">message</param>
        /// <param name="exception"><see cref="T:System.Exception" /></param>
        /// <param name="eventId"><see cref="T:Microsoft.Extensions.Logging.EventId" /></param>
        public static void Error(string categoryName, string message = null, Exception exception = null, EventId eventId = default(EventId))
        {
            Logger.WriteLog(categoryName, eventId, LogLevel.Error, message, exception);
        }

        /// <summary>
        /// writes a critical log message.
        /// <para>Level: 5</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message">message</param>
        /// <param name="exception"><see cref="T:System.Exception" /></param>
        /// <param name="eventId"><see cref="T:Microsoft.Extensions.Logging.EventId" /></param>
        public static void Critical<T>(string message = null, Exception exception = null, EventId eventId = default(EventId)) where T : class
        {
            Logger.WriteLog(typeof(T).FullName, eventId, LogLevel.Critical, message, exception);
        }

        /// <summary>
        /// writes a critical log message.
        /// <para>Level: 5</para>
        /// </summary>
        /// <param name="categoryName"></param>
        /// <param name="message">message</param>
        /// <param name="exception"><see cref="T:System.Exception" /></param>
        /// <param name="eventId"><see cref="T:Microsoft.Extensions.Logging.EventId" /></param>
        public static void Critical(string categoryName, string message = null, Exception exception = null, EventId eventId = default(EventId))
        {
            Logger.WriteLog(categoryName, eventId, LogLevel.Critical, message, exception);
        }

        private static readonly Type[] IgnoreTypes = new Type[]
        {
            typeof(Logger),
            typeof(LoggerExtensions),
            typeof(Logger),
            typeof(LoggerMessage),
            typeof(FileLogAdapter)
        };
        
        private readonly AspNetCore.FileLog.LoggerFactory _loggerFactory;
        
        private LoggerInformation[] _loggers;
        
        private ILogAdapter _logAdapter;
        
        private int _scopeCount;
        
        private IHttpContextAccessor _httpContextAccessor;
        
        private class Scope : IDisposable
        {
            public Scope(int count)
            {
                bool flag = count > 2;
                if (flag)
                {
                    this._disposable = new IDisposable[count - 2];
                }
            }
            
            public void SetDisposable(int index, IDisposable disposable)
            {
                if (index != 0)
                {
                    if (index != 1)
                    {
                        this._disposable[index - 2] = disposable;
                    }
                    else
                    {
                        this._disposable1 = disposable;
                    }
                }
                else
                {
                    this._disposable0 = disposable;
                }
            }
            
            public void Dispose()
            {
                bool flag = !this._isDisposed;
                if (flag)
                {
                    IDisposable disposable = this._disposable0;
                    if (disposable != null)
                    {
                        disposable.Dispose();
                    }
                    IDisposable disposable2 = this._disposable1;
                    if (disposable2 != null)
                    {
                        disposable2.Dispose();
                    }
                    bool flag2 = this._disposable != null;
                    if (flag2)
                    {
                        int count = this._disposable.Length;
                        for (int index = 0; index != count; index++)
                        {
                            bool flag3 = this._disposable[index] != null;
                            if (flag3)
                            {
                                this._disposable[index].Dispose();
                            }
                        }
                    }
                    this._isDisposed = true;
                }
            }
            
            private bool _isDisposed;
            
            private IDisposable _disposable0;
            
            private IDisposable _disposable1;

            private readonly IDisposable[] _disposable;
        }
    }
}
