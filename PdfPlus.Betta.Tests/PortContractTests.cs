using Betta.Attributes;
using Betta.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using Xunit;

namespace PdfPlus.Betta.Tests
{
    /// <summary>
    /// PDF+ on Betta has to look the same as PDF+ 2.1 to a Grasshopper user and to a saved .gh file: the same
    /// components, IDs, names, toolbar tabs, inputs and outputs. original-contract.json records what the 48 hand-written
    /// GH_Components of PDF+ 2.1 registered, read from their C# source before they were removed (branch betta-port,
    /// commit 4b78ff9). These tests compare it with what Betta publishes from PdfPlus.Betta's attributes.
    /// No Rhino core needed.
    /// </summary>
    public class PortContractTests
    {
        private static readonly IReadOnlyList<Original> Originals = Original.ReadAll();
        private static readonly IReadOnlyList<Ported> Ports = Ported.ReadAll();

        [Fact]
        public void EveryOriginalComponentIsPortedWithItsComponentGuid()
        {
            Assert.Equal(48, Originals.Count);

            var portedGuids = Ports.Select(p => p.Guid).ToHashSet();
            var originalGuids = Originals.Select(o => o.Guid).ToHashSet();

            string[] missing = Originals.Where(o => !portedGuids.Contains(o.Guid)).Select(o => $"{o.Name} ({o.File})").ToArray();
            string[] extra = Ports.Where(p => !originalGuids.Contains(p.Guid)).Select(p => p.Name).ToArray();

            Assert.True(missing.Length == 0, "Not ported: " + string.Join(", ", missing));
            Assert.True(extra.Length == 0, "Not in PDF+: " + string.Join(", ", extra));
            Assert.Equal(Ports.Count, portedGuids.Count);
        }

        [Fact]
        public void NamesAndToolbarPlacesMatchTheOriginal()
        {
            var problems = new List<string>();
            foreach (Original o in Originals)
            {
                Ported p = Ports.FirstOrDefault(x => x.Guid == o.Guid);
                if (p == null) continue;
                Check(problems, o.Name, "name", o.Name, p.Name);
                Check(problems, o.Name, "nickname", o.NickName, p.NickName);
                Check(problems, o.Name, "category", o.Category, p.Category);
                Check(problems, o.Name, "subcategory", o.SubCategory, p.SubCategory);
            }
            Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
        }

        [Fact]
        public void InputsMatchTheOriginalInOrder()
        {
            var problems = new List<string>();
            foreach (Original o in Originals)
            {
                Ported p = Ports.FirstOrDefault(x => x.Guid == o.Guid);
                if (p != null) CompareParams(problems, o.Name, "input", o.Inputs, p.Inputs);
            }
            Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
        }

        [Fact]
        public void OutputsMatchTheOriginalInOrder()
        {
            var problems = new List<string>();
            foreach (Original o in Originals)
            {
                Ported p = Ports.FirstOrDefault(x => x.Guid == o.Guid);
                if (p != null) CompareParams(problems, o.Name, "output", o.Outputs, p.Outputs);
            }
            Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
        }

        [Fact]
        public void EveryComponentKeepsItsOriginalIcon()
        {
            string[] resources = typeof(PdfPlus.Components.Documents).Assembly.GetManifestResourceNames();
            // Betta looks the icon up the same way: a manifest resource whose name ends with IconResource.
            string[] missing = Ports
                .Where(p => string.IsNullOrEmpty(p.Icon) || !resources.Any(r => r.EndsWith(p.Icon, StringComparison.OrdinalIgnoreCase)))
                .Select(p => $"{p.Name} ({p.Icon ?? "no IconResource"})")
                .ToArray();
            Assert.True(missing.Length == 0, "No embedded icon: " + string.Join(", ", missing));
        }

        private static void CompareParams(List<string> problems, string component, string kind,
            IReadOnlyList<(string Name, string NickName)> expected, IReadOnlyList<(string Name, string NickName)> actual)
        {
            if (expected.Count != actual.Count)
            {
                problems.Add($"{component}: {expected.Count} {kind}s in PDF+ ({Join(expected)}), {actual.Count} ported ({Join(actual)})");
                return;
            }
            for (int i = 0; i < expected.Count; i++)
            {
                Check(problems, component, $"{kind} {i} name", expected[i].Name, actual[i].Name);
                Check(problems, component, $"{kind} {i} nickname", expected[i].NickName, actual[i].NickName);
            }
        }

        private static string Join(IEnumerable<(string Name, string NickName)> ps) => string.Join(", ", ps.Select(p => p.Name));

        private static void Check(List<string> problems, string component, string what, string expected, string actual)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                problems.Add($"{component}: {what} is \"{actual}\", PDF+ has \"{expected}\"");
        }

        /// <summary>What Betta publishes for one [GrasshopperMethod].</summary>
        private sealed class Ported
        {
            public Guid Guid;
            public string Name, NickName, Category, SubCategory, Icon;
            public List<(string Name, string NickName)> Inputs = new List<(string, string)>();
            public List<(string Name, string NickName)> Outputs = new List<(string, string)>();

            public static IReadOnlyList<Ported> ReadAll()
            {
                Assembly port = typeof(PdfPlus.Components.Documents).Assembly;
                Type[] types;
                try { types = port.GetTypes(); }
                catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(t => t != null).ToArray(); }

                var result = new List<Ported>();
                foreach (Type type in types.Where(t => t.IsClass && typeof(IBettaCollection).IsAssignableFrom(t)))
                {
                    var collection = type.GetCustomAttribute<GrasshopperCollectionAttribute>();
                    foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                    {
                        var attr = method.GetCustomAttribute<GrasshopperMethodAttribute>();
                        if (attr == null) continue;

                        var p = new Ported
                        {
                            Guid = Guid.Parse(attr.Guid),
                            Name = attr.Name,
                            NickName = attr.NickName,
                            Category = attr.Category ?? collection?.Category,
                            SubCategory = attr.SubCategory ?? collection?.SubCategory,
                            Icon = attr.IconResource,
                        };
                        foreach (ParameterInfo param in method.GetParameters().Where(x => !IsService(x.ParameterType)))
                        {
                            var pa = param.GetCustomAttribute<GrasshopperParameterAttribute>();
                            p.Inputs.Add((pa?.Name ?? param.Name, pa?.NickName));
                        }
                        var outputs = method.ReturnParameter.GetCustomAttributes<GrasshopperOutputAttribute>().OrderBy(o => o.Index).ToList();
                        if (outputs.Count > 0) p.Outputs.AddRange(outputs.Select(o => (o.Name, o.NickName)));
                        else if (method.ReturnType != typeof(void)) p.Outputs.Add(("(unnamed)", null));
                        result.Add(p);
                    }
                }
                return result;
            }

            // Parameters Betta fills itself instead of making an input.
            private static bool IsService(Type t) =>
                t == typeof(IBettaReport) || t == typeof(CancellationToken) ||
                (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IProgress<>));
        }

        /// <summary>One hand-written PDF+ 2.1 GH_Component, as original-contract.json records it.</summary>
        private sealed class Original
        {
            public string File;
            public Guid Guid;
            public string Name, NickName, Category, SubCategory;
            public List<(string Name, string NickName)> Inputs;
            public List<(string Name, string NickName)> Outputs;

            public static IReadOnlyList<Original> ReadAll()
            {
                string path = Path.Combine(AppContext.BaseDirectory, "original-contract.json");
                using JsonDocument doc = JsonDocument.Parse(System.IO.File.ReadAllText(path));
                return doc.RootElement.EnumerateArray().Select(e => new Original
                {
                    File = e.GetProperty("file").GetString(),
                    Guid = e.GetProperty("guid").GetGuid(),
                    Name = e.GetProperty("name").GetString(),
                    NickName = e.GetProperty("nickName").GetString(),
                    Category = e.GetProperty("category").GetString(),
                    SubCategory = e.GetProperty("subCategory").GetString(),
                    Inputs = Params(e.GetProperty("inputs")),
                    Outputs = Params(e.GetProperty("outputs")),
                }).ToList();
            }

            private static List<(string Name, string NickName)> Params(JsonElement list) =>
                list.EnumerateArray().Select(p => (p.GetProperty("name").GetString(), p.GetProperty("nickName").GetString())).ToList();
        }
    }
}
