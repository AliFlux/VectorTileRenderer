using BruTile;
using BruTile.Predefined;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VectorTileRenderer;

namespace Mapsui.Demo.WPF
{
    class VectorMbTilesSource : BruTile.ITileSource
    {
        VectorMbTilesProvider provider;

        public ITileSchema Schema { get; }
        public string Name { get; } = "VectorMbTileSource";
        public Attribution Attribution { get; } = new Attribution();

        public VectorMbTilesSource(string path, string stylePath, string cachePath)
        {
            Schema = GetTileSchema();
            provider = new VectorMbTilesProvider(path, stylePath, cachePath);
        }

        public static ITileSchema GetTileSchema()
        {
            var schema = new GlobalSphericalMercator(YAxis.TMS);
            //schema.Resolutions.Clear();
            //schema.Resolutions["0"] = new Resolution("0", 156543.033900000);
            //schema.Resolutions["1"] = new Resolution("1", 78271.516950000);
            return schema;
        }

        public byte[] GetTile(TileInfo tileInfo)
        {
            return provider.GetTile(tileInfo);
        }

        public ITileProvider Provider
        {
            get { return provider; }
        }

    }
}
