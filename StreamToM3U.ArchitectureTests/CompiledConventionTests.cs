using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

using NUnit.Framework;

using StreamToM3U.Configuration;
using StreamToM3U.DataAccess.DataObjects;
using StreamToM3U.Logging;
using StreamToM3U.Service;
using StreamToM3U.Service.Models;
using StreamToM3U.Service.Processors;
using StreamToM3U.Utils;

namespace StreamToM3U.ArchitectureTests
{
    /// <summary>
    /// Verifies compiled naming, sealing, interface pairing, mapping, and language-feature
    /// conventions against the production assembly.
    /// </summary>
    [TestFixture]
    public sealed class CompiledConventionTests
    {
        static readonly Assembly productionAssembly = typeof(IPlaylistFileGenerator).Assembly;

        static readonly string mappingNamespace = "StreamToM3U.Service.Mapping";

        [Test]
        public void GivenProductionAssembly_WhenLoaded_ThenOnlyTheProductionAssemblyIsAnalysed()
        {
            IReadOnlyList<Assembly> loadedAssemblies = [productionAssembly];

            Assert.Multiple(
                () =>
                {
                    Assert.That(loadedAssemblies, Has.Count.EqualTo(1));
                    Assert.That(loadedAssemblies[0].GetName().Name, Is.EqualTo("StreamToM3U"));
                    Assert.That(typeof(ArchitectureSourceRepository).Assembly, Is.Not.SameAs(productionAssembly));
                });
        }

        [Test]
        public void GivenProductionTypes_WhenEligibleClassesAreChecked_ThenTheyAreSealed()
        {
            IEnumerable<Type> eligibleClasses = productionAssembly
                .GetTypes()
                .Where(type =>
                    type.IsClass &&
                    !type.IsAbstract &&
                    type.Namespace is not null &&
                    !string.Equals(type.Namespace, mappingNamespace, StringComparison.Ordinal));

            foreach (Type type in eligibleClasses)
            {
                Assert.That(type.IsSealed, Is.True, $"The production class '{type.FullName}' must be sealed.");
            }
        }

        [Test]
        public void GivenSettingsModelsAndDataObjects_WhenChecked_ThenTheyArePublicSealedClasses()
        {
            Type[] expectedTypes =
            [
                typeof(ApplicationSettings),
                typeof(Options),
                typeof(ChannelStreamEntity),
                typeof(ChannelStream),
                typeof(StreamInfo)
            ];

            foreach (Type type in expectedTypes)
            {
                Assert.Multiple(
                    () =>
                    {
                        Assert.That(type.IsPublic, Is.True, $"The type '{type.FullName}' must be public.");
                        Assert.That(type.IsSealed, Is.True, $"The type '{type.FullName}' must be sealed.");
                        Assert.That(type.IsClass, Is.True, $"The type '{type.FullName}' must be a class.");
                    });
            }
        }

        [Test]
        public void GivenDataObjectsAndSettings_WhenTheirPropertiesAreChecked_ThenAllAreAutoPropertiesWithSetters()
        {
            Type[] propertyTypes =
            [
                typeof(ApplicationSettings),
                typeof(Options),
                typeof(ChannelStreamEntity),
                typeof(ChannelStream),
                typeof(StreamInfo)
            ];

            foreach (Type type in propertyTypes)
            {
                foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    Assert.Multiple(
                        () =>
                        {
                            Assert.That(property.CanRead, Is.True, $"The property '{type.Name}.{property.Name}' must be readable.");
                            Assert.That(
                                property.SetMethod is not null && property.SetMethod.IsPublic,
                                Is.True,
                                $"The property '{type.Name}.{property.Name}' must declare a public setter.");
                        });
                }
            }
        }

        [Test]
        public void GivenConcreteServices_WhenTheyExposePublicMethods_ThenAnEquivalentInterfaceExists()
        {
            Type[] serviceTypes =
            [
                typeof(FileDownloader),
                typeof(PlaylistFileGenerator),
                typeof(PlaylistUrlRetriever)
            ];

            foreach (Type serviceType in serviceTypes)
            {
                AssertConcreteServiceHasMatchingInterface(serviceType);
            }
        }

        [Test]
        public void GivenServiceInterfaces_WhenChecked_ThenTheyAreColocatedWithTheirImplementations()
        {
            Type[] implementationTypes =
            [
                typeof(FileDownloader),
                typeof(PlaylistFileGenerator),
                typeof(PlaylistUrlRetriever)
            ];

            foreach (Type implementationType in implementationTypes)
            {
                Type? interfaceType = implementationType
                    .GetInterfaces()
                    .FirstOrDefault(candidate => string.Equals(candidate.Name, $"I{implementationType.Name}", StringComparison.Ordinal));

                Assert.That(interfaceType, Is.Not.Null, $"The class '{implementationType.Name}' must implement 'I{implementationType.Name}'.");

                Assert.That(
                    interfaceType!.Namespace,
                    Is.EqualTo(implementationType.Namespace),
                    $"The interface '{interfaceType.Name}' must be colocated with '{implementationType.Name}'.");
            }
        }

        [Test]
        public void GivenStreamProvider_WhenChecked_ThenItIsARegularEnumWithTheDocumentedMembers()
        {
            Type providerType = typeof(StreamProvider);

            Assert.Multiple(
                () =>
                {
                    Assert.That(providerType.IsEnum, Is.True);
                    Assert.That(providerType.Namespace, Is.EqualTo("StreamToM3U.Configuration"));
                    Assert.That(Enum.GetNames<StreamProvider>(), Is.EqualTo(new[] { "Website", "TvSportHd", "AntenaPlay", "Streamlink" }));
                    Assert.That(providerType.IsDefined(typeof(FlagsAttribute), false), Is.False);
                });
        }

        [Test]
        public void GivenProcessors_WhenChecked_ThenEveryImplementationImplementsTheProcessorContract()
        {
            Type[] processors =
            [
                typeof(WebsiteProcessr),
                typeof(StreamlinkProcessor),
                typeof(TvSportHdProcessor),
                typeof(AntenaPlayProcessor)
            ];

            foreach (Type processor in processors)
            {
                Assert.Multiple(
                    () =>
                    {
                        Assert.That(processor.IsPublic, Is.True, $"The processor '{processor.FullName}' must be public.");
                        Assert.That(processor.IsSealed, Is.True, $"The processor '{processor.FullName}' must be sealed.");
                        Assert.That(
                            processor.GetInterfaces(),
                            Does.Contain(typeof(IProcessor)),
                            $"The processor '{processor.FullName}' must implement '{nameof(IProcessor)}'.");
                        Assert.That(
                            processor.Namespace,
                            Is.EqualTo("StreamToM3U.Service.Processors"),
                            $"The processor '{processor.FullName}' must reside in the processors namespace.");
                    });
            }
        }

        [Test]
        public void GivenMappingTypes_WhenChecked_ThenTheyAreInternalStaticMappingExtensions()
        {
            Type[] mappingTypes = [.. productionAssembly
                .GetTypes()
                .Where(type => string.Equals(type.Namespace, mappingNamespace, StringComparison.Ordinal))
                .Where(type => !type.IsDefined(typeof(CompilerGeneratedAttribute), false) && !type.Name.Contains('<'))];

            Assert.That(mappingTypes, Is.Not.Empty, "The mapping namespace must contain mapping types.");

            foreach (Type mappingType in mappingTypes)
            {
                Assert.Multiple(
                    () =>
                    {
                        Assert.That(mappingType.IsAbstract, Is.True, $"The mapping type '{mappingType.Name}' must be static.");
                        Assert.That(mappingType.IsSealed, Is.True, $"The mapping type '{mappingType.Name}' must be static.");
                        Assert.That(mappingType.IsNotPublic, Is.True, $"The mapping type '{mappingType.Name}' must be internal.");
                        Assert.That(
                    mappingType.Name,
                    Does.EndWith("MappingExtensions"),
                    $"The mapping type '{mappingType.Name}' must be named '<Noun>MappingExtensions'.");

                        foreach (MethodInfo method in mappingType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly))
                        {
                            Assert.Multiple(
                                () =>
                                {
                                    Assert.That(
                                        method.IsAssembly,
                                        Is.True,
                                        $"The mapping method '{mappingType.Name}.{method.Name}' must be internal.");

                                    Assert.That(
                                        method.GetCustomAttribute<ExtensionAttribute>() is not null,
                                        Is.True,
                                        $"The mapping method '{mappingType.Name}.{method.Name}' must be an extension method.");
                                });
                        }
                    });
            }
        }

        [Test]
        public void GivenMappingMethods_WhenChecked_ThenTheyFollowTheDocumentedConversionVocabulary()
        {
            string[] permittedNames = ["ToServiceModel", "ToServiceModels", "ToDataObject", "ToDataObjects"];

            IEnumerable<MethodInfo> mappingMethods = productionAssembly
                .GetTypes()
                .Where(type => string.Equals(type.Namespace, mappingNamespace, StringComparison.Ordinal))
                .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly));

            foreach (MethodInfo method in mappingMethods)
            {
                Assert.That(
                    permittedNames,
                    Does.Contain(method.Name),
                    $"The mapping method '{method.DeclaringType!.Name}.{method.Name}' does not follow the documented singular or plural conversion vocabulary.");
            }
        }

        [Test]
        public void GivenImplementationTypes_WhenTheirMethodsAreChecked_ThenNoMappingMethodIsDeclaredOutsideTheMappingNamespace()
        {
            IEnumerable<Type> implementationTypes = productionAssembly
                .GetTypes()
                .Where(type => !string.Equals(type.Namespace, mappingNamespace, StringComparison.Ordinal));

            foreach (Type type in implementationTypes)
            {
                foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    Assert.That(
                        method.Name.StartsWith("ToServiceModel", StringComparison.Ordinal) || method.Name.StartsWith("ToDataObject", StringComparison.Ordinal),
                        Is.False,
                        $"The mapping method '{type.Name}.{method.Name}' must live in the mapping namespace.");
                }
            }
        }

        [Test]
        public void GivenLoggingTypes_WhenChecked_ThenTheyResideInTheDedicatedLoggingNamespace()
        {
            Assert.Multiple(
                () =>
                {
                    Assert.That(typeof(MyOperation).Namespace, Is.EqualTo("StreamToM3U.Logging"));
                    Assert.That(typeof(MyLogInfoKey).Namespace, Is.EqualTo("StreamToM3U.Logging"));
                });
        }

        [Test]
        public void GivenProductionInterfaces_WhenNamed_ThenTheyUseTheIPrefixAndPascalCase()
        {
            IEnumerable<Type> interfaces = productionAssembly
                .GetTypes()
                .Where(type => type.IsInterface && !type.Name.Contains('`'));

            Assert.That(interfaces, Is.Not.Empty, "The production assembly must declare at least one interface.");

            foreach (Type interfaceType in interfaces)
            {
                Assert.Multiple(
                    () =>
                    {
                        Assert.That(
                            interfaceType.Name.StartsWith('I'),
                            Is.True,
                            $"The interface '{interfaceType.Name}' must use the 'I' prefix.");

                        Assert.That(
                            char.IsUpper(interfaceType.Name[1]),
                            Is.True,
                            $"The interface '{interfaceType.Name}' must use PascalCase after the 'I' prefix.");
                    });
            }
        }

        [Test]
        public void GivenProductionInterfaces_WhenChecked_ThenNoneResidesInAnInterfacesNamespace()
        {
            foreach (Type interfaceType in productionAssembly.GetTypes().Where(type => type.IsInterface))
            {
                Assert.That(
                    interfaceType.Namespace,
                    Does.Not.EndWith(".Interfaces"),
                    $"The interface '{interfaceType.FullName}' resides in an '.Interfaces' namespace; colocate it with its implementation.");
            }
        }

        [Test]
        public void GivenEntitiesAndSettings_WhenNamed_ThenThePrescribedSuffixesAndPatternsAreUsed()
        {
            string[] entitySuffixes = ["Entity", "DataObject"];

            foreach (Type type in productionAssembly.GetTypes())
            {
                if (type.Namespace is null || !type.Namespace.Contains("DataObjects", StringComparison.Ordinal))
                {
                    continue;
                }

                Assert.That(
                    entitySuffixes.Any(suffix => type.Name.EndsWith(suffix, StringComparison.Ordinal)),
                    Is.True,
                    $"The data-access type '{type.Name}' must end with 'DataObject' or 'Entity'.");
            }

            foreach (Type type in productionAssembly.GetTypes())
            {
                if (type.Namespace is null || !string.Equals(type.Namespace, "StreamToM3U.Configuration", StringComparison.Ordinal))
                {
                    continue;
                }

                bool isSettingsType = type.Name.EndsWith("Settings", StringComparison.Ordinal);
                bool isOptionsOrEnum = type.Name.EndsWith("Options", StringComparison.Ordinal) || type.IsEnum;

                Assert.That(
                    isSettingsType || isOptionsOrEnum,
                    Is.True,
                    $"The configuration type '{type.Name}' must be named '<Noun>Settings', '<Noun>Options', or be an enumeration.");
            }
        }

        [Test]
        public void GivenPublicMethods_WhenChecked_ThenTheyUsePascalCase()
        {
            foreach (Type type in productionAssembly.GetTypes())
            {
                if (!type.IsPublic || type.IsEnum || type.IsInterface)
                {
                    continue;
                }

                foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    if (method.IsSpecialName)
                    {
                        continue;
                    }

                    Assert.That(
                        char.IsUpper(method.Name[0]),
                        Is.True,
                        $"The public method '{type.Name}.{method.Name}' must use PascalCase.");
                }
            }
        }

        [Test]
        public void GivenEnumerations_WhenChecked_ThenNoValueIsExplicitlyNumberedAndValuesAreUnique()
        {
            foreach (Type type in productionAssembly.GetTypes().Where(type => type.IsEnum))
            {
                Array values = Enum.GetValues(type);
                List<object> distinctValues = [];

                foreach (object value in values)
                {
                    object underlyingValue = Convert.ChangeType(value, Enum.GetUnderlyingType(type), CultureInfo.InvariantCulture);
                    int numericValue = Convert.ToInt32(underlyingValue, CultureInfo.InvariantCulture);

                    Assert.That(
                        distinctValues.Contains(underlyingValue),
                        Is.False,
                        $"The enumeration '{type.Name}' declares the duplicate value '{numericValue}'.");

                    distinctValues.Add(underlyingValue);
                }
            }
        }

        [Test]
        public void GivenProductionTypes_WhenAccessibilityIsChecked_ThenNoTypeIsDeclaredPrivate()
        {
            foreach (Type type in productionAssembly.GetTypes())
            {
                if (type.IsDefined(typeof(CompilerGeneratedAttribute), false) || type.Name.Contains('<'))
                {
                    continue;
                }

                Assert.That(
                    type.IsNestedPrivate,
                    Is.False,
                    $"The production type '{type.FullName}' is nested private; extract it into its own file.");
            }
        }

        [Test]
        public void GivenProductionAssembly_WhenChecked_ThenNoTopLevelStatementsAreCompiled()
        {
            Assert.That(
                productionAssembly.EntryPoint,
                Is.Not.Null,
                "The production assembly must declare an explicit entry point, not top-level statements.");

            Assert.That(
                productionAssembly.EntryPoint!.DeclaringType?.Name,
                Is.EqualTo("Program"),
                "The production entry point must be declared by the 'Program' type.");
        }

        [Test]
        public void GivenProductionAssembly_WhenItsReferencesAreChecked_ThenNoTestAssemblyIsReferenced()
        {
            IReadOnlyList<string> referencedNames = [.. productionAssembly
                .GetReferencedAssemblies()
                .Select(assemblyName => assemblyName.Name ?? string.Empty)];

            Assert.Multiple(
                () =>
                {
                    Assert.That(referencedNames, Does.Not.Contain("StreamToM3U.UnitTests"));
                    Assert.That(referencedNames, Does.Not.Contain("StreamToM3U.ArchitectureTests"));
                    Assert.That(referencedNames, Does.Not.Contain("nunit.framework"));
                    Assert.That(referencedNames, Does.Not.Contain("Moq"));
                });
        }

        [Test]
        public void GivenProductionMethods_WhenChecked_ThenNoOutParameterIsDeclared()
        {
            foreach (Type type in productionAssembly.GetTypes())
            {
                foreach (MethodBase method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly).Cast<MethodBase>()
                    .Concat(type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)))
                {
                    bool hasOutParameter = method
                        .GetParameters()
                        .Any(parameter => parameter.IsOut && !parameter.ParameterType.IsByRefLike && !parameter.Name!.Contains("&"));

                    Assert.That(
                        hasOutParameter,
                        Is.False,
                        $"The member '{type.Name}.{method.Name}' declares an out parameter; return a dedicated type instead.");
                }
            }
        }

        [Test]
        public void GivenProductionTypes_WhenChecked_ThenNoTypeIsCompilerGeneratedInHandwrittenSources()
        {
            IEnumerable<Type> handwrittenTypes = productionAssembly
                .GetTypes()
                .Where(type => !type.IsDefined(typeof(CompilerGeneratedAttribute), false) && !type.Name.Contains('<'));

            foreach (Type type in handwrittenTypes)
            {
                Assert.That(
                    type.IsDefined(typeof(CompilerGeneratedAttribute), false) || type.Name.Contains('<'),
                    Is.False,
                    $"The production type '{type.FullName}' is compiler-generated; handwritten sources must not rely on generated types.");
            }
        }

        [Test]
        public void GivenProductionProperties_WhenChecked_ThenNoSettablePropertyIsIndexerOrStatic()
        {
            foreach (Type type in productionAssembly.GetTypes())
            {
                foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    Assert.That(
                        property.GetIndexParameters().Length == 0,
                        Is.True,
                        $"The property '{type.Name}.{property.Name}' must not declare indexer parameters.");
                }
            }
        }

        [Test]
        public void GivenProductionAssembly_WhenChecked_ThenInternalsVisibleToIsNotDeclared()
        {
            string[] friends = [.. productionAssembly
                .GetCustomAttributes<InternalsVisibleToAttribute>()
                .Select(attribute => attribute.AssemblyName)];

            Assert.That(friends, Is.Empty, $"The production assembly must not declare InternalsVisibleTo but declares: {string.Join(", ", friends)}.");
        }

        [Test]
        public void GivenProductionTypes_WhenChecked_ThenNoPreviewOrRequiredLanguageFeatureIsUsed()
        {
            IEnumerable<MemberInfo> members = productionAssembly
                .GetTypes()
                .Cast<MemberInfo>()
                .Concat(
                    productionAssembly
                        .GetTypes()
                        .SelectMany(type => type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)));

            foreach (MemberInfo member in members)
            {
                Assert.That(
                    member.GetCustomAttributes(typeof(CompilerFeatureRequiredAttribute), false),
                    Is.Empty,
                    $"The member '{member.DeclaringType?.FullName}.{member.Name}' requires a compiler feature that is not a stable, fully released language feature.");
            }
        }

        [Test]
        public void GivenProductionProperties_WhenChecked_ThenNoInitOnlySetterIsDeclared()
        {
            IEnumerable<PropertyInfo> properties = productionAssembly
                .GetTypes()
                .SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly));

            foreach (PropertyInfo property in properties)
            {
                if (property.SetMethod is null)
                {
                    continue;
                }

                bool isInitOnly = property.SetMethod.ReturnParameter
                    .GetRequiredCustomModifiers()
                    .Any(modifier => string.Equals(modifier.FullName, typeof(IsExternalInit).FullName, StringComparison.Ordinal));

                Assert.That(
                    isInitOnly,
                    Is.False,
                    $"The property '{property.DeclaringType!.Name}.{property.Name}' declares an init-only setter; regular setters are required.");
            }
        }

        void AssertConcreteServiceHasMatchingInterface(Type serviceType)
        {
            Type? interfaceType = productionAssembly.GetType($"{serviceType.Namespace}.I{serviceType.Name}");

            Assert.Multiple(
                () =>
                {
                    Assert.That(
                        interfaceType,
                        Is.Not.Null,
                        $"The service '{serviceType.FullName}' must have a colocated interface named 'I{serviceType.Name}'.");

                    Assert.That(
                        interfaceType is not null && serviceType.GetInterfaces().Contains(interfaceType),
                        Is.True,
                        $"The service '{serviceType.FullName}' must implement 'I{serviceType.Name}'.");
                });
        }
    }
}