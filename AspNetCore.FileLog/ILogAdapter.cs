using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;

namespace AspNetCore.FileLog
{
    public interface ILogAdapter
    {
        /// <summary>
        ///
        /// </summary>
        string FileDirectory { get; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="category"></param>
        /// <param name="eventName"></param>
        /// <param name="logLevel"></param>
        /// <param name="stackFrames"></param>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        /// <param name="context"></param>
        /// <param name="format"></param>
        void Log(string category, string eventName, LogLevel logLevel, Format format, string message, StackFrame[] stackFrames, Exception exception, HttpContext context);
    }
}
