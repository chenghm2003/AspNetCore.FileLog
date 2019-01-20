using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.Net.Http.Headers;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetCore.FileLog.Middleware
{
    internal class MarkdownFileMiddleware
    {
        internal static void SaveResourceFiles(string path)
        {
            bool flag = !Directory.Exists(Path.Combine(path, "strapdown"));
            if (flag)
            {
                Directory.CreateDirectory(Path.Combine(path, "strapdown"));
            }
            string file = Path.Combine(path, "strapdown", "cerulean.min.css");
            bool flag2 = !File.Exists(file);
            if (flag2)
            {
                using (StreamReader sr = new StreamReader(MarkdownFileMiddleware._assembly.GetManifestResourceStream("AspNetCore.FileLog.Source.markdown.cerulean.min.css")))
                {
                    File.WriteAllText(file, sr.ReadToEnd());
                }
            }
            file = Path.Combine(path, "strapdown", "font.woff2");
            bool flag3 = !File.Exists(file);
            if (flag3)
            {
                using (StreamReader sr2 = new StreamReader(MarkdownFileMiddleware._assembly.GetManifestResourceStream("AspNetCore.FileLog.Source.markdown.font.woff2")))
                {
                    File.WriteAllText(file, sr2.ReadToEnd());
                }
            }
            file = Path.Combine(path, "strapdown", "strapdown.css");
            bool flag4 = !File.Exists(file);
            if (flag4)
            {
                using (StreamReader sr3 = new StreamReader(MarkdownFileMiddleware._assembly.GetManifestResourceStream("AspNetCore.FileLog.Source.markdown.strapdown.min.css")))
                {
                    File.WriteAllText(file, sr3.ReadToEnd());
                }
            }
            file = Path.Combine(path, "strapdown", "strapdown.js");
            bool flag5 = !File.Exists(file);
            if (flag5)
            {
                using (StreamReader sr4 = new StreamReader(MarkdownFileMiddleware._assembly.GetManifestResourceStream("AspNetCore.FileLog.Source.markdown.strapdown.min.js")))
                {
                    File.WriteAllText(file, sr4.ReadToEnd());
                }
            }
            bool flag6 = string.IsNullOrEmpty(MarkdownFileMiddleware.markdownHtmlContent);
            if (flag6)
            {
                using (StreamReader sr5 = new StreamReader(MarkdownFileMiddleware._assembly.GetManifestResourceStream("AspNetCore.FileLog.Source.markdown.markdown.html")))
                {
                    MarkdownFileMiddleware.markdownHtmlContent = sr5.ReadToEnd();
                }
            }
        }
        
        public MarkdownFileMiddleware(RequestDelegate next, string fileDirectory)
        {
            this._next = next;
            this._fileDirectory = fileDirectory;
        }
        
        public async Task Invoke(HttpContext context)
        {
            context.Response.ContentType = "text/html; charset=utf-8";
            string _path = context.Request.Path.ToString().Replace(LoggerSettings.LogRequestPath, "");
            FileInfo file = new FileInfo(Path.Combine(this._fileDirectory, _path.TrimStart(new char[]
            {
                '\\',
                '/'
            })));
            context.Response.Headers.Add("Accept-Ranges", "bytes");
            EntityTagHeaderValue _tag = null;
            bool exists = file.Exists;
            if (exists)
            {
                long value = file.LastAccessTime.ToUniversalTime().ToFileTime() ^ file.Length;
                _tag = EntityTagHeaderValue.Parse("\"" + Convert.ToString(value, 16) + "\"");
                bool flag = context.Request.Headers.Keys.Contains("If-None-Match");
                if (flag)
                {
                    string tag = context.Request.Headers["If-None-Match"].ToString();
                    bool flag2 = tag == _tag.Tag;
                    if (flag2)
                    {
                        context.Response.StatusCode = 304;
                        await Task.CompletedTask;
                        return;
                    }
                    tag = null;
                }
                if (_tag != null)
                {
                    ResponseHeaders _responseHeaders = context.Response.GetTypedHeaders();
                    _responseHeaders.LastModified = new DateTimeOffset?(file.LastAccessTime);
                    _responseHeaders.ETag = _tag;
                    _responseHeaders.CacheControl = new CacheControlHeaderValue
                    {
                        MaxAge = new TimeSpan?(TimeSpan.FromHours(2.0))
                    };
                    _responseHeaders = null;
                }
                string fileContent;
                using (FileStream _stream = file.OpenRead())
                {
                    using (StreamReader _reader = new StreamReader(_stream))
                    {
                        fileContent = _reader.ReadToEnd();
                    }
                }
                StringBuilder html = new StringBuilder(MarkdownFileMiddleware.markdownHtmlContent);
                PathString pathString = context.Request.Path;
                string text = "/";
                string[] array = pathString.Value.Split(new char[]
                {
                    '/'
                }, StringSplitOptions.RemoveEmptyEntries);
                StringBuilder stringBuilder = new StringBuilder("<a href='" + LoggerSettings.SettingsPath + "'>Settings</a>&nbsp;&nbsp;");
                foreach (string text2 in array)
                {
                    text = string.Format("{0}{1}/", text, text2);
                    if (text.TrimEnd(new char[]
                    {
                        '/'
                    }) == context.Request.Path)
                    {
                        stringBuilder.AppendFormat("<a href='javascript:;'>{0}</a>", MarkdownFileMiddleware._htmlEncoder.Encode(text2));
                    }
                    else
                    {
                        stringBuilder.AppendFormat("<a href=\"{0}\">{1}/</a>", MarkdownFileMiddleware._htmlEncoder.Encode(text), MarkdownFileMiddleware._htmlEncoder.Encode(text2));
                    }
                }
                html.Replace("{{content}}", "|时间|消息|请求|错误|跟踪|\r\n|--|--|--|--|--|\r\n" + fileContent).Replace("{{title}}", string.Join("/", array.Take(array.Length - 1))).Replace("{{link}}", stringBuilder.ToString());
                context.Response.StatusCode = 200;
                await context.Response.WriteAsync(html.ToString(), Encoding.UTF8, default(CancellationToken));
                fileContent = null;
                html = null;
                pathString = default(PathString);
                text = null;
                array = null;
                stringBuilder = null;
            }
        }
        
        private readonly RequestDelegate _next;
        
        private static readonly Assembly _assembly = typeof(MarkdownFileMiddleware).Assembly;
        
        private const string cerulean = "AspNetCore.FileLog.Source.markdown.cerulean.min.css";
        
        private const string font = "AspNetCore.FileLog.Source.markdown.font.woff2";
        
        private const string strapdownCss = "AspNetCore.FileLog.Source.markdown.strapdown.min.css";
        
        private const string strapdownJs = "AspNetCore.FileLog.Source.markdown.strapdown.min.js";
        
        private const string markdownHtml = "AspNetCore.FileLog.Source.markdown.markdown.html";
        
        private const string strapdown = "strapdown";
        
        private static string markdownHtmlContent;
        
        private readonly string _fileDirectory;

        private static readonly HtmlEncoder _htmlEncoder = HtmlEncoder.Default;
    }
}
