using System;

namespace VectorTileRenderer
{
/*
    GlobalMercator.cs
    Copyright (c) 2014 Bill Dollins. All rights reserved.
    http://blog.geomusings.com
*************************************************************   
	Based on GlobalMapTiles.js - part of Aggregate Map Tools
	Version 1.0
	Copyright (c) 2009 The Bivings Group
	All rights reserved.
	Author: John Bafford
	
	http://www.bivings.com/
	http://bafford.com/softare/aggregate-map-tools/
*************************************************************   
	Based on GDAL2Tiles / globalmaptiles.py
	Original python version Copyright (c) 2008 Klokan Petr Pridal. All rights reserved.
	http://www.klokan.cz/projects/gdal2tiles/
	
	Permission is hereby granted, free of charge, to any person obtaining a
	copy of this software and associated documentation files (the "Software"),
	to deal in the Software without restriction, including without limitation
	the rights to use, copy, modify, merge, publish, distribute, sublicense,
	and/or sell copies of the Software, and to permit persons to whom the
	Software is furnished to do so, subject to the following conditions:
	
	The above copyright notice and this permission notice shall be included
	in all copies or substantial portions of the Software.
	
	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
	OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
	THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
	FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
	DEALINGS IN THE SOFTWARE.
*/

    public class GlobalMercator
    {
        private int tileSize;
        private double initialResolution;
        private double originShift;
        public class CoordinatePair
        {
            public double X { get; set; }
            public double Y { get; set; }
        }

        public class TileAddress
        {
            public int X { get; set; }
            public int Y { get; set; }
        }

        public class GeoExtent
        {
            public double North { get; set; }
            public double South { get; set; }
            public double East { get; set; }
            public double West { get; set; }
        }

        public GlobalMercator()
        {
            this.tileSize = 256;
            this.initialResolution = 2 * Math.PI * 6378137 / tileSize;
            this.originShift = 2 * Math.PI * 6378137 / 2.0;
        }

        public CoordinatePair LatLonToMeters(double lat, double lon)
        {
            CoordinatePair retval = new CoordinatePair();
            try
            {
                retval.X = lon * this.originShift / 180.0;
                retval.Y = Math.Log(Math.Tan((90 + lat) * Math.PI / 360.0)) / (Math.PI / 180.0);

                retval.Y *= this.originShift / 180.0;
                return retval;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public CoordinatePair MetersToLatLon(double mx, double my)
        {
            CoordinatePair retval = new CoordinatePair();
            try
            {
                retval.X = (mx / this.originShift) * 180.0;
                retval.Y = (my / this.originShift) * 180.0;

                retval.Y = 180 / Math.PI * (2 * Math.Atan(Math.Exp(retval.Y * Math.PI / 180.0)) - Math.PI / 2.0);
                return retval;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public CoordinatePair PixelsToMeters(double px, double py, int zoom)
        {
            CoordinatePair retval = new CoordinatePair();
            try
            {
                var res = Resolution(zoom);
                retval.X = px * res - this.originShift;
                retval.Y = py * res - this.originShift;
                return retval;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public CoordinatePair MetersToPixels(double mx, double my, int zoom)
        {
            CoordinatePair retval = new CoordinatePair();
            try
            {
                var res = Resolution(zoom);
                retval.X = (mx + this.originShift) / res;
                retval.Y = (my + this.originShift) / res;
                return retval;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public TileAddress PixelsToTile(double px, double py)
        {
            TileAddress retval = new TileAddress();
            try
            {
                retval.X = (int)(Math.Ceiling(Convert.ToDouble(px / this.tileSize)) - 1);
                retval.Y = (int)(Math.Ceiling(Convert.ToDouble(py / this.tileSize)) - 1);
                return retval;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public TileAddress MetersToTile(double mx, double my, int zoom)
        {
            TileAddress retval = new TileAddress();
            try
            {
                var p = this.MetersToPixels(mx, my, zoom);
                retval = this.PixelsToTile(p.X, p.Y);
                return retval;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public TileAddress LatLonToTile(double lat, double lon, int zoom)
        {
            TileAddress retval = new TileAddress();
            try
            {
                var m = this.LatLonToMeters(lat, lon);
                retval = this.MetersToTile(m.X, m.Y, zoom);
                return retval;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public TileAddress LatLonToTileXYZ(double lat, double lon, int zoom)
        {
            TileAddress retval = new TileAddress();
            try
            {
                var m = this.LatLonToMeters(lat, lon);
                retval = this.MetersToTile(m.X, m.Y, zoom);
                retval.Y = (int)Math.Pow(2, zoom) - retval.Y - 1;
                return retval;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public GeoExtent TileBounds(int tx, int ty, int zoom)
        {
            GeoExtent retval = new GeoExtent();
            try
            {
                var min = this.PixelsToMeters(tx * this.tileSize, ty * this.tileSize, zoom);
                var max = this.PixelsToMeters((tx + 1) * this.tileSize, (ty + 1) * this.tileSize, zoom);
                retval = new GeoExtent() { North = max.Y, South = min.Y, East = max.X, West = min.X };
                return retval;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public GeoExtent TileLatLonBounds(int tx, int ty, int zoom)
        {
            GeoExtent retval = new GeoExtent();
            try
            {
                var bounds = this.TileBounds(tx, ty, zoom);
                var min = this.MetersToLatLon(bounds.West, bounds.South);
                var max = this.MetersToLatLon(bounds.East, bounds.North);
                retval = new GeoExtent() { North = max.Y, South = min.Y, East = max.X, West = min.X };
                return retval;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public TileAddress GoogleTile(int tx, int ty, int zoom)
        {
            TileAddress retval = new TileAddress();
            try
            {
                retval.X = tx;
                retval.Y = Convert.ToInt32((Math.Pow(2, zoom) - 1) - ty);
                return retval;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public string QuadTree(int tx, int ty, int zoom)
        {
            string retval = "";
            try
            {

                ty = ((1 << zoom) - 1) - ty;
                for (var i = zoom; i >= 1; i--)
                {
                    var digit = 0;

                    var mask = 1 << (i - 1);

                    if ((tx & mask) != 0)
                        digit += 1;

                    if ((ty & mask) != 0)
                        digit += 2;

                    retval += digit;
                }

                return retval;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public TileAddress QuadTreeToTile(string quadtree, int zoom)
        {
            TileAddress retval = new TileAddress();
            try
            {
                var tx = 0;
                var ty = 0;

                for (var i = zoom; i >= 1; i--)
                {
                    var ch = quadtree[zoom - i];
                    var mask = 1 << (i - 1);

                    var digit = ch - '0';

                    if (Convert.ToBoolean(digit & 1))
                        tx += mask;

                    if (Convert.ToBoolean(digit & 2))
                        ty += mask;
                }

                ty = ((1 << zoom) - 1) - ty;
                retval.X = tx;
                retval.Y = ty;
                return retval;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public string LatLonToQuadTree(double lat, double lon, int zoom)
        {
            string retval = "";
            try
            {

                var m = this.LatLonToMeters(lat, lon);
                var t = this.MetersToTile(m.X, m.Y, zoom);

                retval = this.QuadTree(Convert.ToInt32(t.X), Convert.ToInt32(t.Y), zoom);

                return retval;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private double Resolution(int zoom)
        {
            return this.initialResolution / (1 << zoom);
        }

    }
}
