using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace AspNetCore.FileLog
{
    internal class HtmlDirectoryFormatter : IDirectoryFormatter
    {
        private CultureInfo CurrentCulture { get; }
        
        public HtmlDirectoryFormatter() : this(HtmlEncoder.Default)
        {
        }
        
        public HtmlDirectoryFormatter(HtmlEncoder encoder)
        {
            if (encoder == null)
            {
                throw new ArgumentNullException("encoder");
            }
            this._htmlEncoder = encoder;
            this.CurrentCulture = CultureInfo.CurrentCulture;
        }

        /// <summary>
        /// Generates an HTML view for a directory.
        /// </summary>
        public virtual Task GenerateContentAsync(HttpContext context, IEnumerable<IFileInfo> contents)
        {
            bool flag = context == null;
            if (flag)
            {
                throw new ArgumentNullException("context");
            }
            bool flag2 = contents == null;
            if (flag2)
            {
                throw new ArgumentNullException("contents");
            }
            context.Response.ContentType = "text/html; charset=utf-8";
            bool flag3 = HttpMethods.IsHead(context.Request.Method);
            Task result;
            if (flag3)
            {
                result = Task.CompletedTask;
            }
            else
            {
                PathString pathString = context.Request.PathBase + context.Request.Path;
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendFormat("<!DOCTYPE html>\r\n<html lang=\"{0}\">", this.CurrentCulture.TwoLetterISOLanguageName);
                stringBuilder.AppendFormat("\r\n<head>\r\n  <title>{0} {1}</title>", this.HtmlEncode("Index of"), this.HtmlEncode(pathString.Value));
                stringBuilder.Append("\r\n  <style>\r\n    body {\r\n        font-family: \"Segoe UI\", \"Segoe WP\", \"Helvetica Neue\", 'RobotoRegular', sans-serif;\r\n        font-size: 14px;}\r\n    header h1 {\r\n        font-family: \"Segoe UI Light\", \"Helvetica Neue\", 'RobotoLight', \"Segoe UI\", \"Segoe WP\", sans-serif;\r\n        font-size: 28px;\r\n        font-weight: 100;\r\n        margin-top: 5px;\r\n        margin-bottom: 0px;}\r\n    #index {\r\n        border-collapse: separate; \r\n        border-spacing: 0; \r\n        margin: 0 0 20px; }\r\n    #index th {\r\n        vertical-align: bottom;\r\n        padding: 10px 5px 5px 5px;\r\n        font-weight: 400;\r\n        color: #a0a0a0;\r\n        text-align: center; }\r\n    #index td { padding: 3px 10px; }\r\n    #index th, #index td {\r\n        border-right: 1px #ddd solid;\r\n        border-bottom: 1px #ddd solid;\r\n        border-left: 1px transparent solid;\r\n        border-top: 1px transparent solid;\r\n        box-sizing: border-box; }\r\n    #index th:last-child, #index td:last-child {\r\n        border-right: 1px transparent solid; }\r\n    #index td.length, td.modified { text-align:right; }\r\n    a { color:#1ba1e2;text-decoration:none; }\r\n    a:hover { color:#13709e;text-decoration:underline; }\r\n  </style>\r\n</head>\r\n<body>\r\n  <section id=\"main\">");
                stringBuilder.AppendFormat("\r\n    <header><h1><a href=\"/\">Home</a>&nbsp;&nbsp;<a href='{0}'>Settings</a>&nbsp;&nbsp;", LoggerSettings.SettingsPath);
                string text = "/";
                string[] array = pathString.Value.Split(new char[]
                {
                    '/'
                }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string text2 in array)
                {
                    text = text + text2 + "/";
                    bool flag4 = text == pathString.Value;
                    if (flag4)
                    {
                        stringBuilder.AppendFormat("<a href='javascript:;'>{0}</a>", this.HtmlEncode(text2));
                    }
                    else
                    {
                        stringBuilder.AppendFormat("<a href=\"{0}\">{1}/</a>", this.HtmlEncode(text), this.HtmlEncode(text2));
                    }
                }
                stringBuilder.AppendFormat(this.CurrentCulture, "</h1></header>\r\n    <table id=\"index\" summary=\"{0}\">\r\n    <thead>\r\n      <tr><th abbr=\"{1}\">{1}</th><th abbr=\"{2}\">{2}</th><th abbr=\"{3}\">{4}</th></tr>\r\n    </thead>\r\n    <tbody>", new object[]
                {
                    this.HtmlEncode("The list of files in the given directory.  Column headers are listed in the first row."),
                    this.HtmlEncode("Name"),
                    this.HtmlEncode("Size"),
                    this.HtmlEncode("Modified"),
                    this.HtmlEncode("Last Modified")
                });
                foreach (IFileInfo item in from info in contents
                                           where info.IsDirectory
                                           select info)
                {
                    StringBuilder stringBuilder2 = stringBuilder;
                    string arg = this.HtmlEncode(item.Name);
                    DateTimeOffset lastModified = item.LastModified;
                    long? len = new long?(new DirectoryInfo(item.PhysicalPath).GetFiles().Where((FileInfo x) => x.Name != "_logging.json").Sum((FileInfo x) => x.Length));
                    long? num = len;
                    long num2 = 0L;
                    bool flag5 = num.GetValueOrDefault() <= num2 & num != null;
                    if (flag5)
                    {
                        len = null;
                    }
                    stringBuilder2.AppendFormat("<tr class=\"directory\"><td class=\"name\"><a href=\"./{0}/\">{0}/</a></td><td>{1}</td><td class=\"modified\">{2}</td></tr>", arg, this.HtmlEncode((len != null) ? len.GetValueOrDefault().ToString("n0", this.CurrentCulture) : null), this.HtmlEncode(lastModified.ToString(this.CurrentCulture)));
                }
                foreach (IFileInfo item2 in from info in contents
                                            where !info.IsDirectory
                                            select info)
                {
                    bool flag6 = item2.Name == "_logging.json";
                    if (!flag6)
                    {
                        StringBuilder stringBuilder3 = stringBuilder;
                        string arg2 = this.HtmlEncode(item2.Name);
                        string arg3 = this.HtmlEncode(item2.Length.ToString("n0", this.CurrentCulture));
                        string arg4 = this.HtmlEncode(item2.PhysicalPath);
                        stringBuilder3.AppendFormat("\r\n      <tr class=\"file\">\r\n        <td class=\"name\"><a href=\"./{0}\" title=\"{2}\">{0}</a></td>\r\n        <td class=\"length\">{1}</td>\r\n        <td class=\"modified\">{3}</td>\r\n      </tr>", new object[]
                        {
                            arg2,
                            arg3,
                            arg4,
                            this.HtmlEncode(item2.LastModified.ToString(this.CurrentCulture))
                        });
                    }
                }
                stringBuilder.Append("\r\n    </tbody>\r\n    </table>\r\n  </section>\r\n</body>\r\n</html>");
                string s = stringBuilder.ToString();
                byte[] bytes = Encoding.UTF8.GetBytes(s);
                context.Response.ContentLength = new long?((long)bytes.Length);
                result = context.Response.Body.WriteAsync(bytes, 0, bytes.Length);
            }
            return result;
        }
        
        private string HtmlEncode(string body)
        {
            bool flag = string.IsNullOrEmpty(body);
            string result;
            if (flag)
            {
                result = string.Empty;
            }
            else
            {
                result = this._htmlEncoder.Encode(body);
            }
            return result;
        }
        
        private const string TextHtmlUtf8 = "text/html; charset=utf-8";
        
        private HtmlEncoder _htmlEncoder;
    }
}
