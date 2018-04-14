using Mapbox.Vector.Tile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace VectorTileRenderer.Sources
{
    public interface ITileSource
    {
        Task<Stream> GetTile(int x, int y, int zoom);
    }
}
