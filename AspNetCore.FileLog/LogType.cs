using System;
using System.Collections.Generic;
using System.Text;

namespace AspNetCore.FileLog
{
    public enum LogType
    {
        /// <summary>
        /// None
        /// </summary>
        None,
        /// <summary>
        /// Contains http request information
        /// </summary>
        HttpContext,
        /// <summary>
        /// Contains stack trace information
        /// </summary>
        Trace,
        /// <summary>
        /// Contains all information
        /// </summary>
        All
    }
}
