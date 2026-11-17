using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using NUnit.Framework;

namespace StreamToM3U.ArchitectureTests
{
    /// <summary>
    /// Verifies the documented project-level dependency direction independently from the
    /// compiled type dependencies, together with the production assembly's friend assembly
    /// declarations and the architecture project's own required packages.
    /// </summary>
    [TestFixture]
    public sealed class ProjectReferenceConventionTests
    {
        static readonly string productionProjectName = "StreamToM3U";
        static readonly string unitTestProjectName = "StreamToM3U.UnitTests";
        static readonly string architectureProjectName = "StreamToM3U.ArchitectureTests";

        [Test]
        public void GivenTheUnitTestProject_WhenItsReferencesAreChecked_ThenItReferencesTheProductionProject()
        {
            ArchitectureProjectFile project = GetProject(unitTestProjectName);

            Assert.That(
                ReadProjectReferences(project),
                Does.Contain($"../{productionProjectName}/{productionProjectName}.csproj")
                    .Or.Contain($"{productionProjectName}/{productionProjectName}.csproj"),
                $"The unit test project must reference the production project.");
        }

        [Test]
        public void GivenTheProductionProject_WhenItsReferencesAreChecked_ThenItReferencesNoTestProject()
        {
            IReadOnlyList<string> references = ReadProjectReferences(GetProject(productionProjectName));

            Assert.That(
                references.Where(reference => reference.Contains("Tests", StringComparison.Ordinal)),
                Is.Empty,
                "The production project must never reference a test project.");
        }

        [Test]
        public void GivenTheArchitectureProject_WhenItsReferencesAreChecked_ThenItReferencesOnlyTheProductionProject()
        {
            IReadOnlyList<string> references = ReadProjectReferences(GetProject(architectureProjectName));

            Assert.Multiple(
                () =>
                {
                    Assert.That(
                        references,
                        Does.Contain($"../{productionProjectName}/{productionProjectName}.csproj")
                            .Or.Contain($"{productionProjectName}/{productionProjectName}.csproj"),
                        "The architecture test project must reference the production project.");

                    Assert.That(
                        references.Where(reference => reference.Contains("Tests", StringComparison.Ordinal)),
                        Is.Empty,
                        "The architecture test project must not reference another test project.");
                });
        }

        [Test]
        public void GivenTheUnitTestProject_WhenItsReferencesAreChecked_ThenItDoesNotReferenceTheArchitectureProject()
        {
            IReadOnlyList<string> references = ReadProjectReferences(GetProject(unitTestProjectName));

            Assert.That(
                references,
                Does.Not.Contain($"../{architectureProjectName}/{architectureProjectName}.csproj"),
                "The unit test project must not reference the architecture test project.");
        }

        [Test]
        public void GivenProductionSources_WhenInternalsVisibleToIsChecked_ThenItIsNotDeclared()
        {
            foreach (ArchitectureSourceFile sourceFile in ArchitectureSourceRepository.ProductionFiles)
            {
                string source = File.ReadAllText(sourceFile.FullPath);

                Assert.That(
                    source.Contains("InternalsVisibleTo", StringComparison.Ordinal),
                    Is.False,
                    $"The production source '{sourceFile.RelativePath}' must not declare InternalsVisibleTo.");
            }

            bool referencesATestProject = ReadProjectReferences(GetProject(productionProjectName))
                .Any(reference => reference.Contains("Tests", StringComparison.Ordinal));

            Assert.That(
                referencesATestProject,
                Is.False,
                "The production project must not be opened to test assemblies through a project reference.");
        }

        [Test]
        public void GivenTheArchitectureProject_WhenItsPackagesAreChecked_ThenNUnitArchUnitAndRoslynAreDeclared()
        {
            IReadOnlyList<string> packages = ReadPackageReferences(GetProject(architectureProjectName));

            Assert.Multiple(
                () =>
                {
                    Assert.That(packages, Does.Contain("NUnit"));
                    Assert.That(packages, Does.Contain("NUnit3TestAdapter"));
                    Assert.That(packages, Does.Contain("TngTech.ArchUnitNET"));
                    Assert.That(packages, Does.Contain("TngTech.ArchUnitNET.NUnit"));
                    Assert.That(packages, Does.Contain("Microsoft.CodeAnalysis.CSharp"));
                });
        }

        [Test]
        public void GivenTheArchitectureProject_WhenItsSourceInputsAreChecked_ThenEveryProductionAndTestSourceIsStaged()
        {
            ArchitectureProjectFile project = GetProject(architectureProjectName);
            IReadOnlyList<string> stagedProjects = [.. XDocument
                .Load(project.FullPath)
                .Descendants("ArchitectureSource")
                .Select(element => element.Attribute("Include")?.Value ?? string.Empty)
                .Select(value => value.Replace('\\', '/'))];

            Assert.Multiple(
                () =>
                {
                    Assert.That(
                        stagedProjects,
                        Does.Contain($"../{productionProjectName}/**/*.cs"),
                        "The architecture project must stage the production sources.");

                    Assert.That(
                        stagedProjects,
                        Does.Contain($"../{unitTestProjectName}/**/*.cs"),
                        "The architecture project must stage the test sources.");
                });
        }

        static ArchitectureProjectFile GetProject(string projectName)
            => ArchitectureSourceRepository.Projects
                .SingleOrDefault(project => string.Equals(project.Name, projectName, StringComparison.Ordinal))
                ?? throw new InvalidOperationException($"The project '{projectName}' was not staged for checking.");

        static IReadOnlyList<string> ReadProjectReferences(ArchitectureProjectFile project)
            => [.. XDocument
                .Load(project.FullPath)
                .Descendants("ProjectReference")
                .Select(element => (element.Attribute("Include")?.Value ?? string.Empty).Replace('\\', '/'))
                .Where(reference => !string.IsNullOrWhiteSpace(reference))];

        static IReadOnlyList<string> ReadPackageReferences(ArchitectureProjectFile project)
            => [.. XDocument
                .Load(project.FullPath)
                .Descendants("PackageReference")
                .Select(element => element.Attribute("Include")?.Value ?? string.Empty)
                .Where(name => !string.IsNullOrWhiteSpace(name))];
    }
}