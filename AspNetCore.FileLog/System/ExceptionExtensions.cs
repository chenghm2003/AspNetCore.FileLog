using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace System
{
    /// <summary>
    ///
    /// </summary>
    public static class ExceptionExtensions
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static StringBuilder GetString(this Exception exception, ExceptionType type = ExceptionType.All)
        {
            StringBuilder _message = new StringBuilder();
            while (exception != null)
            {
                switch (type)
                {
                    case ExceptionType.All:
                        _message.Insert(0, string.Concat(new string[]
                        {
                        exception.GetType().Name,
                        "=> ",
                        exception.Message,
                        Environment.NewLine,
                        exception.StackTrace,
                        Environment.NewLine
                        }));
                        break;
                    case ExceptionType.Message:
                        _message.Insert(0, exception.GetType().Name + "=> " + exception.Message + Environment.NewLine);
                        break;
                    case ExceptionType.Details:
                        _message.Insert(0, exception.StackTrace + Environment.NewLine);
                        break;
                }
                exception = exception.InnerException;
            }
            return _message;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static Exception Log(this Exception exception, string message = null)
        {
            bool flag = exception == null;
            Exception result;
            if (flag)
            {
                result = null;
            }
            else
            {
                Logger.Error(exception.GetType().FullName, message, exception, default(EventId));
                result = exception;
            }
            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="exception"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static Exception Log<T>(this Exception exception, string message = null) where T : class
        {
            bool flag = exception == null;
            Exception result;
            if (flag)
            {
                result = null;
            }
            else
            {
                Logger.Error<T>(message, exception, default(EventId));
                result = exception;
            }
            return result;
        }
    }
}
