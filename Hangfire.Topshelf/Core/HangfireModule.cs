using System.Linq;
using System.Reflection;
using Autofac;
using Autofac.Core;
using Autofac.Core.Registration;
using Hangfire.Topshelf.Jobs;
using Hangfire.Samples.Framework;
using Hangfire.Samples.Framework.Logging;
using Autofac.Core.Resolving.Pipeline;

namespace Hangfire.Topshelf.Core
{
	/// <summary>
	/// Hangfire Module
	/// </summary>
	public class HangfireModule : Autofac.Module
	{
		protected override void AttachToComponentRegistration(IComponentRegistryBuilder componentRegistry, IComponentRegistration registration)
		{
			base.AttachToComponentRegistration(componentRegistry, registration);

			// Handle constructor parameters.
			//registration.Preparing += OnComponentPreparing;
            registration.PipelineBuilding += Registration_PipelineBuilding;

			// Handle properties.
			//registration.Activated += (sender, e) => InjectLoggerProperties(e.Instance);
		}

        private void Registration_PipelineBuilding(object sender, Autofac.Core.Resolving.Pipeline.IResolvePipelineBuilder pipeline)
        {
            pipeline.Use(PipelinePhase.Activation, MiddlewareInsertionMode.EndOfPhase, (c, next) =>
            {
                next(c);

                // Do something with the component instance
                //var instance = c.Instance;
                InjectLoggerProperties(c.Instance);

            });

            pipeline.Use(PipelinePhase.ParameterSelection, MiddlewareInsertionMode.StartOfPhase, (c, next) =>
            {
                next(c);

                // Do something with the component instance
                //var instance = c.Instance;
                InjectLoggerProperties(c.Instance);
				c.Parameters.Union(new[]
                {
                    new ResolvedParameter(
                        (p, i) => p.ParameterType == typeof(ILog),
                        (p, i) => LogProvider.GetLogger(p.Member.DeclaringType)
                    ),
                });

            });
        }

        private void InjectLoggerProperties(object instance)
		{
			var instanceType = instance.GetType();

			// Get all the injectable properties to set.
			// If you wanted to ensure the properties were only UNSET properties,
			// here's where you'd do it.
			var properties = instanceType
			  .GetProperties(BindingFlags.Public | BindingFlags.Instance)
			  .Where(p => p.PropertyType == typeof(ILog) && p.CanWrite && p.GetIndexParameters().Length == 0);

			// Set the properties located.
			foreach (var propToSet in properties)
			{
				propToSet.SetValue(instance, LogProvider.GetLogger(instanceType), null);
			}
		}

		private void OnComponentPreparing(object sender, PreparingEventArgs e)
		{
			e.Parameters = e.Parameters.Union(new[]
				 {
					new ResolvedParameter(
						(p, i) => p.ParameterType == typeof(ILog),
						(p, i) => LogProvider.GetLogger(p.Member.DeclaringType)
					),
				 });
		}

		/// <summary>
		/// Auto register
		/// </summary>
		/// <param name="builder"></param>
		protected override void Load(ContainerBuilder builder)
		{
			//register all implemented interfaces
			builder.RegisterAssemblyTypes(ThisAssembly)
				.Where(t => typeof(IDependency).IsAssignableFrom(t) && t != typeof(IDependency) && !t.IsInterface)
				.AsImplementedInterfaces();

			//register speicified types here  
			builder.Register(x => new RecurringJobService());
			builder.Register(x => new MyJob1());
			builder.Register(x => new MyJob2());
			builder.Register(x => new LongRunningJob());
		}
	}
}
