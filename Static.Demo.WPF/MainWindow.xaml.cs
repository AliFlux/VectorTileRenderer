using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VectorTileRenderer;

namespace Demo.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        GlobalMercator gmt = new GlobalMercator();
        string mainDir = "../../../";

        public MainWindow()
        {
            InitializeComponent();

            // first, we extract necessary pbf tiles from mbtiles db

            var coords = gmt.LatLonToTile(47.371143, 8.543924, 14);
            var tileSource = new VectorTileRenderer.Sources.MbTilesSource(mainDir + @"tiles/zurich.mbtiles");
            tileSource.ExtractTile(coords.X, coords.Y, 14, mainDir + @"tiles/zurich.pbf.gz");

            coords = gmt.LatLonToTile(33.693189, 73.061415, 11);
            tileSource = new VectorTileRenderer.Sources.MbTilesSource(mainDir + @"tiles/islamabad.mbtiles");
            tileSource.ExtractTile(coords.X, coords.Y, 11, mainDir + @"tiles/islamabad.pbf.gz");
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            var functionName = (sender as RadioButton).Tag as string;

            // use a little reflection to call example function by name ;)
            MethodInfo theMethod = this.GetType().GetMethod(functionName, BindingFlags.Instance | BindingFlags.NonPublic);
            theMethod.Invoke(this, null);
        }

        void zurichMbTilesAliFluxStyle()
        {
            showMbTiles(mainDir + @"tiles/zurich.mbtiles", mainDir + @"styles/aliflux-style.json", 8579, 10645, 8581, 10647, 14, 512);
        }

        void zurichMbTilesBasicStyle()
        {
            showMbTiles(mainDir + @"tiles/zurich.mbtiles", mainDir + @"styles/basic-style.json", 8579, 10645, 8581, 10647, 14, 512);
        }

        void zurichMbTilesLibertyStyle()
        {
            showMbTiles(mainDir + @"tiles/zurich.mbtiles", mainDir + @"styles/liberty-style.json", 8579, 10645, 8581, 10647, 14, 512);
        }

        void zurichMbTilesBrightStyle()
        {
            showMbTiles(mainDir + @"tiles/zurich.mbtiles", mainDir + @"styles/bright-style.json", 8579, 10645, 8581, 10647, 14, 512);
        }

        void zurichMbTilesDarkStyle()
        {
            showMbTiles(mainDir + @"tiles/zurich.mbtiles", mainDir + @"styles/dark-style.json", 8579, 10645, 8581, 10647, 14, 512);
        }

        void islamabadMbTilesBrightStyle()
        {
            showMbTiles(mainDir + @"tiles/islamabad.mbtiles", mainDir + @"styles/bright-style.json", 1438, 1226, 1440, 1228, 11, 512);
        }

        void islamabadMbTilesLightStyle()
        {
            showMbTiles(mainDir + @"tiles/islamabad.mbtiles", mainDir + @"styles/light-style.json", 1438, 1226, 1440, 1228, 11, 512);
        }

        void guangzhouMbTilesAliFluxStyle()
        {
            //showMbTiles(mainDir + @"tiles/guangzhou.mbtiles", mainDir + @"styles/aliflux-style.json", 416, 288, 418, 290, 9, 512);
            showMbTiles(@"F:\AliData\C#\FlightMapper\FlightMapper\bin\Debug\tiles\asia.mbtiles", mainDir + @"styles/aliflux-style.json", 368, 311, 373, 313, 9, 512);
        }

        void zurichPbfBasicStyle()
        {
            showPbf(mainDir + @"tiles/zurich.pbf.gz", mainDir + @"styles/basic-style.json", 14);
        }

        void islamabadScalePbfBasicStyle()
        {
            showPbf(mainDir + @"tiles/islamabad.pbf.gz", mainDir + @"styles/basic-style.json", 11, 512, 2);
        }

        void islamabadSizePbfBasicStyle()
        {
            showPbf(mainDir + @"tiles/islamabad.pbf.gz", mainDir + @"styles/basic-style.json", 11, 1024, 1);
        }

        void newyorkPbfMbStreetsStyle()
        {
            showPbf(mainDir + @"tiles/newyork-mapbox.pbf", mainDir + @"styles/streets-style.json", 11);
        }

        void newyorkPbfMbRunnerStyle()
        {
            showPbf(mainDir + @"tiles/newyork-mapbox.pbf", mainDir + @"styles/Runner-style.json", 11);
        }

        void zurichOverzoomedMbTilesBasicStyle()
        {
            var coords = gmt.LatLonToTile(47.382047, 8.525868, 16);
            showMbTiles(mainDir + @"tiles/zurich.mbtiles", mainDir + @"styles/basic-style.json", coords.X, coords.Y, coords.X, coords.Y, 16, 512);
        }

        async void zurichMbTilesHybridStyle()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            // load style and font
            var style = new VectorTileRenderer.Style(mainDir + @"styles/hybrid-style.json");
            style.FontDirectory = mainDir + @"styles/fonts/";

            // set pbf as tile provider
            var vectorProvider = new VectorTileRenderer.Sources.PbfTileSource(mainDir + @"tiles/zurich.pbf.gz");
            style.SetSourceProvider(0, vectorProvider);

            // load raster source
            var rasterProvider = new VectorTileRenderer.Sources.RasterTileSource(mainDir + @"tiles/zurich.jpg");
            style.SetSourceProvider("satellite", rasterProvider);

            // render it on a skia canvas
            var canvas = new SkiaCanvas();
            var bitmapR = await Renderer.Render(style, canvas, 0, 0, 14, 256, 256, 1);
            demoImage.Source = bitmapR;

            scrollViewer.Background = new SolidColorBrush(style.GetBackgroundColor(14));

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            Console.WriteLine(elapsedMs + "ms time");
        }

        async void showPbf(string path, string stylePath, double zoom, double size = 512, double scale = 1)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            // load style and font
            var style = new VectorTileRenderer.Style(stylePath);
            style.FontDirectory = mainDir + @"styles/fonts/";

            // set pbf as tile provider
            var provider = new VectorTileRenderer.Sources.PbfTileSource(path);
            style.SetSourceProvider(0, provider);

            // render it on a skia canvas
            var canvas = new SkiaCanvas();
            var bitmapR = await Renderer.Render(style, canvas, 0, 0, zoom, size, size, scale);
            demoImage.Source = bitmapR;

            scrollViewer.Background = new SolidColorBrush(style.GetBackgroundColor(zoom));

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            Console.WriteLine(elapsedMs + "ms time");
        }

        async void showMbTiles(string path, string stylePath, int minX, int minY, int maxX, int maxY, int zoom, double size = 512, double scale = 1)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            // load style and font
            var style = new VectorTileRenderer.Style(stylePath);
            style.FontDirectory = mainDir + @"styles/fonts/";

            // set pbf as tile provider
            var provider = new VectorTileRenderer.Sources.MbTilesSource(path);
            style.SetSourceProvider(0, provider);

            BitmapSource[,] bitmapSources = new BitmapSource[maxX - minX + 1, maxY - minY + 1];

            // loop through tiles and render them
            Parallel.For(minX, maxX + 1, (int x) =>
            {
                Parallel.For(minY, maxY + 1, async (int y) =>
                {
                    var canvas = new SkiaCanvas();
                    var bitmapR = await Renderer.Render(style, canvas, x, y, zoom, size, size, scale);

                    if (bitmapR == null)
                    {

                    }

                    bitmapSources[x - minX, maxY - y] = bitmapR;
                });
            });

            // merge the tiles and show it
            var bitmap = mergeBitmaps(bitmapSources);
            demoImage.Source = bitmap;

            scrollViewer.Background = new SolidColorBrush(style.GetBackgroundColor(zoom));

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            Console.WriteLine(elapsedMs + "ms time");
        }

        BitmapSource mergeBitmaps(BitmapSource[,] bitmapSources)
        {
            DrawingVisual drawingVisual = new DrawingVisual();
            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                for (int x = 0; x < bitmapSources.GetLength(0); x++)
                {
                    for (int y = 0; y < bitmapSources.GetLength(1); y++)
                    {
                        drawingContext.DrawImage(bitmapSources[x, y], new Rect(x * bitmapSources[x, y].Width, y * bitmapSources[x, y].Height, bitmapSources[x, y].Width, bitmapSources[x, y].Height));
                    }
                }
            }

            RenderTargetBitmap bmp = new RenderTargetBitmap((int)(bitmapSources.GetLength(0) * bitmapSources[0, 0].Width), (int)(bitmapSources.GetLength(1) * bitmapSources[0, 0].Height), 96, 96, PixelFormats.Pbgra32);
            bmp.Render(drawingVisual);
            bmp.Freeze();

            return bmp;
        }

        private void demoImage_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
            e.Handled = true;
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Images|*.png;*.bmp;*.jpg";
            if (sfd.ShowDialog() != null)
            {
                string ext = System.IO.Path.GetExtension(sfd.FileName);
                BitmapEncoder encoder = new BmpBitmapEncoder();
                switch (ext)
                {
                    case ".jpg":
                        encoder = new JpegBitmapEncoder();
                        break;
                    case ".bmp":
                        encoder = new PngBitmapEncoder();
                        break;
                }

                encoder.Frames.Add(BitmapFrame.Create(demoImage.Source as BitmapSource));

                using (var fileStream = new System.IO.FileStream(sfd.FileName, System.IO.FileMode.Create))
                {
                    encoder.Save(fileStream);
                }
            }
        }
    }
}
