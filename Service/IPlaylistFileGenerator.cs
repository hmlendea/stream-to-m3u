namespace StreamToM3U.Service
{
    public interface IPlaylistFileGenerator
    {
        void GeneratePlaylist(string inputFile, string outputFile);
    }
}