using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace StreamToM3U.ArchitectureTests
{
    /// <summary>
    /// Resolves the production, test, project, solution, and documentation inputs that the
    /// architecture checks examine. Every input is resolved from metadata staged next to the
    /// test assembly, so the checks never depend on the current working directory, the host
    /// file-system layout, or machine-specific paths.
    /// </summary>
    public static class ArchitectureSourceRepository
    {
        static readonly string SourcesDirectoryName = "ArchitectureSources";
        static readonly string SourcesProjectDirectoryName = "Sources";
        static readonly string ProjectsDirectoryName = "Projects";

        static readonly Lazy<IReadOnlyList<ArchitectureSourceFile>> SourceFiles = new(LoadSourceFiles);
        static readonly Lazy<IReadOnlyList<ArchitectureProjectFile>> ProjectFiles = new(LoadProjectFiles);
        static readonly Lazy<ArchitectureSolutionFile> SolutionFile = new(LoadSolutionFile);

        /// <summary>
        /// Gets the handwritten source files staged for the architecture checks.
        /// </summary>
        public static IReadOnlyList<ArchitectureSourceFile> Files => SourceFiles.Value;

        /// <summary>
        /// Gets the project files staged for the architecture checks.
        /// </summary>
        public static IReadOnlyList<ArchitectureProjectFile> Projects => ProjectFiles.Value;

        /// <summary>
        /// Gets the solution file staged for the architecture checks.
        /// </summary>
        public static ArchitectureSolutionFile Solution => SolutionFile.Value;

        /// <summary>
        /// Gets the source files that belong to the production project.
        /// </summary>
        public static IEnumerable<ArchitectureSourceFile> ProductionFiles
            => Files.Where(file => file.IsProduction);

        /// <summary>
        /// Gets the source files that belong to the test projects.
        /// </summary>
        public static IEnumerable<ArchitectureSourceFile> TestFiles
            => Files.Where(file => !file.IsProduction);

        /// <summary>
        /// Gets the staged root directory that holds every architecture input.
        /// </summary>
        public static string SourcesDirectory => ResolveSourcesDirectory();

        static string ResolveSourcesDirectory()
        {
            string? assemblyDirectory = Path.GetDirectoryName(
                typeof(ArchitectureSourceRepository).Assembly.Location);

            if (string.IsNullOrWhiteSpace(assemblyDirectory))
            {
                throw new InvalidOperationException(
                    "The architecture test assembly location could not be resolved.");
            }

            string sourcesDirectory = Path.Combine(assemblyDirectory!, SourcesDirectoryName);

            if (!Directory.Exists(sourcesDirectory))
            {
                throw new InvalidOperationException(
                    $"The architecture sources directory '{sourcesDirectory}' is missing. " +
                    "The build target that stages the checked inputs did not run.");
            }

            return sourcesDirectory;
        }

        static IReadOnlyList<ArchitectureSourceFile> LoadSourceFiles()
        {
            string sourcesDirectory = Path.Combine(SourcesDirectory, SourcesProjectDirectoryName);

            if (!Directory.Exists(sourcesDirectory))
            {
                throw new InvalidOperationException(
                    $"The architecture sources directory '{sourcesDirectory}' is missing. " +
                    "The build target that stages the checked sources did not run.");
            }

            List<ArchitectureSourceFile> files = [];

            foreach (string projectDirectory in Directory.EnumerateDirectories(sourcesDirectory))
            {
                string projectName = Path.GetFileName(projectDirectory);

                foreach (string filePath in Directory
                    .EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories))
                {
                    files.Add(new ArchitectureSourceFile(
                        projectName,
                        Path.GetRelativePath(projectDirectory, filePath).Replace('\\', '/'),
                        filePath));
                }
            }

            if (files.Count == 0)
            {
                throw new InvalidOperationException(
                    $"The architecture sources directory '{sourcesDirectory}' contains no C# files.");
            }

            return [.. files.OrderBy(file => file.RelativePath, StringComparer.Ordinal)];
        }

        static IReadOnlyList<ArchitectureProjectFile> LoadProjectFiles()
        {
            string projectsDirectory = Path.Combine(SourcesDirectory, ProjectsDirectoryName);

            if (!Directory.Exists(projectsDirectory))
            {
                throw new InvalidOperationException(
                    $"The architecture projects directory '{projectsDirectory}' is missing. " +
                    "The build target that stages the checked project files did not run.");
            }

            List<ArchitectureProjectFile> files = [.. Directory
                .EnumerateFiles(projectsDirectory, "*.csproj", SearchOption.TopDirectoryOnly)
                .Select(filePath => new ArchitectureProjectFile(
                    Path.GetFileNameWithoutExtension(filePath),
                    filePath))];

            if (files.Count == 0)
            {
                throw new InvalidOperationException(
                    $"The architecture projects directory '{projectsDirectory}' contains no project files.");
            }

            return [.. files.OrderBy(file => file.Name, StringComparer.Ordinal)];
        }

        static ArchitectureSolutionFile LoadSolutionFile()
        {
            string projectsDirectory = Path.Combine(SourcesDirectory, ProjectsDirectoryName);
            string? solutionPath = Directory
                .EnumerateFiles(projectsDirectory, "*.slnx", SearchOption.TopDirectoryOnly)
                .OrderBy(path => path, StringComparer.Ordinal)
                .FirstOrDefault();

            if (solutionPath is null)
            {
                throw new InvalidOperationException(
                    $"The architecture projects directory '{projectsDirectory}' contains no solution file.");
            }

            return new ArchitectureSolutionFile(solutionPath);
        }
    }
}