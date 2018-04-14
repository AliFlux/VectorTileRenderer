using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.Projections;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VectorTileRenderer;

namespace Gmap.Demo.WinForms
{
    class VectorMbTilesProvider : GMapProvider
    {
        Style style;
        VectorTileRenderer.Sources.MbTilesSource provider;
        string cachePath;

        public VectorMbTilesProvider(string path, string stylePath, string cachePath)
        {
            style = new Style(stylePath);
            style.FontFallbackDirectory = @"styles/fonts/";
            this.cachePath = cachePath;

            provider = new VectorTileRenderer.Sources.MbTilesSource(path);
            style.SetSourceProvider(0, provider);

            this.BypassCache = true;
        }

        readonly Guid id = new Guid("36F6CE12-7191-1129-2C48-79DE8C9FB563");
        public override Guid Id
        {
            get
            {
                return id;
            }
        }

        readonly string name = "VectorTileRendererMap";
        public override string Name
        {
            get
            {
                return name;
            }
        }

        public override PureProjection Projection
        {
            get
            {
                return MercatorProjection.Instance;
            }
        }

        GMapProvider[] overlays;
        public override GMapProvider[] Overlays
        {
            get
            {
                if (overlays == null)
                {
                    overlays = new GMapProvider[] { this };
                }
                return overlays;
            }
        }

        public override PureImage GetTileImage(GPoint pos, int zoom)
        {
            var newY = (int)Math.Pow(2, zoom) - pos.Y - 1;

            var canvas = new SkiaCanvas();
            System.Windows.Media.Imaging.BitmapSource bitmapSource;

            try
            {
                bitmapSource = Renderer.RenderCached(cachePath, style, canvas, (int)pos.X, (int)newY, zoom, 256, 256, 1).Result;
            } catch(Exception)
            {
                bitmapSource = null;
            }

            if(bitmapSource == null)
            {
                bitmapSource = System.Windows.Media.Imaging.BitmapImage.Create(
                    2,
                    2,
                    96,
                    96,
                    System.Windows.Media.PixelFormats.Indexed1,
                    new System.Windows.Media.Imaging.BitmapPalette(new List<System.Windows.Media.Color> { System.Windows.Media.Colors.Transparent }),
                    new byte[] { 0, 0, 0, 0 },
                    1);
            }

            return GetTileImageFromArray(GetBytesFromBitmapSource(bitmapSource));
        }
        
        static byte[] GetBytesFromBitmapSource(System.Windows.Media.Imaging.BitmapSource bmp)
        {
            System.Windows.Media.Imaging.PngBitmapEncoder encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            //encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
            // byte[] bit = new byte[0];
            using (MemoryStream stream = new MemoryStream())
            {
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bmp));
                encoder.Save(stream);
                byte[] bit = stream.ToArray();
                stream.Close();

                return bit;
            }
        }


        Bitmap GetBitmap(System.Windows.Media.Imaging.BitmapSource source)
        {
            Bitmap bmp = new Bitmap(
              source.PixelWidth,
              source.PixelHeight,
              PixelFormat.Format32bppPArgb);
            BitmapData data = bmp.LockBits(
              new Rectangle(Point.Empty, bmp.Size),
              ImageLockMode.WriteOnly,
              PixelFormat.Format32bppPArgb);
            source.CopyPixels(
              System.Windows.Int32Rect.Empty,
              data.Scan0,
              data.Height * data.Stride,
              data.Stride);
            bmp.UnlockBits(data);
            return bmp;
        }

    }
}
