using ClipperLib;
using SkiaSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace VectorTileRenderer
{
    public class SkiaCanvas : ICanvas
    {
        int width;
        int height;

        WriteableBitmap bitmap;
        SKSurface surface;
        SKCanvas canvas;

        private GRContext grContext;
        GRBackendRenderTargetDesc renderTarget;

        public bool ClipOverflow { get; set; } = false;
        private Rect clipRectangle;
        List<IntPoint> clipRectanglePath;

        ConcurrentDictionary<string, SKTypeface> fontPairs = new ConcurrentDictionary<string, SKTypeface>();
        private static readonly Object fontLock = new Object();

        List<Rect> textRectangles = new List<Rect>();

        public void StartDrawing(double width, double height)
        {
            this.width = (int)width;
            this.height = (int)height;

            bitmap = new WriteableBitmap(this.width, this.height, 96, 96, PixelFormats.Pbgra32, null);
            bitmap.Lock();
            var info = new SKImageInfo(this.width, this.height, SKImageInfo.PlatformColorType, SKAlphaType.Premul);

            //var glInterface = GRGlInterface.CreateNativeGlInterface();
            //grContext = GRContext.Create(GRBackend.OpenGL, glInterface);

            //renderTarget = SkiaGL.CreateRenderTarget();
            //renderTarget.Width = this.width;
            //renderTarget.Height = this.height;


            surface = SKSurface.Create(info, bitmap.BackBuffer, bitmap.BackBufferStride);
            //surface = SKSurface.Create(grContext, renderTarget);
            canvas = surface.Canvas;

            double padding = -5;
            clipRectangle = new Rect(padding, padding, this.width - padding * 2, this.height - padding * 2);

            clipRectanglePath = new List<IntPoint>();
            clipRectanglePath.Add(new IntPoint((int)clipRectangle.Top, (int)clipRectangle.Left));
            clipRectanglePath.Add(new IntPoint((int)clipRectangle.Top, (int)clipRectangle.Right));
            clipRectanglePath.Add(new IntPoint((int)clipRectangle.Bottom, (int)clipRectangle.Right));
            clipRectanglePath.Add(new IntPoint((int)clipRectangle.Bottom, (int)clipRectangle.Left));

            //clipRectanglePath = new List<IntPoint>();
            //clipRectanglePath.Add(new IntPoint((int)clipRectangle.Top + 10, (int)clipRectangle.Left + 10));
            //clipRectanglePath.Add(new IntPoint((int)clipRectangle.Top + 10, (int)clipRectangle.Right - 10));
            //clipRectanglePath.Add(new IntPoint((int)clipRectangle.Bottom - 10, (int)clipRectangle.Right - 10));
            //clipRectanglePath.Add(new IntPoint((int)clipRectangle.Bottom - 10, (int)clipRectangle.Left + 10));
        }

        public void DrawBackground(Brush style)
        {
            canvas.Clear(new SKColor(style.Paint.BackgroundColor.R, style.Paint.BackgroundColor.G, style.Paint.BackgroundColor.B, style.Paint.BackgroundColor.A));
        }

        SKStrokeCap convertCap(PenLineCap cap)
        {
            if (cap == PenLineCap.Flat)
            {
                return SKStrokeCap.Butt;
            }
            else if (cap == PenLineCap.Round)
            {
                return SKStrokeCap.Round;
            }

            return SKStrokeCap.Square;
        }

        //private double getAngle(double x1, double y1, double x2, double y2)
        //{
        //    double degrees;

        //    if (x2 - x1 == 0)
        //    {
        //        if (y2 > y1)
        //            degrees = 90;
        //        else
        //            degrees = 270;
        //    }
        //    else
        //    {
        //        // Calculate angle from offset.
        //        double riseoverrun = (y2 - y1) / (x2 - x1);
        //        double radians = Math.Atan(riseoverrun);
        //        degrees = radians * (180 / Math.PI);

        //        if ((x2 - x1) < 0 || (y2 - y1) < 0)
        //            degrees += 180;
        //        if ((x2 - x1) > 0 && (y2 - y1) < 0)
        //            degrees -= 180;
        //        if (degrees < 0)
        //            degrees += 360;
        //    }
        //    return degrees;
        //}

        //private double getAngleAverage(double a, double b)
        //{
        //    a = a % 360;
        //    b = b % 360;

        //    double sum = a + b;
        //    if (sum > 360 && sum < 540)
        //    {
        //        sum = sum % 180;
        //    }
        //    return sum / 2;
        //}

        double clamp(double number, double min = 0, double max = 1)
        {
            return Math.Max(min, Math.Min(max, number));
        }

        List<List<Point>> clipPolygon(List<Point> geometry) // may break polygons into multiple ones
        {
            Clipper c = new Clipper();

            var polygon = new List<IntPoint>();

            foreach (var point in geometry)
            {
                polygon.Add(new IntPoint((int)point.X, (int)point.Y));
            }

            c.AddPolygon(polygon, PolyType.ptSubject);

            c.AddPolygon(clipRectanglePath, PolyType.ptClip);

            List<List<IntPoint>> solution = new List<List<IntPoint>>();

            bool success = c.Execute(ClipType.ctIntersection, solution, PolyFillType.pftNonZero, PolyFillType.pftEvenOdd);

            if (success && solution.Count > 0)
            {
                var result = solution.Select(s => s.Select(item => new Point(item.X, item.Y)).ToList()).ToList();
                return result;
            }

            return null;
        }

        List<Point> clipLine(List<Point> geometry)
        {
            return LineClipper.ClipPolyline(geometry, clipRectangle);
        }

        SKPath getPathFromGeometry(List<Point> geometry)
        {

            SKPath path = new SKPath
            {
                FillType = SKPathFillType.EvenOdd,
            };

            var firstPoint = geometry[0];

            path.MoveTo((float)firstPoint.X, (float)firstPoint.Y);
            foreach (var point in geometry.Skip(1))
            {
                var lastPoint = path.LastPoint;
                path.LineTo((float)point.X, (float)point.Y);
            }

            return path;
        }

        public void DrawLineString(List<Point> geometry, Brush style)
        {
            if (ClipOverflow)
            {
                geometry = clipLine(geometry);
                if (geometry == null)
                {
                    return;
                }
            }

            var path = getPathFromGeometry(geometry);
            if (path == null)
            {
                return;
            }

            SKPaint fillPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeCap = convertCap(style.Paint.LineCap),
                StrokeWidth = (float)style.Paint.LineWidth,
                Color = new SKColor(style.Paint.LineColor.R, style.Paint.LineColor.G, style.Paint.LineColor.B, (byte)clamp(style.Paint.LineColor.A * style.Paint.LineOpacity, 0, 255)),
                IsAntialias = true,
            };

            if (style.Paint.LineDashArray.Count() > 0)
            {
                var effect = SKPathEffect.CreateDash(style.Paint.LineDashArray.Select(n => (float)n).ToArray(), 0);
                fillPaint.PathEffect = effect;
            }

            //Console.WriteLine(style.Paint.LineWidth);

            canvas.DrawPath(path, fillPaint);
        }

        SKTextAlign convertAlignment(TextAlignment alignment)
        {
            if (alignment == TextAlignment.Center)
            {
                return SKTextAlign.Center;
            }
            else if (alignment == TextAlignment.Left)
            {
                return SKTextAlign.Left;
            }
            else if (alignment == TextAlignment.Right)
            {
                return SKTextAlign.Right;
            }

            return SKTextAlign.Center;
        }

        SKPaint getTextStrokePaint(Brush style)
        {
            var paint = new SKPaint()
            {
                IsStroke = true,
                StrokeWidth = (float)style.Paint.TextStrokeWidth,
                Color = new SKColor(style.Paint.TextStrokeColor.R, style.Paint.TextStrokeColor.G, style.Paint.TextStrokeColor.B, (byte)clamp(style.Paint.TextStrokeColor.A * style.Paint.TextOpacity, 0, 255)),
                TextSize = (float)style.Paint.TextSize,
                IsAntialias = true,
                TextEncoding = SKTextEncoding.Utf32,
                TextAlign = convertAlignment(style.Paint.TextJustify),
                Typeface = getFont(style.Paint.TextFont, style),
            };

            return paint;
        }

        SKPaint getTextPaint(Brush style)
        {
            var paint = new SKPaint()
            {
                Color = new SKColor(style.Paint.TextColor.R, style.Paint.TextColor.G, style.Paint.TextColor.B, (byte)clamp(style.Paint.TextColor.A * style.Paint.TextOpacity, 0, 255)),
                TextSize = (float)style.Paint.TextSize,
                IsAntialias = true,
                TextEncoding = SKTextEncoding.Utf32,
                TextAlign = convertAlignment(style.Paint.TextJustify),
                Typeface = getFont(style.Paint.TextFont, style),
                HintingLevel = SKPaintHinting.Normal,
            };

            return paint;
        }

        string transformText(string text, Brush style)
        {
            if (text.Length == 0)
            {
                return "";
            }

            if (style.Paint.TextTransform == TextTransform.Uppercase)
            {
                text = text.ToUpper();
            }
            else if (style.Paint.TextTransform == TextTransform.Lowercase)
            {
                text = text.ToLower();
            }

            var paint = getTextPaint(style);
            text = breakText(text, paint, style);

            return text;
            //return Encoding.UTF32.GetBytes(newText);
        }

        string breakText(string input, SKPaint paint, Brush style)
        {
            var restOfText = input;
            var brokenText = "";
            do
            {
                var lineLength = paint.BreakText(restOfText, (float)(style.Paint.TextMaxWidth * style.Paint.TextSize));

                if (lineLength == restOfText.Length)
                {
                    // its the end
                    brokenText += restOfText.Trim();
                    break;
                }

                var lastSpaceIndex = restOfText.LastIndexOf(' ', (int)(lineLength - 1));
                if (lastSpaceIndex == -1 || lastSpaceIndex == 0)
                {
                    // no more spaces, probably ;)
                    brokenText += restOfText.Trim();
                    break;
                }

                brokenText += restOfText.Substring(0, (int)lastSpaceIndex).Trim() + "\n";

                restOfText = restOfText.Substring((int)lastSpaceIndex, restOfText.Length - (int)lastSpaceIndex);

            } while (restOfText.Length > 0);

            return brokenText.Trim();
        }

        bool textCollides(Rect rectangle)
        {
            foreach (var rect in textRectangles)
            {
                if (rect.IntersectsWith(rectangle))
                {
                    return true;
                }
            }
            return false;
        }

        SKTypeface getFont(string[] familyNames, Brush style)
        {
            lock (fontLock)
            {
                foreach (var name in familyNames)
                {
                    if (fontPairs.ContainsKey(name))
                    {
                        return fontPairs[name];
                    }

                    if (style.GlyphsDirectory != null)
                    {
                        // check file system for ttf
                        var newType = SKTypeface.FromFile(System.IO.Path.Combine(style.GlyphsDirectory, name + ".ttf"));
                        if (newType != null)
                        {
                            fontPairs[name] = newType;
                            return newType;
                        }

                        // check file system for otf
                        newType = SKTypeface.FromFile(System.IO.Path.Combine(style.GlyphsDirectory, name + ".otf"));
                        if (newType != null)
                        {
                            fontPairs[name] = newType;
                            return newType;
                        }
                    }

                    var typeface = SKTypeface.FromFamilyName(name);
                    if (typeface.FamilyName == name)
                    {
                        // gotcha!
                        fontPairs[name] = typeface;
                        return typeface;
                    }
                }

                // all options exhausted...
                // get the first one
                var fallback = SKTypeface.FromFamilyName(familyNames.First());
                fontPairs[familyNames.First()] = fallback;
                return fallback;
            }
        }

        SKTypeface qualifyTypeface(string text, SKTypeface typeface)
        {
            var glyphs = new ushort[typeface.CountGlyphs(text)];
            if (glyphs.Length < text.Length)
            {
                var fm = SKFontManager.Default;
                var charIdx = (glyphs.Length > 0) ? glyphs.Length : 0;
                return fm.MatchCharacter(text[glyphs.Length]);
            }

            return typeface;
        }

        void qualifyTypeface(Brush style, SKPaint paint)
        {
            var glyphs = new ushort[paint.Typeface.CountGlyphs(style.Text)];
            if (glyphs.Length < style.Text.Length)
            {
                var fm = SKFontManager.Default;
                var charIdx = (glyphs.Length > 0) ? glyphs.Length : 0;
                var newTypeface = fm.MatchCharacter(style.Text[glyphs.Length]);

                if (newTypeface == null)
                {
                    return;
                }

                paint.Typeface = newTypeface;

                glyphs = new ushort[newTypeface.CountGlyphs(style.Text)];
                if (glyphs.Length < style.Text.Length)
                {
                    // still causing issues
                    // so we cut the rest
                    charIdx = (glyphs.Length > 0) ? glyphs.Length : 0;

                    style.Text = style.Text.Substring(0, charIdx);
                }
            }

        }

        public void DrawText(Point geometry, Brush style)
        {
            if (style.Paint.TextOptional)
            {
                // TODO check symbol collision
                //return;
            }

            var paint = getTextPaint(style);
            qualifyTypeface(style, paint);

            var strokePaint = getTextStrokePaint(style);
            var text = transformText(style.Text, style);
            var allLines = text.Split('\n');

            //paint.Typeface = qualifyTypeface(text, paint.Typeface);

            // detect collisions
            if (allLines.Length > 0)
            {
                var biggestLine = allLines.OrderBy(line => line.Length).Last();
                var bytes = Encoding.UTF32.GetBytes(biggestLine);

                var width = (int)(paint.MeasureText(bytes));
                int left = (int)(geometry.X - width / 2);
                int top = (int)(geometry.Y - style.Paint.TextSize / 2 * allLines.Length);
                int height = (int)(style.Paint.TextSize * allLines.Length);

                var rectangle = new Rect(left, top, width, height);
                rectangle.Inflate(5, 5);

                if (ClipOverflow)
                {
                    if (!clipRectangle.Contains(rectangle))
                    {
                        return;
                    }
                }

                if (textCollides(rectangle))
                {
                    // collision detected
                    return;
                }
                textRectangles.Add(rectangle);

                //var list = new List<Point>()
                //{
                //    rectangle.TopLeft,
                //    rectangle.TopRight,
                //    rectangle.BottomRight,
                //    rectangle.BottomLeft,
                //};

                //var brush = new Brush();
                //brush.Paint = new Paint();
                //brush.Paint.FillColor = Color.FromArgb(150, 255, 0, 0);

                //this.DrawPolygon(list, brush);
            }

            int i = 0;
            foreach (var line in allLines)
            {
                var bytes = Encoding.UTF32.GetBytes(line);
                float lineOffset = (float)(i * style.Paint.TextSize) - ((float)(allLines.Length) * (float)style.Paint.TextSize) / 2 + (float)style.Paint.TextSize;
                var position = new SKPoint((float)geometry.X + (float)(style.Paint.TextOffset.X * style.Paint.TextSize), (float)geometry.Y + (float)(style.Paint.TextOffset.Y * style.Paint.TextSize) + lineOffset);

                if (style.Paint.TextStrokeWidth != 0)
                {
                    canvas.DrawText(bytes, position, strokePaint);
                }

                canvas.DrawText(bytes, position, paint);
                i++;
            }

        }

        public void DrawTextOnPath(List<Point> geometry, Brush style)
        {
            // buggggyyyyyy
            // requires an amazing collision system to work :/
            // --
            return;

            if (ClipOverflow)
            {
                geometry = clipLine(geometry);
                if (geometry == null)
                {
                    return;
                }
            }

            var path = getPathFromGeometry(geometry);
            var text = transformText(style.Text, style);

            var left = geometry.Min(item => item.X) - style.Paint.TextSize;
            var top = geometry.Min(item => item.Y) - style.Paint.TextSize;
            var right = geometry.Max(item => item.X) + style.Paint.TextSize;
            var bottom = geometry.Max(item => item.Y) + style.Paint.TextSize;

            var rectangle = new Rect(left, top, right - left, bottom - top);

            if (textCollides(rectangle))
            {
                // collision detected
                return;
            }
            textRectangles.Add(rectangle);


            //var list = new List<Point>()
            //{
            //    rectangle.TopLeft,
            //    rectangle.TopRight,
            //    rectangle.BottomRight,
            //    rectangle.BottomLeft,
            //};

            //var brush = new Brush();
            //brush.Paint = new Paint();
            //brush.Paint.FillColor = Color.FromArgb(150, 255, 0, 0);

            //this.DrawPolygon(list, brush);


            var offset = new SKPoint((float)style.Paint.TextOffset.X, (float)style.Paint.TextOffset.Y);
            var bytes = Encoding.UTF32.GetBytes(text);
            if (style.Paint.TextStrokeWidth != 0)
            {
                // TODO optimize this DrawTextOnPath in Skia repo
                canvas.DrawTextOnPath(bytes, path, offset, getTextStrokePaint(style));
            }

            canvas.DrawTextOnPath(bytes, path, offset, getTextPaint(style));
        }

        public void DrawPoint(Point geometry, Brush style)
        {
            if (style.Paint.IconImage != null)
            {
                // draw icon here
            }
        }

        public void DrawPolygon(List<Point> geometry, Brush style)
        {
            List<List<Point>> allGeometries = null;
            if (ClipOverflow)
            {
                allGeometries = clipPolygon(geometry);
            } else
            {
                allGeometries = new List<List<Point>>() { geometry };
            }

            if(allGeometries == null)
            {
                return;
            }

            foreach(var geometryPart in allGeometries)
            {
                var path = getPathFromGeometry(geometryPart);
                if (path == null)
                {
                    return;
                }

                SKPaint fillPaint = new SKPaint
                {
                    Style = SKPaintStyle.Fill,
                    StrokeCap = convertCap(style.Paint.LineCap),
                    Color = new SKColor(style.Paint.FillColor.R, style.Paint.FillColor.G, style.Paint.FillColor.B, (byte)clamp(style.Paint.FillColor.A * style.Paint.FillOpacity, 0, 255)),
                    IsAntialias = true,
                };

                canvas.DrawPath(path, fillPaint);
            }

        }


        static SKImage toSKImage(BitmapSource bitmap)
        {
            // TODO: maybe keep the same color types where we can, instead of just going to the platform default
            var info = new SKImageInfo(bitmap.PixelWidth, bitmap.PixelHeight);
            var image = SKImage.Create(info);
            using (var pixmap = image.PeekPixels())
            {
                toSKPixmap(bitmap, pixmap);
            }
            return image;
        }

        static void toSKPixmap(BitmapSource bitmap, SKPixmap pixmap)
        {
            // TODO: maybe keep the same color types where we can, instead of just going to the platform default
            if (pixmap.ColorType == SKImageInfo.PlatformColorType)
            {
                var info = pixmap.Info;
                var converted = new FormatConvertedBitmap(bitmap, PixelFormats.Pbgra32, null, 0);
                converted.CopyPixels(new Int32Rect(0, 0, info.Width, info.Height), pixmap.GetPixels(), info.BytesSize, info.RowBytes);
            }
            else
            {
                // we have to copy the pixels into a format that we understand
                // and then into a desired format
                // TODO: we can still do a bit more for other cases where the color types are the same
                using (var tempImage = toSKImage(bitmap))
                {
                    tempImage.ReadPixels(pixmap, 0, 0);
                }
            }
        }

        public void DrawImage(Stream imageStream, Brush style)
        {
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = imageStream;
            bitmapImage.DecodePixelWidth = this.width;
            bitmapImage.DecodePixelHeight = this.height;
            bitmapImage.EndInit();

            var image = toSKImage(bitmapImage);

            canvas.DrawImage(image, new SKPoint(0, 0));
        }

        public void DrawUnknown(List<List<Point>> geometry, Brush style)
        {

        }

        public BitmapSource FinishDrawing()
        {
            //surface.Canvas.Flush();
            //grContext.


            bitmap.AddDirtyRect(new Int32Rect(0, 0, this.width, this.height));
            bitmap.Unlock();
            bitmap.Freeze();

            return bitmap;

        }
    }
}

