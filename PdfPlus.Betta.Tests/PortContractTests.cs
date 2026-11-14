using Betta.Attributes;
using Betta.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Xunit;

namespace PdfPlus.Betta.Tests
{
    /// <summary>
    /// The port has to look the same as PDF+ to a Grasshopper user and to a saved .gh file: the same components, IDs,
    /// names, toolbar tabs, inputs and outputs. These tests read the original components' C# source
    /// (PdfPlus/Components) and compare it with what Betta publishes from PdfPlus.Betta's attributes.
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

        /// <summary>One hand-written PDF+ GH_Component, read from its C# source.</summary>
        private sealed class Original
        {
            public string File;
            public Guid Guid;
            public string Name, NickName, Category, SubCategory;
            public List<(string Name, string NickName)> Inputs = new List<(string, string)>();
            public List<(string Name, string NickName)> Outputs = new List<(string, string)>();

            public static IReadOnlyList<Original> ReadAll()
            {
                string components = Path.Combine(RepoRoot(), "PdfPlus", "Components");
                var result = new List<Original>();
                foreach (string file in Directory.GetFiles(components, "*.cs", SearchOption.AllDirectories))
                {
                    // PDF+ keeps some unfinished parameters as commented-out registrations (Author, Keywords, ...).
                    string src = StripComments(System.IO.File.ReadAllText(file));
                    if (!src.Contains("ComponentGuid") || Regex.IsMatch(src, @"\babstract\s+class\b")) continue;

                    var o = new Original { File = Path.GetRelativePath(components, file) };
                    o.Guid = Guid.Parse(Regex.Match(src, @"ComponentGuid[\s\S]*?new Guid\(""([0-9a-fA-F-]+)""\)").Groups[1].Value);

                    List<string> baseArgs = Args(src, src.IndexOf("base(", StringComparison.Ordinal) + 4);
                    o.Name = Eval(baseArgs[0]);
                    o.NickName = Eval(baseArgs[1]);
                    o.Category = Eval(baseArgs[3]);
                    o.SubCategory = Eval(baseArgs[4]);

                    int inputs = src.IndexOf("RegisterInputParams", StringComparison.Ordinal);
                    int outputs = src.IndexOf("RegisterOutputParams", StringComparison.Ordinal);
                    int solve = src.IndexOf("SolveInstance", outputs, StringComparison.Ordinal);
                    o.Inputs = Registrations(src, inputs, outputs);
                    o.Outputs = Registrations(src, outputs, solve < 0 ? src.Length : solve);
                    result.Add(o);
                }
                return result.OrderBy(o => o.Name).ToList();
            }

            // pManager.AddXParameter("Name", "Nick", ...) or pManager.AddParameter(param, "Name", "Nick", ...).
            private static List<(string, string)> Registrations(string src, int from, int to)
            {
                var list = new List<(string, string)>();
                foreach (Match m in Regex.Matches(src.Substring(from, to - from), @"pManager\.(Add\w*)\("))
                {
                    List<string> args = Args(src, from + m.Index + m.Length - 1);
                    int first = m.Groups[1].Value == "AddParameter" ? 1 : 0;
                    list.Add((Eval(args[first]), Eval(args[first + 1])));
                }
                return list;
            }

            private static string RepoRoot()
            {
                for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
                    if (Directory.Exists(Path.Combine(dir.FullName, "PdfPlus", "Components"))) return dir.FullName;
                throw new DirectoryNotFoundException("PdfPlus/Components not found above " + AppContext.BaseDirectory);
            }

            /// <summary>The source without // and /* */ comments; string literals (URLs in descriptions) are left alone.</summary>
            private static string StripComments(string src)
            {
                var sb = new StringBuilder(src.Length);
                for (int i = 0; i < src.Length; i++)
                {
                    char c = src[i];
                    char next = i + 1 < src.Length ? src[i + 1] : '\0';
                    if (c == '/' && next == '/')
                    {
                        while (i < src.Length && src[i] != '\n') i++;
                        sb.Append('\n');
                    }
                    else if (c == '/' && next == '*')
                    {
                        i += 2;
                        while (i + 1 < src.Length && !(src[i] == '*' && src[i + 1] == '/')) i++;
                        i++;
                    }
                    else if (c == '"')
                    {
                        bool verbatim = i > 0 && src[i - 1] == '@';
                        sb.Append(c);
                        for (i++; i < src.Length; i++)
                        {
                            sb.Append(src[i]);
                            if (!verbatim && src[i] == '\\' && i + 1 < src.Length) { sb.Append(src[++i]); continue; }
                            if (src[i] == '"')
                            {
                                if (verbatim && i + 1 < src.Length && src[i + 1] == '"') { sb.Append(src[++i]); continue; }
                                break;
                            }
                        }
                    }
                    else sb.Append(c);
                }
                return sb.ToString();
            }

            /// <summary>The comma-separated arguments of the call whose '(' is at <paramref name="open"/>.</summary>
            private static List<string> Args(string src, int open)
            {
                var args = new List<string>();
                var current = new StringBuilder();
                int depth = 0;
                for (int i = open + 1; i < src.Length; i++)
                {
                    char c = src[i];
                    if (c == '"')
                    {
                        bool verbatim = i > 0 && src[i - 1] == '@';
                        int start = i++;
                        while (i < src.Length)
                        {
                            if (!verbatim && src[i] == '\\') { i += 2; continue; }
                            if (src[i] == '"')
                            {
                                if (verbatim && i + 1 < src.Length && src[i + 1] == '"') { i += 2; continue; }
                                break;
                            }
                            i++;
                        }
                        current.Append(src, start, i - start + 1);
                        continue;
                    }
                    if (c == '(' || c == '[' || c == '{') depth++;
                    else if (c == ')' || c == ']' || c == '}')
                    {
                        if (depth == 0) { args.Add(current.ToString().Trim()); return args; }
                        depth--;
                    }
                    else if (c == ',' && depth == 0) { args.Add(current.ToString().Trim()); current.Clear(); continue; }
                    current.Append(c);
                }
                throw new FormatException("Unclosed call at " + open);
            }

            /// <summary>A string literal, a concatenation of them, or a PDF+ Constants member such as Constants.Block.Name.</summary>
            private static string Eval(string expr)
            {
                expr = expr.Trim();
                if (expr.StartsWith("@\"", StringComparison.Ordinal))
                    return expr.Substring(2, expr.Length - 3).Replace("\"\"", "\"");
                if (expr.StartsWith("\"", StringComparison.Ordinal) && expr.EndsWith("\"", StringComparison.Ordinal) && !expr.Contains("\" +"))
                    return Regex.Unescape(expr.Substring(1, expr.Length - 2));
                if (expr.Contains("+"))
                    return string.Concat(Regex.Split(expr, @"\+(?=(?:[^""]*""[^""]*"")*[^""]*$)").Select(Eval));

                Match member = Regex.Match(expr, @"^Constants\.(\w+)(?:\.(\w+))?$");
                if (!member.Success) return expr;
                object value = typeof(Constants).GetProperty(member.Groups[1].Value, BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                if (member.Groups[2].Success && value != null)
                    value = value.GetType().GetProperty(member.Groups[2].Value)?.GetValue(value);
                return value?.ToString() ?? expr;
            }
        }
    }
}
