using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace VectorTileRenderer.Sources
{
    public interface IVectorTileSource : ITileSource
    {
        Task<VectorTile> GetVectorTile(int x, int y, int zoom);
    }
}
