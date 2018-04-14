using BruTile.Predefined;
using Mapsui.Layers;
using Mapsui.Projection;
using System;
using System.Collections.Generic;
using System.Linq;
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

namespace Mapsui.Demo.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            var point = new Point(8.542693, 47.368659);
            var sphericalPoint = SphericalMercator.FromLonLat(point.X, point.Y);

            MyMapControl.Map.NavigateTo(sphericalPoint);
            MyMapControl.Map.Viewport.Resolution = 12;
            
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var styleName = (styleBox.SelectedItem as ComboBoxItem).Tag as string;
            var mainDir = "../../../";

            var source = new VectorMbTilesSource(mainDir + @"tiles/zurich.mbtiles", mainDir + @"styles/" + styleName + "-style.json", mainDir + @"tile-cache/");
            MyMapControl.Map.Layers.Clear();
            MyMapControl.Map.Layers.Add(new TileLayer(source));
            MyMapControl.Map.ViewChanged(true);
        }
    }
}
