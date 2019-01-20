using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.IO;
using System.Threading;

namespace AspNetCore.FileLog.System
{
    /// <summary>
    ///
    /// </summary>
    public static class HttpContextExtensions
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static string ReadBody(this HttpContext context)
        {
            bool flag = context.Items["_BODY"] != null;
            string result;
            if (flag)
            {
                result = (context.Items["_BODY"] as string);
            }
            else
            {
                object state = HttpContextExtensions._state;
                lock (state)
                {
                    Stream body = context.Request.Body;
                    FileBufferingReadStream stream;
                    bool flag3 = (stream = (body as FileBufferingReadStream)) == null;
                    if (flag3)
                    {
                        bool hasFormContentType = context.Request.HasFormContentType;
                        if (hasFormContentType)
                        {
                            IFormCollection form = context.Request.ReadFormAsync(default(CancellationToken)).GetAwaiter().GetResult();
                        }
                        stream = (FileBufferingReadStream)(context.Request.Body = new FileBufferingReadStream(body, 30720, null, HttpContextExtensions._getTempDirectory));
                        context.Response.RegisterForDispose(stream);
                    }
                    bool isDisposed = stream.IsDisposed;
                    if (isDisposed)
                    {
                        result = string.Empty;
                    }
                    else
                    {
                        bool flag4 = stream.Position > 0L;
                        if (flag4)
                        {
                            stream.Seek(0L, SeekOrigin.Begin);
                        }
                        StreamReader sr = new StreamReader(stream);
                        string content = sr.ReadToEnd();
                        bool flag5 = content.Length < 2097152;
                        if (flag5)
                        {
                            context.Items["_BODY"] = content;
                        }
                        stream.Seek(0L, SeekOrigin.Begin);
                        result = content;
                    }
                }
            }
            return result;
        }

        /// <summary>
        ///
        /// </summary>
        public static string TempDirectory
        {
            get
            {
                bool flag = HttpContextExtensions._tempDirectory == null;
                if (flag)
                {
                    string text = Environment.GetEnvironmentVariable("ASPNETCORE_TEMP") ?? Path.GetTempPath();
                    bool flag2 = !Directory.Exists(text);
                    if (flag2)
                    {
                        throw new DirectoryNotFoundException(text);
                    }
                    HttpContextExtensions._tempDirectory = text;
                }
                return HttpContextExtensions._tempDirectory;
            }
        }
        
        private static readonly object _state = new object();
        
        internal const string Body = "_BODY";
        
        private static readonly Func<string> _getTempDirectory = () => HttpContextExtensions.TempDirectory;
        
        private static string _tempDirectory;
    }
}
