using System;
using System.Globalization;
using System.Reflection;
using System.Threading;
using OSPSuite.Utility.Container;
using PKSim.CLI.Core.MinimalImplementations;
using PKSim.Core;
using PKSim.Infrastructure;

namespace PKSim.Matlab
{
   internal class ApplicationStartup
   {
      private static bool _initialized;

      public static void Initialize()
      {
         if (_initialized) return;

         redirectNHibernateAssembly();

         new ApplicationStartup().initializeForMatlab();
         _initialized = true;
      }

      private void initializeForMatlab()
      {
         if (IoC.Container != null)
            return;

         var container = InfrastructureRegister.Initialize();

         using (container.OptimizeDependencyResolution())
         {
            container.RegisterImplementationOf(new SynchronizationContext());
            container.AddRegister(x => x.FromType<MatlabRegister>());
            container.AddRegister(x => x.FromType<CoreRegister>());
            container.AddRegister(x => x.FromType<InfrastructureRegister>());

            //no computation required in matlab interface
            InfrastructureRegister.RegisterSerializationDependencies();
            container.Register<ICoreWorkspace, OSPSuite.Core.IWorkspace, CLIWorkspace>(LifeStyle.Singleton);
         }
      }

      private static void redirectNHibernateAssembly()
      {
         redirectAssembly("NHibernate", new Version(5, 2, 0, 0), "aa95f207798dfdb4");
      }

      private static void redirectAssembly(string shortName, Version targetVersion, string publicKeyToken)
      {
         Assembly Handler(object sender, ResolveEventArgs args)
         {
            var requestedAssembly = new AssemblyName(args.Name);
            if (requestedAssembly.Name != shortName) return null;

            requestedAssembly.Version = targetVersion;
            requestedAssembly.SetPublicKeyToken(new AssemblyName("x, PublicKeyToken=" + publicKeyToken).GetPublicKeyToken());
            requestedAssembly.CultureInfo = CultureInfo.InvariantCulture;

            //once found, not need to react to event anymore
            AppDomain.CurrentDomain.AssemblyResolve -= Handler;

            return Assembly.Load(requestedAssembly);
         }

         AppDomain.CurrentDomain.AssemblyResolve += Handler;
      }
   }
}