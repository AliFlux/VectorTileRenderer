using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace VectorTileRenderer
{
    public interface ICanvas
    {
        bool ClipOverflow { get; set; }

        void StartDrawing(double sizeX, double sizeY);

        void DrawBackground(Brush style);

        void DrawLineString(List<Point> geometry, Brush style);

        void DrawPolygon(List<Point> geometry, Brush style);

        void DrawPoint(Point geometry, Brush style);

        void DrawText(Point geometry, Brush style);

        void DrawTextOnPath(List<Point> geometry, Brush style);

        void DrawImage(Stream imageStream, Brush style);

        void DrawUnknown(List<List<Point>> geometry, Brush style);

        BitmapSource FinishDrawing();
    }
}
