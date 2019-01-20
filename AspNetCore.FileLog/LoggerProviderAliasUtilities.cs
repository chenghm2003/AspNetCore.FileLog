using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace AspNetCore.FileLog
{
    /// <summary>
    /// 获取签名属性
    /// </summary>
    internal class LoggerProviderAliasUtilities
    {
        internal static string GetAlias(Type providerType)
        {
            foreach (object attribute in providerType.GetTypeInfo().GetCustomAttributes(false))
            {
                bool flag = attribute.GetType().FullName == "Microsoft.Extensions.Logging.ProviderAliasAttribute";
                if (flag)
                {
                    PropertyInfo valueProperty = attribute.GetType().GetProperty("Alias", BindingFlags.Instance | BindingFlags.Public);
                    bool flag2 = valueProperty != null;
                    if (flag2)
                    {
                        return valueProperty.GetValue(attribute) as string;
                    }
                }
            }
            return null;
        }
        private const string AliasAttibuteTypeFullName = "Microsoft.Extensions.Logging.ProviderAliasAttribute";
        
        private const string AliasAttibuteAliasProperty = "Alias";
    }
}
