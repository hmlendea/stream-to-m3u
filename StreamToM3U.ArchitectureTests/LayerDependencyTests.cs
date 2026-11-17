using System;
using System.Collections.Generic;
using System.Linq;

using ArchUnitNET.Domain;
using ArchUnitNET.Domain.Dependencies;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.NUnit;

using NUnit.Framework;

using static ArchUnitNET.Fluent.ArchRuleDefinition;

using ProductionAssembly = System.Reflection.Assembly;

namespace StreamToM3U.ArchitectureTests
{
    /// <summary>
    /// Verifies the layer dependency matrix documented in <c>docs/architecture.md</c> and
    /// <c>ARCHITECTURE.md</c> against the compiled production assembly, using ArchUnit.NET.
    /// </summary>
    [TestFixture]
    public sealed class LayerDependencyTests
    {
        static readonly string EntryNamespacePattern = "^StreamToM3U$";

        static readonly string ConfigurationNamespacePattern = @"^StreamToM3U\.Configuration$";
        static readonly string ServiceNamespacePattern = @"^StreamToM3U\.Service$";
        static readonly string ModelsNamespacePattern = @"^StreamToM3U\.Service\.Models$";
        static readonly string MappingNamespacePattern = @"^StreamToM3U\.Service\.Mapping$";
        static readonly string ProcessorsNamespacePattern = @"^StreamToM3U\.Service\.Processors$";
        static readonly string DataAccessNamespacePattern = @"^StreamToM3U\.DataAccess(\.DataObjects)?$";
        static readonly string LoggingNamespacePattern = @"^StreamToM3U\.Logging$";
        static readonly string UtilsNamespacePattern = @"^StreamToM3U\.Utils$";

        static readonly string PlaylistWriterName = "PlaylistFileGenerator";
        static readonly string PlaylistRetrieverName = "PlaylistUrlRetriever";
        static readonly string OptionsName = "Options";

        static readonly string TvSportHdProcessorName = "TvSportHdProcessor";
        static readonly string AntenaPlayProcessorName = "AntenaPlayProcessor";
        static readonly string StreamlinkProcessorName = "StreamlinkProcessor";
        static readonly string WebsiteProcessorName = "WebsiteProcessr";

        static readonly Architecture Architecture = LoadProductionArchitecture();

        [Test]
        public void GivenProductionAssembly_WhenLoaded_ThenOnlyProductionTypesAreAnalysed()
        {
            IReadOnlyList<string> assemblyNames = [.. Architecture.Assemblies.Select(assembly => assembly.Name)];

            Assert.Multiple(
                () =>
                {
                    Assert.That(assemblyNames, Does.Contain("StreamToM3U"));
                    Assert.That(assemblyNames, Does.Not.Contain("StreamToM3U.UnitTests"));
                    Assert.That(assemblyNames, Does.Not.Contain("StreamToM3U.ArchitectureTests"));
                });
        }

        [Test]
        public void GivenTheDocumentedLayers_WhenSelected_ThenEveryLayerIsPopulated()
        {
            Assert.Multiple(
                () =>
                {
                    AssertNamespaceIsPopulated(EntryNamespacePattern);
                    AssertNamespaceIsPopulated(ConfigurationNamespacePattern);
                    AssertNamespaceIsPopulated(ServiceNamespacePattern);
                    AssertNamespaceIsPopulated(ModelsNamespacePattern);
                    AssertNamespaceIsPopulated(MappingNamespacePattern);
                    AssertNamespaceIsPopulated(ProcessorsNamespacePattern);
                    AssertNamespaceIsPopulated(DataAccessNamespacePattern);
                    AssertNamespaceIsPopulated(LoggingNamespacePattern);
                    AssertNamespaceIsPopulated(UtilsNamespacePattern);
                });
        }

        [Test]
        public void GivenTheConfigurationLayer_WhenItDependsOnOtherProductionLayers_ThenNoDependencyIsAllowed()
        {
            IArchRule rule = Types()
                .That().ResideInNamespaceMatching(ConfigurationNamespacePattern)
                .Should().NotDependOnAny(
                    Types().That().ResideInNamespaceMatching(ServiceNamespacePattern)
                        .Or().ResideInNamespaceMatching(ModelsNamespacePattern)
                        .Or().ResideInNamespaceMatching(MappingNamespacePattern)
                        .Or().ResideInNamespaceMatching(ProcessorsNamespacePattern)
                        .Or().ResideInNamespaceMatching(DataAccessNamespacePattern)
                        .Or().ResideInNamespaceMatching(LoggingNamespacePattern)
                        .Or().ResideInNamespaceMatching(UtilsNamespacePattern))
                .Because("configuration is a leaf area that supplies settings to every other layer");

            rule.Check(Architecture);
        }

        [Test]
        public void GivenTheUtilsLayer_WhenItDependsOnOtherProductionLayers_ThenNoDependencyIsAllowed()
        {
            IArchRule rule = Types()
                .That().ResideInNamespaceMatching(UtilsNamespacePattern)
                .Should().NotDependOnAny(
                    Types().That().ResideInNamespaceMatching(EntryNamespacePattern)
                        .Or().ResideInNamespaceMatching(ConfigurationNamespacePattern)
                        .Or().ResideInNamespaceMatching(ServiceNamespacePattern)
                        .Or().ResideInNamespaceMatching(ModelsNamespacePattern)
                        .Or().ResideInNamespaceMatching(MappingNamespacePattern)
                        .Or().ResideInNamespaceMatching(ProcessorsNamespacePattern)
                        .Or().ResideInNamespaceMatching(DataAccessNamespacePattern)
                        .Or().ResideInNamespaceMatching(LoggingNamespacePattern))
                .Because("utilities are a leaf area that must be usable by any layer without cycles");

            rule.Check(Architecture);
        }

        [Test]
        public void GivenTheLoggingLayer_WhenItDependsOnOtherProductionLayers_ThenNoDependencyIsAllowed()
        {
            IArchRule rule = Types()
                .That().ResideInNamespaceMatching(LoggingNamespacePattern)
                .Should().NotDependOnAny(
                    Types().That().ResideInNamespaceMatching(EntryNamespacePattern)
                        .Or().ResideInNamespaceMatching(ConfigurationNamespacePattern)
                        .Or().ResideInNamespaceMatching(ServiceNamespacePattern)
                        .Or().ResideInNamespaceMatching(ModelsNamespacePattern)
                        .Or().ResideInNamespaceMatching(MappingNamespacePattern)
                        .Or().ResideInNamespaceMatching(ProcessorsNamespacePattern)
                        .Or().ResideInNamespaceMatching(DataAccessNamespacePattern)
                        .Or().ResideInNamespaceMatching(UtilsNamespacePattern))
                .Because("logging is a leaf vocabulary that the other layers consume");

            rule.Check(Architecture);
        }

        [Test]
        public void GivenTheDataAccessLayer_WhenItDependsOnServiceOrUtilityLayers_ThenNoDependencyIsAllowed()
        {
            IArchRule rule = Types()
                .That().ResideInNamespaceMatching(DataAccessNamespacePattern)
                .Should().NotDependOnAny(
                    Types().That().ResideInNamespaceMatching(ServiceNamespacePattern)
                        .Or().ResideInNamespaceMatching(ModelsNamespacePattern)
                        .Or().ResideInNamespaceMatching(MappingNamespacePattern)
                        .Or().ResideInNamespaceMatching(ProcessorsNamespacePattern)
                        .Or().ResideInNamespaceMatching(UtilsNamespacePattern)
                        .Or().ResideInNamespaceMatching(LoggingNamespacePattern))
                .Because("data-access objects never reach processors and are mapped before use");

            rule.Check(Architecture);
        }

        [Test]
        public void GivenTheServiceModels_WhenTheyDependOnOrchestrationOrInfrastructure_ThenNoDependencyIsAllowed()
        {
            IArchRule rule = Types()
                .That().ResideInNamespaceMatching(ModelsNamespacePattern)
                .Should().NotDependOnAny(
                    Types().That().ResideInNamespaceMatching(ServiceNamespacePattern)
                        .Or().ResideInNamespaceMatching(MappingNamespacePattern)
                        .Or().ResideInNamespaceMatching(ProcessorsNamespacePattern)
                        .Or().ResideInNamespaceMatching(DataAccessNamespacePattern)
                        .Or().ResideInNamespaceMatching(LoggingNamespacePattern)
                        .Or().ResideInNamespaceMatching(UtilsNamespacePattern))
                .Because("service models are a leaf area that orchestration and processors consume");

            rule.Check(Architecture);
        }

        [Test]
        public void GivenTheMappingLayer_WhenItDependsOnOrchestrationOrUtilities_ThenNoDependencyIsAllowed()
        {
            IArchRule rule = Types()
                .That().ResideInNamespaceMatching(MappingNamespacePattern)
                .Should().NotDependOnAny(
                    Types().That().ResideInNamespaceMatching(ServiceNamespacePattern)
                        .Or().ResideInNamespaceMatching(ProcessorsNamespacePattern)
                        .Or().ResideInNamespaceMatching(LoggingNamespacePattern)
                        .Or().ResideInNamespaceMatching(UtilsNamespacePattern))
                .Because("mapping converts data-access objects into service models and nothing else");

            rule.Check(Architecture);
        }

        [Test]
        public void GivenTheProcessors_WhenTheyDependOnOrchestrationTypes_ThenNoDependencyIsAllowed()
        {
            IArchRule rule = Types()
                .That().ResideInNamespaceMatching(ProcessorsNamespacePattern)
                .Should().NotDependOnAny(
                    Types().That().HaveName(PlaylistWriterName)
                        .Or().HaveName(PlaylistRetrieverName)
                        .Or().HaveName(OptionsName))
                .Because("processors resolve a single stream and must not write playlists, dispatch providers, or read options");

            rule.Check(Architecture);
        }

        [Test]
        public void GivenTheProcessors_WhenTheyDependOnTheCompositionRoot_ThenNoDependencyIsAllowed()
        {
            IArchRule rule = Types()
                .That().ResideInNamespaceMatching(ProcessorsNamespacePattern)
                .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(EntryNamespacePattern))
                .Because("processors are constructed by the orchestration layer, never by the composition root directly");

            rule.Check(Architecture);
        }

        [Test]
        public void GivenTheOrchestrationLayer_WhenItDependsOnBrowserUtilities_ThenNoDependencyIsAllowed()
        {
            IArchRule rule = Types()
                .That().ResideInNamespaceMatching(ServiceNamespacePattern)
                .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(UtilsNamespacePattern))
                .Because("only processors may drive the shared browser handle");

            rule.Check(Architecture);
        }

        [Test]
        public void GivenTheOrchestrationLayer_WhenItDependsOnTheCompositionRoot_ThenNoDependencyIsAllowed()
        {
            IArchRule rule = Types()
                .That().ResideInNamespaceMatching(ServiceNamespacePattern)
                .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(EntryNamespacePattern))
                .Because("dependencies point inward, so services never reference the entry point");

            rule.Check(Architecture);
        }

        [Test]
        public void GivenTheCompositionRoot_WhenItDependsOnOrchestrationAndUtilities_ThenTheDependenciesAreDocumented()
        {
            Assert.Multiple(
                () =>
                {
                    Types()
                        .That().ResideInNamespaceMatching(EntryNamespacePattern)
                        .Should().DependOnAny(Types().That().ResideInNamespaceMatching(ServiceNamespacePattern))
                        .Because("the composition root wires and invokes the orchestration services")
                        .Check(Architecture);

                    Types()
                        .That().ResideInNamespaceMatching(EntryNamespacePattern)
                        .Should().DependOnAny(Types().That().ResideInNamespaceMatching(UtilsNamespacePattern))
                        .Because("the composition root closes the shared browser handle at shutdown")
                        .Check(Architecture);
                });
        }

        [Test]
        public void GivenTheProviderStrategy_WhenConcreteProcessorsAreReferenced_ThenOnlyTheFactoryDoesSo()
        {
            IArchRule rule = Types()
                .That().DoNotResideInNamespaceMatching(ProcessorsNamespacePattern)
                .And().DoNotHaveName(PlaylistRetrieverName)
                .Should().NotDependOnAny(
                    Types().That().HaveName(TvSportHdProcessorName)
                        .Or().HaveName(AntenaPlayProcessorName)
                        .Or().HaveName(StreamlinkProcessorName)
                        .Or().HaveName(WebsiteProcessorName))
                .Because("provider dispatch is selected by an enumeration switch in the retriever");

            rule.Check(Architecture);
        }

        [Test]
        public void GivenTheProductionAssembly_WhenItsNamespacesAreSliced_ThenOnlyTheDocumentedProcessorsServiceCycleExists()
        {
            IReadOnlyList<string> cycles = FindNamespaceCycles();

            Assert.That(
                cycles,
                Is.Empty,
                $"The production namespaces must be free of dependency cycles except the documented processors and service cycle, but the following cycles were found: {string.Join("; ", cycles)}.");
        }

        [Test]
        public void GivenTheLeafAreas_WhenTheirDependenciesAreChecked_ThenTheyAreIndependent()
        {
            string[] leafNamespaces =
            [
                "StreamToM3U.Configuration",
                "StreamToM3U.Logging",
                "StreamToM3U.Utils",
                "StreamToM3U.DataAccess",
                "StreamToM3U.DataAccess.DataObjects"
            ];

            foreach (string leafNamespace in leafNamespaces)
            {
                IReadOnlyList<string> dependencies = GetProductionDependencies(leafNamespace);

                Assert.That(
                    dependencies,
                    Is.Empty,
                    $"The leaf namespace '{leafNamespace}' must not depend on other production namespaces but depends on: {string.Join(", ", dependencies)}.");
            }
        }

        [Test]
        public void GivenTheModelsAndMappingLayers_WhenTheirDependenciesAreChecked_ThenOnlyConfigurationIsPermitted()
        {
            string[] permittedNamespaces =
            [
                "StreamToM3U.Configuration",
                "StreamToM3U.Service.Models",
                "StreamToM3U.DataAccess.DataObjects",
                "StreamToM3U.Service.Mapping"
            ];

            foreach (string layerNamespace in new[] { "StreamToM3U.Service.Models", "StreamToM3U.Service.Mapping" })
            {
                IReadOnlyList<string> unexpectedDependencies = [.. GetProductionDependencies(layerNamespace)
                    .Where(dependency => !permittedNamespaces.Contains(dependency, StringComparer.Ordinal))];

                Assert.That(
                    unexpectedDependencies,
                    Is.Empty,
                    $"The namespace '{layerNamespace}' may depend only on configuration and data-access objects but depends on: {string.Join(", ", unexpectedDependencies)}.");
            }
        }

        static IReadOnlyList<string> GetProductionDependencies(string namespaceName)
            => [.. Architecture
                .Namespaces
                .Where(namespaceDefinition => string.Equals(namespaceDefinition.FullName, namespaceName, StringComparison.Ordinal))
                .SelectMany(namespaceDefinition => namespaceDefinition.Types)
                .SelectMany(type => type.Dependencies)
                .Select(dependency => dependency.Target.Namespace.FullName)
                .Where(dependencyNamespace => !string.IsNullOrWhiteSpace(dependencyNamespace))
                .Select(dependencyNamespace => dependencyNamespace!)
                .Where(dependencyNamespace => !string.Equals(dependencyNamespace, namespaceName, StringComparison.Ordinal))
                .Where(dependencyNamespace => dependencyNamespace.StartsWith("StreamToM3U", StringComparison.Ordinal))
                .Distinct()
                .OrderBy(dependencyNamespace => dependencyNamespace, StringComparer.Ordinal)];

        static IReadOnlyList<string> FindNamespaceCycles()
        {
            string permittedCycleStart = "StreamToM3U.Service";
            string permittedCycleEnd = "StreamToM3U.Service.Processors";

            IReadOnlyDictionary<string, IReadOnlyList<string>> graph = BuildNamespaceGraph();
            List<string> cycles = [];

            foreach (KeyValuePair<string, IReadOnlyList<string>> node in graph)
            {
                foreach (string target in node.Value)
                {
                    if (!graph.TryGetValue(target, out IReadOnlyList<string>? targetDependencies))
                    {
                        continue;
                    }

                    bool targetDependsBack = targetDependencies.Contains(node.Key, StringComparer.Ordinal);

                    if (!targetDependsBack)
                    {
                        continue;
                    }

                    bool isDocumentedCycle =
                        (string.Equals(node.Key, permittedCycleStart, StringComparison.Ordinal) && string.Equals(target, permittedCycleEnd, StringComparison.Ordinal)) ||
                        (string.Equals(node.Key, permittedCycleEnd, StringComparison.Ordinal) && string.Equals(target, permittedCycleStart, StringComparison.Ordinal));

                    if (isDocumentedCycle)
                    {
                        continue;
                    }

                    string cycle = $"{node.Key} <-> {target}";

                    if (!cycles.Contains(cycle, StringComparer.Ordinal))
                    {
                        cycles.Add(cycle);
                    }
                }
            }

            return cycles;
        }

        static IReadOnlyDictionary<string, IReadOnlyList<string>> BuildNamespaceGraph()
        {
            Dictionary<string, SortedSet<string>> graph = new(StringComparer.Ordinal);

            foreach (Namespace namespaceDefinition in Architecture.Namespaces)
            {
                string namespaceName = namespaceDefinition.FullName;

                if (namespaceName.StartsWith("StreamToM3U", StringComparison.Ordinal))
                {
                    graph.TryAdd(namespaceName, new SortedSet<string>(StringComparer.Ordinal));
                }
            }

            foreach (Namespace namespaceDefinition in Architecture.Namespaces)
            {
                string namespaceName = namespaceDefinition.FullName;

                if (!graph.TryGetValue(namespaceName, out SortedSet<string>? targets))
                {
                    continue;
                }

                foreach (IType type in namespaceDefinition.Types)
                {
                    foreach (ITypeDependency dependency in type.Dependencies)
                    {
                        string? targetNamespace = dependency.Target.Namespace.FullName;

                        if (string.IsNullOrWhiteSpace(targetNamespace) ||
                            string.Equals(targetNamespace, namespaceName, StringComparison.Ordinal) ||
                            !graph.ContainsKey(targetNamespace))
                        {
                            continue;
                        }

                        targets!.Add(targetNamespace!);
                    }
                }
            }

            return graph.ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyList<string>)[.. pair.Value],
                StringComparer.Ordinal);
        }

        static void AssertNamespaceIsPopulated(string namespacePattern)
            => Assert.That(
                Types().That().ResideInNamespaceMatching(namespacePattern).GetObjects(Architecture),
                Is.Not.Empty,
                $"The namespace pattern '{namespacePattern}' selected no production types.");

        static Architecture LoadProductionArchitecture()
        {
            ProductionAssembly productionAssembly = typeof(Service.IPlaylistFileGenerator).Assembly;

            return new ArchLoader()
                .LoadAssemblies(productionAssembly)
                .Build();
        }
    }
}