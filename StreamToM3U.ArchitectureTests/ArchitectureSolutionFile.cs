using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace StreamToM3U.ArchitectureTests
{
    /// <summary>
    /// Represents the solution file that the architecture checks examine.
    /// </summary>
    /// <param name="FullPath">
    /// The absolute path of the staged solution file.
    /// </param>
    public sealed record ArchitectureSolutionFile(string FullPath)
    {
        /// <summary>
        /// Gets the declared project paths, in the order they appear in the solution file.
        /// </summary>
        public IReadOnlyList<string> DeclaredProjectPaths => ReadDeclaredProjectPaths();

        /// <summary>
        /// Gets the name of the solution file without its extension.
        /// </summary>
        public string Name => Path.GetFileNameWithoutExtension(FullPath);

        /// <summary>
        /// Determines whether the solution declares the project inside a sibling folder named after it.
        /// </summary>
        /// <param name="projectName">The project name to look up.</param>
        public bool HostsProjectInSiblingFolder(string projectName)
            => DeclaredProjectPaths.Any(
                path => string.Equals(path, $"{projectName}/{projectName}.csproj", StringComparison.Ordinal));

        IReadOnlyList<string> ReadDeclaredProjectPaths()
            => [.. XDocument
                .Load(FullPath)
                .Descendants("Project")
                .Select(element => element.Attribute("Path")?.Value ?? string.Empty)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => path.Replace('\\', '/'))];
    }
}