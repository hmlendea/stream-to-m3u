using System;
using System.IO;

namespace StreamToM3U.ArchitectureTests
{
    /// <summary>
    /// Represents a handwritten C# source file that the architecture checks examine.
    /// </summary>
    /// <param name="ProjectName">
    /// The name of the solution project that declares the file.
    /// </param>
    /// <param name="RelativePath">
    /// The path of the file relative to the staged sources root, using forward slashes.
    /// </param>
    /// <param name="FullPath">
    /// The absolute path of the staged file.
    /// </param>
    public sealed record ArchitectureSourceFile(
        string ProjectName,
        string RelativePath,
        string FullPath)
    {
        /// <summary>
        /// Gets the name of the solution project that declares the production sources.
        /// </summary>
        public static string ProductionProjectName => "StreamToM3U";

        /// <summary>
        /// Gets a value indicating whether the file belongs to the production project.
        /// </summary>
        public bool IsProduction => string.Equals(ProjectName, ProductionProjectName, StringComparison.Ordinal);

        /// <summary>
        /// Gets the namespace implied by the file's project and folder path.
        /// </summary>
        public string ExpectedNamespace => BuildExpectedNamespace();

        string BuildExpectedNamespace()
        {
            string? directory = Path.GetDirectoryName(RelativePath);

            if (string.IsNullOrWhiteSpace(directory))
            {
                return ProjectName;
            }

            return $"{ProjectName}.{directory.Replace('\\', '.').Replace('/', '.')}";
        }
    }
}