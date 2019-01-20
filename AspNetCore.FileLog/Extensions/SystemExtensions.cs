using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;

namespace AspNetCore.FileLog
{
    internal static class SystemExtensions
    {
        /// <summary>
        /// Use Reflect get value of current <paramref name="value" /> by <paramref name="name" />
        /// </summary>
        /// <param name="value">current value</param>
        /// <param name="name">field or property
        /// <para>e.g.: 'a.b.c'</para>
        /// </param>
        /// <returns></returns>
        public static object Value(this object value, string name)
        {
            bool flag = value == null || value == DBNull.Value || string.IsNullOrEmpty(name);
            object result;
            if (flag)
            {
                result = null;
            }
            else
            {
                string[] names = name.Split(new char[]
                {
                    '.'
                }, StringSplitOptions.RemoveEmptyEntries);
                object _value = value;
                foreach (string i in names)
                {
                    Func<object, string, bool, object, object> @delegate = FastExpressions.CreateDelegate(_value);
                    bool flag2 = @delegate != null;
                    if (flag2)
                    {
                        _value = @delegate(_value, i, false, null);
                    }
                    bool flag3 = _value == null;
                    if (flag3)
                    {
                        break;
                    }
                }
                result = _value;
            }
            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static bool IsFile(this HttpContext context)
        {
            bool flag = context != null && context.Request.Path.HasValue;
            if (flag)
            {
                bool flag2 = SystemExtensions.FileRegex.IsMatch(context.Request.Path.ToString());
                if (flag2)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="stackTrace"></param>
        /// <param name="maxCount"></param>
        /// <param name="needSkip"></param>
        /// <returns></returns>
        public static StackFrame[] GetFrames(this StackTrace stackTrace, int maxCount, Func<StackFrame, MethodBase, bool> needSkip = null)
        {
            bool flag = maxCount > 0;
            List<StackFrame> frames;
            if (flag)
            {
                frames = new List<StackFrame>(maxCount);
            }
            else
            {
                frames = new List<StackFrame>(stackTrace.FrameCount);
            }
            int count = 0;
            int iFrameIndex = 0;
            while (iFrameIndex < stackTrace.FrameCount)
            {
                StackFrame sf = stackTrace.GetFrame(iFrameIndex);
                MethodBase mb = sf.GetMethod();
                bool flag2 = mb != null;
                if (flag2)
                {
                    bool flag3 = needSkip != null && needSkip(sf, mb);
                    if (!flag3)
                    {
                        bool flag4 = maxCount > 0 && count >= maxCount;
                        if (flag4)
                        {
                            break;
                        }
                        count++;
                        frames.Add(sf);
                    }
                }
                iFrameIndex++;
                continue;
            }
            return frames.ToArray();
        }
        public static StringBuilder GetString(this StackFrame[] stackFrames)
        {
            bool flag = stackFrames == null || stackFrames.Length == 0;
            StringBuilder result;
            if (flag)
            {
                result = null;
            }
            else
            {
                StringBuilder sb = new StringBuilder(255);
                bool displayFilenames = true;
                bool fFirstFrame = true;
                foreach (StackFrame sf in stackFrames)
                {
                    MethodBase mb = sf.GetMethod();
                    bool flag2 = mb != null;
                    if (flag2)
                    {
                        bool flag3 = fFirstFrame;
                        if (flag3)
                        {
                            fFirstFrame = false;
                            sb.AppendFormat(CultureInfo.InvariantCulture, "{0} ", "at");
                        }
                        else
                        {
                            sb.Append(Environment.NewLine);
                            sb.AppendFormat(CultureInfo.InvariantCulture, "   {0} ", "at");
                        }
                        Type t = mb.DeclaringType;
                        bool flag4 = t != null;
                        if (flag4)
                        {
                            sb.Append(t.FullName.Replace('+', '.'));
                            sb.Append(".");
                        }
                        sb.Append(mb.Name);
                        bool flag5 = mb is MethodInfo && ((MethodInfo)mb).IsGenericMethod;
                        if (flag5)
                        {
                            Type[] typars = ((MethodInfo)mb).GetGenericArguments();
                            sb.Append("[");
                            int i = 0;
                            bool fFirstTyParam = true;
                            while (i < typars.Length)
                            {
                                bool flag6 = !fFirstTyParam;
                                if (flag6)
                                {
                                    sb.Append(",");
                                }
                                else
                                {
                                    fFirstTyParam = false;
                                }
                                sb.Append(typars[i].Name);
                                i++;
                            }
                            sb.Append("]");
                        }
                        sb.Append("(");
                        ParameterInfo[] pi = mb.GetParameters();
                        bool fFirstParam = true;
                        for (int j = 0; j < pi.Length; j++)
                        {
                            bool flag7 = !fFirstParam;
                            if (flag7)
                            {
                                sb.Append(", ");
                            }
                            else
                            {
                                fFirstParam = false;
                            }
                            string typeName = "<UnknownType>";
                            bool flag8 = pi[j].ParameterType != null;
                            if (flag8)
                            {
                                typeName = pi[j].ParameterType.Name;
                            }
                            sb.Append(typeName + " " + pi[j].Name);
                        }
                        sb.Append(")");
                        bool flag9 = displayFilenames && sf.GetILOffset() != -1;
                        if (flag9)
                        {
                            string fileName = null;
                            try
                            {
                                fileName = sf.GetFileName();
                            }
                            catch (SecurityException)
                            {
                                displayFilenames = false;
                            }
                            bool flag10 = fileName != null;
                            if (flag10)
                            {
                                sb.Append(' ');
                                sb.AppendFormat(CultureInfo.InvariantCulture, "in {0}:line {1}", fileName, sf.GetFileLineNumber());
                            }
                        }
                    }
                }
                sb.Append(Environment.NewLine);
                result = sb;
            }
            return result;
        }
        public static StringBuilder GetString(this StackTrace stackTrace, Func<StackFrame, MethodBase, bool> needSkip = null)
        {
            StringBuilder sb = new StringBuilder(255);
            bool flag = stackTrace == null || stackTrace.FrameCount <= 0;
            StringBuilder result;
            if (flag)
            {
                result = null;
            }
            else
            {
                bool displayFilenames = true;
                bool fFirstFrame = true;
                int iFrameIndex = 0;
                while (iFrameIndex < stackTrace.FrameCount)
                {
                    StackFrame sf = stackTrace.GetFrame(iFrameIndex);
                    MethodBase mb = sf.GetMethod();
                    bool flag2 = mb != null;
                    if (flag2)
                    {
                        bool flag3 = needSkip != null && needSkip(sf, mb);
                        if (!flag3)
                        {
                            bool flag4 = fFirstFrame;
                            if (flag4)
                            {
                                fFirstFrame = false;
                                sb.AppendFormat(CultureInfo.InvariantCulture, "{0} ", "at");
                            }
                            else
                            {
                                sb.Append(Environment.NewLine);
                                sb.AppendFormat(CultureInfo.InvariantCulture, "   {0} ", "at");
                            }
                            Type t = mb.DeclaringType;
                            bool flag5 = t != null;
                            if (flag5)
                            {
                                sb.Append(t.FullName.Replace('+', '.'));
                                sb.Append(".");
                            }
                            sb.Append(mb.Name);
                            bool flag6 = mb is MethodInfo && ((MethodInfo)mb).IsGenericMethod;
                            if (flag6)
                            {
                                Type[] typars = ((MethodInfo)mb).GetGenericArguments();
                                sb.Append("[");
                                int i = 0;
                                bool fFirstTyParam = true;
                                while (i < typars.Length)
                                {
                                    bool flag7 = !fFirstTyParam;
                                    if (flag7)
                                    {
                                        sb.Append(",");
                                    }
                                    else
                                    {
                                        fFirstTyParam = false;
                                    }
                                    sb.Append(typars[i].Name);
                                    i++;
                                }
                                sb.Append("]");
                            }
                            sb.Append("(");
                            ParameterInfo[] pi = mb.GetParameters();
                            bool fFirstParam = true;
                            for (int j = 0; j < pi.Length; j++)
                            {
                                bool flag8 = !fFirstParam;
                                if (flag8)
                                {
                                    sb.Append(", ");
                                }
                                else
                                {
                                    fFirstParam = false;
                                }
                                string typeName = "<UnknownType>";
                                bool flag9 = pi[j].ParameterType != null;
                                if (flag9)
                                {
                                    typeName = pi[j].ParameterType.Name;
                                }
                                sb.Append(typeName + " " + pi[j].Name);
                            }
                            sb.Append(")");
                            bool flag10 = displayFilenames && sf.GetILOffset() != -1;
                            if (flag10)
                            {
                                string fileName = null;
                                try
                                {
                                    fileName = sf.GetFileName();
                                }
                                catch (SecurityException)
                                {
                                    displayFilenames = false;
                                }
                                bool flag11 = fileName != null;
                                if (flag11)
                                {
                                    sb.Append(' ');
                                    sb.AppendFormat(CultureInfo.InvariantCulture, "in {0}:line {1}", fileName, sf.GetFileLineNumber());
                                }
                            }
                        }
                    }
                    iFrameIndex++;
                    continue;
                }
                sb.Append(Environment.NewLine);
                result = sb;
            }
            return result;
        }
        private static JsonSerializer CreateDefault()
        {
            JsonSerializer serializer = JsonSerializer.CreateDefault();
            bool flag = serializer.ContractResolver == null;
            if (flag)
            {
                serializer.ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy(true, true)
                };
            }
            serializer.DateFormatString = "yyyy/MM/dd HH:mm:ss";
            serializer.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            return serializer;
        }
        
        public static string ToJson(this object value)
        {
            StringWriter stringWriter = new StringWriter(new StringBuilder(256));
            using (JsonTextWriter jsonTextWriter = new JsonTextWriter(stringWriter))
            {
                SystemExtensions.CreateDefault().Serialize(jsonTextWriter, value);
            }
            return stringWriter.ToString();
        }

        private static Regex FileRegex = new Regex("\\.[^\\.]+$", RegexOptions.IgnoreCase);
        
        private const string word_At = "at";
        
        private const string inFileLineNum = "in {0}:line {1}";
    }
}
