using AspNetCore.FileLog;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection
{
    internal class DefaultDiagnosticListener : DiagnosticListener
    {
        public DefaultDiagnosticListener() : base("Microsoft.AspNetCore.Mvc")
        {
        }
        
        public DefaultDiagnosticListener(string name) : base(name)
        {
        }
        
        public override bool IsEnabled(string name)
        {
            bool result;
            if (!(name == "Microsoft.AspNetCore.Mvc.AfterActionResult"))
            {
                bool enabled = base.IsEnabled(name);
                result = enabled;
            }
            else
            {
                result = true;
            }
            return result;
        }
        
        public override void Write(string name, object value)
        {
            base.Write(name, value);
            if (name == "Microsoft.AspNetCore.Mvc.AfterActionResult")
            {
                ActionContext actionContext = (ActionContext)((value != null) ? value.Value("actionContext") : null);
                PathString? path = (actionContext != null) ? new PathString?(actionContext.HttpContext.Request.Path) : null;
                object result = (value != null) ? value.Value("result") : null;
                bool flag = actionContext != null && result != null;
                if (flag)
                {
                    ContentResult contentResult;
                    bool flag2 = (contentResult = (result as ContentResult)) != null;
                    object obj;
                    if (flag2)
                    {
                        obj = new
                        {
                            ActionType = result.GetType().Name,
                            Path = path,
                            Content = contentResult.Content,
                            ContentType = contentResult.ContentType,
                            StatusCode = contentResult.StatusCode
                        };
                    }
                    else
                    {
                        ObjectResult objectResult;
                        bool flag3 = (objectResult = (result as ObjectResult)) != null;
                        if (flag3)
                        {
                            obj = new
                            {
                                ActionType = result.GetType().Name,
                                Path = path,
                                Value = objectResult.Value
                            };
                        }
                        else
                        {
                            PageResult pageResult;
                            bool flag4 = (pageResult = (result as PageResult)) != null;
                            if (flag4)
                            {
                                obj = new
                                {
                                    ActionType = result.GetType().Name,
                                    Path = path,
                                    PagePath = pageResult.Page.Path,
                                    Layout = pageResult.Page.Layout
                                };
                            }
                            else
                            {
                                ViewResult viewResult;
                                bool flag5 = (viewResult = (result as ViewResult)) != null;
                                if (flag5)
                                {
                                    obj = new
                                    {
                                        ActionType = result.GetType().Name,
                                        Path = path,
                                        ViewName = viewResult.ViewName,
                                        Model = viewResult.Model
                                    };
                                }
                                else
                                {
                                    JsonResult jsonResult;
                                    bool flag6 = (jsonResult = (result as JsonResult)) != null;
                                    if (flag6)
                                    {
                                        obj = new
                                        {
                                            ActionType = result.GetType().Name,
                                            Path = path,
                                            Value = jsonResult.Value
                                        };
                                    }
                                    else
                                    {
                                        obj = new
                                        {
                                            ActionType = result.GetType().Name,
                                            Path = path
                                        };
                                    }
                                }
                            }
                        }
                    }
                    bool flag7 = obj != null;
                    if (flag7)
                    {
                        Logger.Debug<ActionResult>(obj.ToJson(), null, default(EventId));
                    }
                }
            }
        }
        
        private const string AfterActionResult = "Microsoft.AspNetCore.Mvc.AfterActionResult";
    }
}
