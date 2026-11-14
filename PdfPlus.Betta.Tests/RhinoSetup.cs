using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace PdfPlus.Betta.Tests
{
    /// <summary>
    /// RhinoCommon and Grasshopper come from the Rhino 8 install (the NuGet packages are compile-time only), and
    /// Rhino's native core has to be on PATH. This runs before any test code touches a Rhino type.
    /// Set RHINO8_SYSTEM_DIR if Rhino 8 isn't in the default folder.
    /// </summary>
    internal static class RhinoInstall
    {
        public static readonly string SystemDir =
            Environment.GetEnvironmentVariable("RHINO8_SYSTEM_DIR") ?? @"C:\Program Files\Rhino 8\System";

        private static string GrasshopperDir =>
            Path.Combine(Path.GetDirectoryName(SystemDir.TrimEnd('\\', '/')), "Plug-ins", "Grasshopper");

        [ModuleInitializer]
        internal static void Install()
        {
            Environment.SetEnvironmentVariable("PATH", SystemDir + ";" + Environment.GetEnvironmentVariable("PATH"));
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                string name = new AssemblyName(args.Name).Name;
                if (name == null || name.EndsWith(".resources", StringComparison.Ordinal)) return null;
                foreach (string dir in new[] { SystemDir, GrasshopperDir })
                {
                    string path = Path.Combine(dir, name + ".dll");
                    if (File.Exists(path)) return Assembly.LoadFrom(path);
                }
                return null;
            };
        }
    }

    /// <summary>
    /// Rhino's core, started once for the whole run on its own STA thread, with no window. Native geometry
    /// (planes, curves) and PDF+'s layout need it. Test code runs on that thread through <see cref="Run{T}"/>.
    /// </summary>
    public sealed class RhinoFixture : IDisposable
    {
        private readonly BlockingCollection<Action> work = new BlockingCollection<Action>();
        private readonly ManualResetEventSlim ready = new ManualResetEventSlim();
        private readonly Thread thread;
        private Exception startError;

        public RhinoFixture()
        {
            thread = new Thread(Loop) { IsBackground = true, Name = "Rhino core" };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            ready.Wait();
            if (startError != null) throw new InvalidOperationException("Rhino 8 did not start in-process.", startError);
        }

        private void Loop()
        {
            IDisposable core = null;
            try { core = RhinoCoreStarter.Start(); }
            catch (Exception ex) { startError = ex; }
            ready.Set();
            if (core == null) return;

            foreach (Action item in work.GetConsumingEnumerable()) item();
            core.Dispose();
        }

        public T Run<T>(Func<T> func)
        {
            var result = new TaskCompletionSource<T>();
            work.Add(() =>
            {
                try { result.SetResult(func()); }
                catch (Exception ex) { result.SetException(ex); }
            });
            return result.Task.GetAwaiter().GetResult();
        }

        public void Run(Action action) => Run(() => { action(); return true; });

        public void Dispose()
        {
            work.CompleteAdding();
            thread.Join(TimeSpan.FromSeconds(60));
        }
    }

    // Kept apart so that RhinoCommon loads only when the core starts, after RhinoInstall has run.
    internal static class RhinoCoreStarter
    {
        public static IDisposable Start()
        {
            var core = new Rhino.Runtime.InProcess.RhinoCore(new[] { "/nosplash" }, Rhino.Runtime.InProcess.WindowStyle.NoWindow);
            // Grasshopper always runs with a Rhino document open, and PDF+ reads its tolerances from it
            // (curves become Béziers at RhinoDoc.ActiveDoc.ModelAbsoluteTolerance). A bare core has none.
            Rhino.RhinoDoc.ActiveDoc = Rhino.RhinoDoc.CreateHeadless(null);
            return core;
        }
    }

    [CollectionDefinition(Name)]
    public class RhinoCollection : ICollectionFixture<RhinoFixture>
    {
        public const string Name = "Rhino core";
    }
}
