using System.IO;
using System.Threading.Tasks;

namespace VectorTileRenderer.Sources
{
    public class RasterTileSource : ITileSource
    {
        public string Path { get; private set; }

        public RasterTileSource(string path)
        {
            this.Path = path;
        }

        public async Task<Stream> GetTile(int x, int y, int zoom)
        {
            var qualifiedPath = Path
                .Replace("{x}", x.ToString())
                .Replace("{y}", y.ToString())
                .Replace("{z}", zoom.ToString());

            return File.Open(qualifiedPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }
    }
}
