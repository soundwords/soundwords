using Autofac;
using Autofac.Core;
using Serilog;
using Serilog.Events;

namespace SoundWords
{
    public class SerilogAutofacTraceModule : Module
    {
        public ILogger Logger => _logger ?? (_logger = Log.ForContext<SerilogAutofacTraceModule>());
        public int Depth;
        private ILogger _logger;

        protected override void AttachToComponentRegistration(IComponentRegistry componentRegistry,
                                                              IComponentRegistration registration)
        {
            registration.Preparing += RegistrationOnPreparing;
            registration.Activating += RegistrationOnActivating;
            base.AttachToComponentRegistration(componentRegistry, registration);
        }

        private string GetPrefix()
        {
            return new string('-', Depth * 2);
        }

        private void RegistrationOnPreparing(object sender, PreparingEventArgs preparingEventArgs)
        {
            if (Logger.IsEnabled(LogEventLevel.Verbose))
            {
                Logger.Verbose($"{GetPrefix()}Resolving {{Type}}", preparingEventArgs.Component.Activator.LimitType);
            }
            Depth++;
        }

        private void RegistrationOnActivating(object sender, ActivatingEventArgs<object> activatingEventArgs)
        {
            Depth--;
            if (Logger.IsEnabled(LogEventLevel.Verbose))
            {
                Logger.Verbose($"{GetPrefix()}Resolving {{Type}}", activatingEventArgs.Component.Activator.LimitType);
            }
        }
    }
}