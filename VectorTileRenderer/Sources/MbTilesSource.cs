using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace VectorTileRenderer.Sources
{
    // MbTiles loading code in GIST by geobabbler
    // https://gist.github.com/geobabbler/9213392

    public class MbTilesSource : IVectorTileSource
    {
        public GlobalMercator.GeoExtent Bounds { get; private set; }
        public GlobalMercator.CoordinatePair Center { get; private set; }
        public int MinZoom { get; private set; }
        public int MaxZoom { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public string MBTilesVersion { get; private set; }
        public string Path { get; private set; }

        Dictionary<string, VectorTile> tileCache = new Dictionary<string, VectorTile>();

        private GlobalMercator gmt = new GlobalMercator();

        public MbTilesSource(string path)
        {
            this.Path = path;
            loadMetadata();
        }

        private void loadMetadata()
        {
            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(String.Format("Data Source={0};Version=3;", this.Path)))
                {
                    conn.Open();
                    using (SQLiteCommand cmd = new SQLiteCommand() { Connection = conn, CommandText = "SELECT * FROM metadata;" })
                    {
                        SQLiteDataReader reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            string name = reader["name"].ToString();
                            switch (name.ToLower())
                            {
                                case "bounds":
                                    string val = reader["value"].ToString();
                                    string[] vals = val.Split(new char[] { ',' });
                                    this.Bounds = new GlobalMercator.GeoExtent() { West = Convert.ToDouble(vals[0]), South = Convert.ToDouble(vals[1]), East = Convert.ToDouble(vals[2]), North = Convert.ToDouble(vals[3]) };
                                    break;
                                case "center":
                                    val = reader["value"].ToString();
                                    vals = val.Split(new char[] { ',' });
                                    this.Center = new GlobalMercator.CoordinatePair() { X = Convert.ToDouble(vals[0]), Y = Convert.ToDouble(vals[1]) };
                                    break;
                                case "minzoom":
                                    this.MinZoom = Convert.ToInt32(reader["value"]);
                                    break;
                                case "maxzoom":
                                    this.MaxZoom = Convert.ToInt32(reader["value"]);
                                    break;
                                case "name":
                                    this.Name = reader["value"].ToString();
                                    break;
                                case "description":
                                    this.Description = reader["value"].ToString();
                                    break;
                                case "version":
                                    this.MBTilesVersion = reader["value"].ToString();
                                    break;

                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new MemberAccessException("Could not load Mbtiles source file");
            }
        }

        public Stream GetRawTile(int x, int y, int zoom)
        {
            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(String.Format("Data Source={0};Version=3;", Path)))
                {
                    conn.Open();
                    using (SQLiteCommand cmd = new SQLiteCommand() { Connection = conn, CommandText = String.Format("SELECT * FROM tiles WHERE tile_column = {0} and tile_row = {1} and zoom_level = {2}", x, y, zoom) })
                    {
                        SQLiteDataReader reader = cmd.ExecuteReader();

                        if (reader.Read())
                        {
                            var stream = reader.GetStream(reader.GetOrdinal("tile_data"));
                            return stream;
                        }
                    }
                }
            }
            catch
            {
                throw new MemberAccessException("Could not load tile from Mbtiles");
            }

            return null;
        }

        public void ExtractTile(int x, int y, int zoom, string path)
        {
            if (File.Exists(path))
                System.IO.File.Delete(path);

            using (var fileStream = File.Create(path))
            using (Stream tileStream = GetRawTile(x, y, zoom))
            {
                tileStream.Seek(0, SeekOrigin.Begin);
                tileStream.CopyTo(fileStream);
            }
        }

        public async Task<VectorTile> GetVectorTile(int x, int y, int zoom)
        {
            var extent = new Rect(0, 0, 1, 1);
            bool overZoomed = false;

            if(zoom > MaxZoom)
            {
                var bounds = gmt.TileLatLonBounds(x, y, zoom);

                var northEast = new GlobalMercator.CoordinatePair();
                northEast.X = bounds.East;
                northEast.Y = bounds.North;

                var northWest = new GlobalMercator.CoordinatePair();
                northWest.X = bounds.West;
                northWest.Y = bounds.North;

                var southEast = new GlobalMercator.CoordinatePair();
                southEast.X = bounds.East;
                southEast.Y = bounds.South;

                var southWest = new GlobalMercator.CoordinatePair();
                southWest.X = bounds.West;
                southWest.Y = bounds.South;

                var center = new GlobalMercator.CoordinatePair();
                center.X = (northEast.X + southWest.X) / 2;
                center.Y = (northEast.Y + southWest.Y) / 2;

                var biggerTile = gmt.LatLonToTile(center.Y, center.X, MaxZoom);

                var biggerBounds = gmt.TileLatLonBounds(biggerTile.X, biggerTile.Y, MaxZoom);

                var newL = Utils.ConvertRange(northWest.X, biggerBounds.West, biggerBounds.East, 0, 1);
                var newT = Utils.ConvertRange(northWest.Y, biggerBounds.North, biggerBounds.South, 0, 1);

                var newR = Utils.ConvertRange(southEast.X, biggerBounds.West, biggerBounds.East, 0, 1);
                var newB = Utils.ConvertRange(southEast.Y, biggerBounds.North, biggerBounds.South, 0, 1);

                extent = new Rect(new Point(newL, newT), new Point(newR, newB));
                //thisZoom = MaxZoom;

                x = biggerTile.X;
                y = biggerTile.Y;
                zoom = MaxZoom;

                overZoomed = true;
            }
            
            try
            {
                var actualTile = await getCachedVectorTile(x, y, zoom);

                if (actualTile != null)
                {
                    actualTile.IsOverZoomed = overZoomed;
                    actualTile = actualTile.ApplyExtent(extent);
                }

                return actualTile;

            } catch(Exception e)
            {
                return null;
            }
        }

        async Task<VectorTile> getCachedVectorTile(int x, int y, int zoom)
        {
            var key = x.ToString() + "," + y.ToString() + "," + zoom.ToString();

            lock(key)
            {
                if (tileCache.ContainsKey(key))
                {
                    return tileCache[key];
                }

                using (var rawTileStream = GetRawTile(x, y, zoom))
                {
                    var pbfTileProvider = new PbfTileSource(rawTileStream);
                    var tile = pbfTileProvider.GetVectorTile(x, y, zoom).Result;
                    tileCache[key] = tile;

                    return tile;
                }
            }
            
        }

        async Task<Stream> ITileSource.GetTile(int x, int y, int zoom)
        {
            return GetRawTile(x, y, zoom);
        }
    }
}
