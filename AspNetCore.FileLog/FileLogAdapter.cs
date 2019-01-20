using AspNetCore.FileLog.System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Threading;

namespace AspNetCore.FileLog
{
    /// <summary>
    ///
    /// </summary>
    // Token: 0x02000010 RID: 16
    public class FileLogAdapter : ILogAdapter
    {
        /// <summary>
        ///
        /// </summary>
        public FileLogAdapter()
        {
            this.FileDirectory = LoggerSettings.LogDirectory;
            this.htmlEncoder = HtmlEncoder.Default;
        }

        /// <summary>
        ///
        /// </summary>
        public string FileDirectory { get; }

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
        public virtual void Log(string category, string eventName, LogLevel logLevel, Format format, string message, StackFrame[] stackFrames, Exception exception, HttpContext context)
        {
            if (format != Format.Txt)
            {
                if (format == Format.Markdown)
                {
                    this.LogMarkdown(category, eventName, logLevel, message, stackFrames, exception, context);
                }
            }
            else
            {
                this.LogTxt(category, eventName, logLevel, message, stackFrames, exception, context);
            }
        }

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
        protected virtual void LogTxt(string category, string eventName, LogLevel logLevel, string message, StackFrame[] stackFrames, Exception exception, HttpContext context)
        {
            string path = string.Empty;
            bool flag = !string.IsNullOrEmpty(eventName);
            if (flag)
            {
                int index = eventName.LastIndexOf('.');
                string name = eventName;
                bool flag2 = index > 0;
                if (flag2)
                {
                    name = eventName.Remove(0, eventName.LastIndexOf('.') + 1);
                }
                path = Path.Combine(new string[]
                {
                    this.FileDirectory,
                    ((exception != null) ? LogLevel.Error : logLevel).ToString(),
                    category,
                    name,
                    DateTime.Now.ToString("yyyyMMdd") + ".txt"
                });
            }
            else
            {
                path = Path.Combine(this.FileDirectory, ((exception != null) ? LogLevel.Error : logLevel).ToString(), category, DateTime.Now.ToString("yyyyMMdd") + ".txt");
            }
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("#################### " + DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fffffff") + " ###############");
            string excetionDetails = null;
            bool flag3 = message == "[null]" && exception != null;
            if (flag3)
            {
                sb.AppendLine(FileLogAdapter.MessageTitle[0] + exception.GetString(ExceptionType.Message).ToString().Trim());
                excetionDetails = exception.GetString(ExceptionType.Details).ToString().Trim();
                exception = null;
            }
            else
            {
                sb.AppendLine(FileLogAdapter.MessageTitle[0] + FileLogAdapter.NewLineRegex.Replace(message.TrimStart(new char[]
                {
                    '\n',
                    '\r'
                }), "$1\t    "));
            }
            bool flag4 = context != null;
            if (flag4)
            {
                bool hasValue = context.Request.Host.HasValue;
                if (hasValue)
                {
                    sb.AppendLine(string.Format("{0}{1}://{2}{3}{4}", new object[]
                    {
                        FileLogAdapter.MessageTitle[1],
                        context.Request.Scheme,
                        context.Request.Host,
                        context.Request.Path,
                        context.Request.QueryString
                    }));
                }
                sb.AppendLine(FileLogAdapter.MessageTitle[2] + context.Request.Method);
                sb.AppendLine(string.Format("{0}{1}", FileLogAdapter.MessageTitle[3], context.Connection.RemoteIpAddress));
                bool flag5 = context.Request.Headers["User-Agent"].Count > 0;
                if (flag5)
                {
                    sb.AppendLine(string.Format("{0}{1}", FileLogAdapter.MessageTitle[4], context.Request.Headers["User-Agent"]));
                }
                bool isAuthenticated = context.User.Identity.IsAuthenticated;
                if (isAuthenticated)
                {
                    sb.AppendLine(FileLogAdapter.MessageTitle[5] + context.User.Identity.Name);
                }
                bool flag6;
                if (!context.IsFile())
                {
                    long? contentLength = context.Request.ContentLength;
                    long num = 0L;
                    flag6 = ((contentLength.GetValueOrDefault() > num & contentLength != null) || context.Request.HasFormContentType);
                }
                else
                {
                    flag6 = false;
                }
                bool flag7 = flag6;
                if (flag7)
                {
                    bool flag8 = context.Request.Query.Count > 0;
                    if (flag8)
                    {
                        sb.AppendLine(FileLogAdapter.MessageTitle[6] + string.Join(",", from kv in context.Request.Query
                                                                                        select string.Format("{0}={1}", kv.Key, kv.Value)));
                    }
                    bool hasFormContentType = context.Request.HasFormContentType;
                    if (hasFormContentType)
                    {
                        IFormCollection forms = context.Request.ReadFormAsync(default(CancellationToken)).GetAwaiter().GetResult();
                        bool flag9 = forms.Count > 0;
                        if (flag9)
                        {
                            sb.AppendLine(FileLogAdapter.MessageTitle[7] + string.Join(",", from kv in forms
                                                                                            select string.Format("{0}={1}", kv.Key, kv.Value)));
                        }
                    }
                    else
                    {
                        string body = context.ReadBody();
                        bool flag10 = !string.IsNullOrEmpty(body);
                        if (flag10)
                        {
                            sb.AppendLine(FileLogAdapter.MessageTitle[8] + body.TrimStart(new char[]
                            {
                                '\n',
                                '\r',
                                ' '
                            }));
                        }
                    }
                }
            }
            bool flag11 = exception != null;
            if (flag11)
            {
                sb.AppendLine(string.Format("{0}{1}", FileLogAdapter.MessageTitle[9], (exception != null) ? exception.GetString(ExceptionType.All) : null));
            }
            else
            {
                bool flag12 = excetionDetails != null;
                if (flag12)
                {
                    sb.AppendLine(FileLogAdapter.MessageTitle[9] + excetionDetails);
                }
            }
            bool flag13 = stackFrames != null && stackFrames.Length != 0;
            if (flag13)
            {
                sb.AppendLine(string.Format("{0}{1}", FileLogAdapter.MessageTitle[10], stackFrames.GetString()));
            }
            new LoggerContent(path, sb.ToString());
            sb.Clear();
        }

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
        protected virtual void LogMarkdown(string category, string eventName, LogLevel logLevel, string message, StackFrame[] stackFrames, Exception exception, HttpContext context)
        {
            string path = string.Empty;
            bool flag = !string.IsNullOrEmpty(eventName);
            if (flag)
            {
                int index = eventName.LastIndexOf('.');
                string name = eventName;
                bool flag2 = index > 0;
                if (flag2)
                {
                    name = eventName.Remove(0, eventName.LastIndexOf('.') + 1);
                }
                path = Path.Combine(new string[]
                {
                    this.FileDirectory,
                    ((exception != null) ? LogLevel.Error : logLevel).ToString(),
                    category,
                    name,
                    DateTime.Now.ToString("yyyyMMdd") + ".md"
                });
            }
            else
            {
                path = Path.Combine(this.FileDirectory, ((exception != null) ? LogLevel.Error : logLevel).ToString(), category, DateTime.Now.ToString("yyyyMMdd") + ".md");
            }
            StringBuilder sb = new StringBuilder("|");
            sb.Append(DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fffffff"));
            sb.Append("|");
            string excetionDetails = null;
            bool flag3 = message == "[null]" && exception != null;
            if (flag3)
            {
                sb.Append(exception.GetString(ExceptionType.Message).Replace("\n", "").Replace("\r", "").Replace("|", "&#124;").ToString().Trim());
                excetionDetails = exception.GetString(ExceptionType.Details).ToString().Trim();
                exception = null;
            }
            else
            {
                sb.Append(this.htmlEncoder.Encode(FileLogAdapter.HtmlReg.Replace(message, " ").Replace("__", "\\__").Replace("|", "&#124;")));
            }
            sb.Append("|");
            bool flag4 = context != null;
            if (flag4)
            {
                sb.Append("<ul>");
                bool hasValue = context.Request.Host.HasValue;
                if (hasValue)
                {
                    sb.AppendFormat("<li>{0}{1}://{2}{3}{4}</li>", new object[]
                    {
                        FileLogAdapter.MessageTitle[1].TrimEnd(Array.Empty<char>()),
                        context.Request.Scheme,
                        context.Request.Host,
                        context.Request.Path,
                        context.Request.QueryString
                    });
                }
                sb.AppendFormat("<li>{0}{1}</li>", FileLogAdapter.MessageTitle[2].TrimEnd(Array.Empty<char>()), context.Request.Method);
                sb.AppendFormat("<li>{0}{1}</li>", FileLogAdapter.MessageTitle[3].TrimEnd(Array.Empty<char>()), context.Connection.RemoteIpAddress);
                bool flag5 = context.Request.Headers["User-Agent"].Count > 0;
                if (flag5)
                {
                    sb.AppendFormat("<li>{0}{1}</li>", FileLogAdapter.MessageTitle[4].TrimEnd(Array.Empty<char>()), context.Request.Headers["User-Agent"]);
                }
                bool isAuthenticated = context.User.Identity.IsAuthenticated;
                if (isAuthenticated)
                {
                    sb.AppendFormat("<li>{0}{1}</li>", FileLogAdapter.MessageTitle[5].TrimEnd(Array.Empty<char>()), context.User.Identity.Name);
                }
                bool flag6;
                if (!context.IsFile())
                {
                    long? contentLength = context.Request.ContentLength;
                    long num = 0L;
                    flag6 = ((contentLength.GetValueOrDefault() > num & contentLength != null) || context.Request.HasFormContentType);
                }
                else
                {
                    flag6 = false;
                }
                bool flag7 = flag6;
                if (flag7)
                {
                    bool flag8 = context.Request.Query.Count > 0;
                    if (flag8)
                    {
                        sb.AppendFormat("<li>{0}{1}</li>", FileLogAdapter.MessageTitle[6].TrimEnd(Array.Empty<char>()), string.Join(",", from kv in context.Request.Query
                                                                                                                                         select string.Format("{0}={1}", kv.Key, kv.Value)));
                    }
                    bool hasFormContentType = context.Request.HasFormContentType;
                    if (hasFormContentType)
                    {
                        IFormCollection forms = context.Request.ReadFormAsync(default(CancellationToken)).GetAwaiter().GetResult();
                        bool flag9 = forms.Count > 0;
                        if (flag9)
                        {
                            sb.AppendFormat("<li>{0}{1}</li>", FileLogAdapter.MessageTitle[7].TrimEnd(Array.Empty<char>()), string.Join(",", from kv in forms
                                                                                                                                             select string.Format("{0}={1}", kv.Key, kv.Value)));
                        }
                    }
                    else
                    {
                        string body = context.ReadBody();
                        bool flag10 = !string.IsNullOrEmpty(body);
                        if (flag10)
                        {
                            StringBuilder _sb = new StringBuilder();
                            StringWriter sw = new StringWriter(_sb);
                            this.htmlEncoder.Encode(sw, body, 0, 1048576);
                            sb.AppendFormat("<li>{0}{1}</li>", FileLogAdapter.MessageTitle[8].TrimEnd(Array.Empty<char>()), _sb.ToString());
                        }
                    }
                }
                sb.Append("</ul>");
            }
            else
            {
                sb.Append('-');
            }
            sb.Append("|");
            bool flag11 = exception != null || excetionDetails != null;
            if (flag11)
            {
                sb.Append("<ul>");
                using (StringReader strReader = new StringReader(excetionDetails ?? exception.GetString(ExceptionType.All).ToString()))
                {
                    string line;
                    while ((line = strReader.ReadLine()) != null)
                    {
                        sb.AppendFormat("<li>{0}</li>", this.htmlEncoder.Encode(line.Trim().Replace("__", "\\__").Replace("|", "&#124;")));
                    }
                }
                sb.Append("</ul>");
            }
            else
            {
                sb.Append('-');
            }
            sb.Append("|");
            bool flag12 = stackFrames != null && stackFrames.Length != 0;
            if (flag12)
            {
                sb.Append("<ul>");
                using (StringReader strReader2 = new StringReader(stackFrames.GetString().ToString()))
                {
                    string line2;
                    while ((line2 = strReader2.ReadLine()) != null)
                    {
                        sb.AppendFormat("<li>{0}</li>", this.htmlEncoder.Encode(line2.Trim().Replace("__", "\\__").Replace("|", "&#124;")));
                    }
                }
                sb.Append("</ul>|");
            }
            else
            {
                sb.Append("-|");
            }
            new LoggerContent(path, sb.Replace("`", "\\`").Replace("(", "&#40;").Replace("[", "&#91;").Replace("{", "&#123;").Replace("~~", "\\~~").Replace("**", "\\**").Replace("://", "&#58;//").ToString());
            sb.Clear();
            sb = null;
        }
        private static readonly Regex HtmlReg = new Regex("([\\n\\r\\t]|[\\s]{2})");

        internal const string MarkdownHead = "|时间|消息|请求|错误|跟踪|\r\n|--|--|--|--|--|\r\n";

        private HtmlEncoder htmlEncoder;

        private const string NullMessage = "[null]";

        private static readonly Regex NewLineRegex = new Regex("([\\n\\r]+)");

        private static readonly string[] MessageTitle = new string[]
        {
            "Message:    ",
            "Path:       ",
            "Method:     ",
            "From IP:    ",
            "UserAgent:  ",
            "User:       ",
            "Query:      ",
            "Form:       ",
            "Body:       ",
            "Error:      ",
            "StackTrace: "
        };
    }
}
