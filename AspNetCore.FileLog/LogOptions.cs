using System;
using System.Collections.Generic;
using System.Text;

namespace AspNetCore.FileLog
{
    /// <summary>
    /// log options
    /// </summary>
    public class LogOptions
    {
        /// <summary>
        /// request url
        /// <para>
        /// default: "/_Logs_"
        /// </para>
        /// </summary>
        public string LogRequestPath { get; set; } = "/_Logs_";

        /// <summary>
        /// settings url
        /// <para>
        /// default: "/_Settings_"
        /// </para>
        /// </summary>
        public string SettingsPath { get; set; } = "/_Settings_";

        /// <summary>
        /// Log file physical directory
        /// <para>
        /// default: ".Logs" of application path
        /// </para>
        /// </summary>
        public string LogDirectory { get; set; }

        /// <summary>
        /// log file saved format
        /// <para>
        /// default: <see cref="F:AspNetCore.FileLog.Format.Txt" />
        /// </para>
        /// </summary>
        public Format Format { get; set; }
    }
}
