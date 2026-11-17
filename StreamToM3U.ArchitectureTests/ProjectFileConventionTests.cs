using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using NUnit.Framework;

namespace StreamToM3U.ArchitectureTests
{
    /// <summary>
    /// Verifies the project and solution conventions declared by the checked <c>.csproj</c>
    /// files and the solution file. The checks read staged project metadata and never depend
    /// on the host file-system layout.
    /// </summary>
    [TestFixture]
    public sealed class ProjectFileConventionTests
    {
        static readonly string targetFramework = "net10.0";
        static readonly string nullableEnabledValue = "enable";
        static readonly string implicitUsingsDisabledValue = "disable";
        static readonly string latestLanguageVersion = "latest";

        [Test]
        public void GivenEveryProject_WhenItsTargetFrameworkIsChecked_ThenNet10IsDeclared()
        {
            foreach (ArchitectureProjectFile project in ArchitectureSourceRepository.Projects)
            {
                IReadOnlyList<string> targetFrameworks = ReadPropertyValues(project, "TargetFramework");

                Assert.That(targetFrameworks, Is.Not.Empty, $"The project '{project.Name}' declares no TargetFramework.");

                foreach (string declaredTargetFramework in targetFrameworks)
                {
                    Assert.That(
                        declaredTargetFramework,
                        Is.EqualTo(targetFramework),
                        $"The project '{project.Name}' targets '{declaredTargetFramework}'; every project must target {targetFramework}.");
                }
            }
        }

        [Test]
        public void GivenEveryProject_WhenItsCompilerSettingsAreChecked_ThenTheMandatoryPropertiesAreDeclared()
        {
            foreach (ArchitectureProjectFile project in ArchitectureSourceRepository.Projects)
            {
                Assert.Multiple(
                    () =>
                    {
                        Assert.That(
                            ReadSinglePropertyValue(project, "ImplicitUsings"),
                            Is.EqualTo(implicitUsingsDisabledValue),
                            $"The project '{project.Name}' must disable implicit usings.");

                        Assert.That(
                            ReadSinglePropertyValue(project, "LangVersion"),
                            Is.EqualTo(latestLanguageVersion),
                            $"The project '{project.Name}' must set LangVersion to latest.");

                        Assert.That(
                            ReadSinglePropertyValue(project, "Nullable"),
                            Is.EqualTo(nullableEnabledValue),
                            $"The project '{project.Name}' must enable nullable reference types.");

                        Assert.That(
                            ReadSinglePropertyValue(project, "IsPackable"),
                            Is.EqualTo("false"),
                            $"The project '{project.Name}' is not a NuGet package and must set IsPackable to false.");
                    });
            }
        }

        [Test]
        public void GivenEveryProject_WhenItsPackageReferencesAreChecked_ThenTheyAreAlphabeticallyOrdered()
        {
            foreach (ArchitectureProjectFile project in ArchitectureSourceRepository.Projects)
            {
                Assert.That(
                    ReadPackageReferences(project),
                    Is.Ordered.Using<string>(StringComparer.Ordinal),
                    $"The package references of '{project.Name}' must be ordered alphabetically by their Include attribute.");
            }
        }

        [Test]
        public void GivenEveryTestProject_WhenItsTestFlagsAreChecked_ThenTheyAreDeclaredExplicitly()
        {
            IReadOnlyList<ArchitectureProjectFile> testProjects = [.. ArchitectureSourceRepository.Projects
                .Where(project => IsTestProject(project))];

            Assert.That(testProjects, Is.Not.Empty, "The solution must declare at least one test project.");

            foreach (ArchitectureProjectFile project in testProjects)
            {
                Assert.Multiple(
                    () =>
                    {
                        Assert.That(
                            ReadSinglePropertyValue(project, "IsTestProject"),
                            Is.EqualTo("true"),
                            $"The test project '{project.Name}' must set IsTestProject to true.");

                        Assert.That(
                            ReadSinglePropertyValue(project, "ExcludeFromCodeCoverage"),
                            Is.EqualTo("true"),
                            $"The test project '{project.Name}' must set ExcludeFromCodeCoverage to true.");
                    });
            }
        }

        [Test]
        public void GivenTheSolution_WhenItsStructureIsChecked_ThenItIsSdkStyleWithoutDeclarationOrRootAttributes()
        {
            string content = File.ReadAllText(ArchitectureSourceRepository.Solution.FullPath);

            Assert.Multiple(
                () =>
                {
                    Assert.That(
                        content.StartsWith("<Solution>", StringComparison.Ordinal),
                        Is.True,
                        "The solution file must start with a '<Solution>' element that declares no attributes.");

                    Assert.That(
                        content.TrimEnd().EndsWith("</Solution>", StringComparison.Ordinal),
                        Is.True,
                        "The solution file must end with a '</Solution>' element.");

                    Assert.That(
                        content.Contains("<?xml", StringComparison.Ordinal),
                        Is.False,
                        "The solution file must not contain an XML declaration.");

                    Assert.That(
                        content.Contains("<Version", StringComparison.Ordinal),
                        Is.False,
                        "The solution file must not contain a Version element.");

                    Assert.That(
                        ArchitectureSourceRepository.Solution.FullPath,
                        Does.EndWith(".slnx"),
                        "The solution must use the .slnx format.");

                    foreach (XElement projectElement in XDocument
                        .Load(ArchitectureSourceRepository.Solution.FullPath)
                        .Descendants("Project"))
                    {
                        Assert.That(
                            projectElement.Attributes().Select(attribute => attribute.Name.LocalName),
                            Is.EqualTo(["Path"]),
                            "Every solution Project element must declare only the Path attribute.");
                    }
                });
        }

        [Test]
        public void GivenTheSolution_WhenItsProjectOrderIsChecked_ThenTheMainProjectPrecedesTheTestProjects()
        {
            IReadOnlyList<string> declaredProjectPaths = ArchitectureSourceRepository.Solution.DeclaredProjectPaths;

            Assert.That(declaredProjectPaths, Is.Not.Empty, "The solution must declare at least one project.");

            Assert.That(
                declaredProjectPaths[0],
                Is.EqualTo("StreamToM3U/StreamToM3U.csproj"),
                "The main project must be declared first in the solution.");

            IReadOnlyList<string> testProjectPaths = [.. declaredProjectPaths.Where(path => path.Contains("Tests/", StringComparison.Ordinal))];

            Assert.That(
                testProjectPaths,
                Is.Ordered.Using<string>(StringComparer.Ordinal),
                "The test projects must be listed alphabetically after the main project.");
        }

        [Test]
        public void GivenEveryProject_WhenItsLocationIsChecked_ThenItResidesInASiblingProjectFolder()
        {
            foreach (ArchitectureProjectFile project in ArchitectureSourceRepository.Projects)
            {
                Assert.That(
                    ArchitectureSourceRepository.Solution.HostsProjectInSiblingFolder(project.Name),
                    Is.True,
                    $"The project '{project.Name}' must reside in a sibling folder named after the project, without a 'src' nesting layer.");
            }
        }

        [Test]
        public void GivenTheUnitTestProject_WhenItsNameIsChecked_ThenItFollowsTheDocumentedNamingPattern()
        {
            ArchitectureProjectFile? unitTestProject = ArchitectureSourceRepository.Projects
                .FirstOrDefault(project => string.Equals(project.Name, "StreamToM3U.UnitTests", StringComparison.Ordinal));

            Assert.That(
                unitTestProject,
                Is.Not.Null,
                "The solution must declare a unit test project named 'StreamToM3U.UnitTests'.");

            Assert.That(
                ArchitectureSourceRepository.Solution.DeclaredProjectPaths,
                Does.Contain("StreamToM3U.UnitTests/StreamToM3U.UnitTests.csproj"),
                "The unit test project must be declared in the solution.");
        }

        static bool IsTestProject(ArchitectureProjectFile project)
            => project.Name.EndsWith("Tests", StringComparison.Ordinal) && !IsArchitectureProject(project);

        static bool IsArchitectureProject(ArchitectureProjectFile project)
            => project.Name.EndsWith("ArchitectureTests", StringComparison.Ordinal);

        static IReadOnlyList<string> ReadPropertyValues(ArchitectureProjectFile project, string propertyName)
            => [.. XDocument
                .Load(project.FullPath)
                .Descendants("PropertyGroup")
                .SelectMany(group => group.Elements(propertyName))
                .Select(element => element.Value.Trim())];

        static string? ReadSinglePropertyValue(ArchitectureProjectFile project, string propertyName)
            => ReadPropertyValues(project, propertyName).FirstOrDefault();

        static IReadOnlyList<string> ReadPackageReferences(ArchitectureProjectFile project)
            => [.. XDocument
                .Load(project.FullPath)
                .Descendants("PackageReference")
                .Select(element => element.Attribute("Include")?.Value ?? string.Empty)
                .Where(name => !string.IsNullOrWhiteSpace(name))];
    }
}