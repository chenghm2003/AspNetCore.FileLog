using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Builder.Internal;
using Microsoft.AspNetCore.Hosting.Builder;
using Microsoft.AspNetCore.Http.Features;
using System;
using System.Collections.Concurrent;

namespace AspNetCore.FileLog.Factory
{
    internal class DefaultApplicationBuilderFactory : IApplicationBuilderFactory
    {
        public DefaultApplicationBuilderFactory(IServiceProvider serviceProvider)
        {
            this._serviceProvider = serviceProvider;
        }
        
        public IApplicationBuilder CreateBuilder(IFeatureCollection serverFeatures)
        {
            ApplicationBuilder builder = new ApplicationBuilder(this._serviceProvider, serverFeatures);
            while (DefaultApplicationBuilderFactory._builderActions != null && !DefaultApplicationBuilderFactory._builderActions.IsEmpty)
            {
                DefaultApplicationBuilderFactory.BuilderAction action;
                bool flag = DefaultApplicationBuilderFactory._builderActions.TryTake(out action);
                if (!flag)
                {
                    throw new Exception("TryTake");
                }
                Action<IApplicationBuilder> action2 = action.Action;
                if (action2 != null)
                {
                    action2(builder);
                }
                Action<IApplicationBuilder, object> actionWithState = action.ActionWithState;
                if (actionWithState != null)
                {
                    actionWithState(builder, action.State);
                }
            }
            DefaultApplicationBuilderFactory._builderActions = null;
            return builder;
        }
        
        public static void OnCreateBuilder(Action<IApplicationBuilder> builderAction)
        {
            bool flag = builderAction != null;
            if (flag)
            {
                DefaultApplicationBuilderFactory._builderActions.Add(new DefaultApplicationBuilderFactory.BuilderAction
                {
                    Action = builderAction
                });
            }
        }
        
        public static void OnCreateBuilder(Action<IApplicationBuilder, object> builderAction, object state)
        {
            bool flag = state == null;
            if (flag)
            {
                throw new ArgumentNullException("state", "Please use OnCreateBuilder(Action<IApplicationBuilder> builderAction).");
            }
            bool flag2 = builderAction != null;
            if (flag2)
            {
                DefaultApplicationBuilderFactory._builderActions.Add(new DefaultApplicationBuilderFactory.BuilderAction
                {
                    ActionWithState = builderAction,
                    State = state
                });
            }
        }
        
        private static ConcurrentBag<DefaultApplicationBuilderFactory.BuilderAction> _builderActions = new ConcurrentBag<DefaultApplicationBuilderFactory.BuilderAction>();
        
        private readonly IServiceProvider _serviceProvider;
        
        private class BuilderAction
        {
            public object State { get; set; }
            
            public Action<IApplicationBuilder, object> ActionWithState { get; set; }
            
            public Action<IApplicationBuilder> Action { get; set; }
        }
    }
}
