using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;

namespace VectorTileRenderer
{
    public class Brush
    {
        public int ZIndex { get; set; } = 0;
        public Paint Paint { get; set; }
        public string TextField { get; set; }
        public string Text { get; set; }
        public string GlyphsDirectory { get; set; } = null;
        public Layer _layer { get; set; }
    }

    public enum SymbolPlacement
    {
        Point,
        Line
    }

    public enum TextTransform
    {
        None,
        Uppercase,
        Lowercase
    }

    public class Paint
    {
        public Color BackgroundColor { get; set; }
        public string BackgroundPattern { get; set; }
        public double BackgroundOpacity { get; set; } = 1;

        public Color FillColor { get; set; }
        public string FillPattern { get; set; }
        public Point FillTranslate { get; set; } = new Point();
        public double FillOpacity { get; set; } = 1;

        public Color LineColor { get; set; }
        public string LinePattern { get; set; }
        public Point LineTranslate { get; set; } = new Point();
        public PenLineCap LineCap { get; set; } = PenLineCap.Flat;
        public double LineWidth { get; set; } = 1;
        public double LineOffset { get; set; } = 0;
        public double LineBlur { get; set; } = 0;
        public double[] LineDashArray { get; set; } = new double[0];
        public double LineOpacity { get; set; } = 1;

        public SymbolPlacement SymbolPlacement { get; set; } = SymbolPlacement.Point;
        public double IconScale { get; set; } = 1;
        public string IconImage { get; set; }
        public double IconRotate { get; set; } = 0;
        public Point IconOffset { get; set; } = new Point();
        public double IconOpacity { get; set; } = 1;

        public Color TextColor { get; set; }
        public string[] TextFont { get; set; } = new string[] { "Open Sans Regular", "Arial Unicode MS Regular" };
        public double TextSize { get; set; } = 16;
        public double TextMaxWidth { get; set; } = 10;
        public TextAlignment TextJustify { get; set; } = TextAlignment.Center;
        public double TextRotate { get; set; } = 0;
        public Point TextOffset { get; set; } = new Point();
        public Color TextStrokeColor { get; set; }
        public double TextStrokeWidth { get; set; } = 0;
        public double TextStrokeBlur { get; set; } = 0;
        public bool TextOptional { get; set; } = false;
        public TextTransform TextTransform { get; set; } = TextTransform.None;
        public double TextOpacity { get; set; } = 1;

        public bool Visibility { get; set; } = true; // visibility
    }

    //class ComparableColor:IComparable
    //{
    //    private long numericColor;

    //    public ComparableColor(string encodedColor)
    //    {

    //    }

    //    public int CompareTo(object obj)
    //    {
    //        if(obj.GetType() != typeof(ComparableColor))
    //        {
    //            return -1;
    //        }

    //        return numericColor.CompareTo((ComparableColor)obj);
    //    }
    //}

    public class Layer
    {
        public int Index { get; set; } = -1;
        public string ID { get; set; } = "";
        public string Type { get; set; } = "";
        public string SourceName { get; set; } = "";
        public Source Source { get; set; } = null;
        public string SourceLayer { get; set; } = "";
        public Dictionary<string, object> Paint { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> Layout { get; set; } = new Dictionary<string, object>();
        public object[] Filter { get; set; } = new object[0];
        public double? MinZoom { get; set; } = null;
        public double? MaxZoom { get; set; } = null;
    }

    public class Source
    {
        public string URL { get; set; } = "";
        public string Type { get; set; } = "";
        public string Name { get; set; } = "";
        public Sources.ITileSource Provider { get; set; } = null;
        public double? MinZoom { get; set; } = null;
        public double? MaxZoom { get; set; } = null;
    }

    public class Style
    {
        public readonly string Hash = "";
        public List<Layer> Layers = new List<Layer>();
        public Dictionary<string, Source> Sources = new Dictionary<string, Source>();
        public Dictionary<string, object> Metadata = new Dictionary<string, object>();
        //double screenScale = 0.2;// = 0.3;
        //double emToPx = 16;

        ConcurrentDictionary<string, Brush[]> brushesCache = new ConcurrentDictionary<string, Brush[]>();

        public string FontDirectory { get; set; } = null;

        public Style(string path, double scale = 1)
        {
            var json = System.IO.File.ReadAllText(path);
            dynamic jObject = JObject.Parse(json);

            if (jObject["metadata"] != null)
            {
                Metadata = jObject.metadata.ToObject<Dictionary<string, object>>();
            }

            List<string> fontNames = new List<string>();

            foreach (JProperty jSource in jObject.sources)
            {
                var source = new Source();

                IDictionary<string, JToken> sourceDict = jSource.Value as JObject;

                source.Name = jSource.Name;

                if (sourceDict.ContainsKey("url"))
                {
                    source.URL = plainifyJson(sourceDict["url"]) as string;
                }

                if (sourceDict.ContainsKey("type"))
                {
                    source.Type = plainifyJson(sourceDict["type"]) as string;
                }

                if (sourceDict.ContainsKey("minzoom"))
                {
                    source.MinZoom = Convert.ToDouble(plainifyJson(sourceDict["minzoom"]));
                }

                if (sourceDict.ContainsKey("maxzoom"))
                {
                    source.MaxZoom = Convert.ToDouble(plainifyJson(sourceDict["maxzoom"]));
                }

                Sources[jSource.Name] = source;
            }

            int i = 0;
            foreach (var jLayer in jObject.layers)
            {
                var layer = new Layer();
                layer.Index = i;

                IDictionary<string, JToken> layerDict = jLayer;

                if (layerDict.ContainsKey("minzoom"))
                {
                    layer.MinZoom = Convert.ToDouble(plainifyJson(layerDict["minzoom"]));
                }

                if (layerDict.ContainsKey("maxzoom"))
                {
                    layer.MaxZoom = Convert.ToDouble(plainifyJson(layerDict["maxzoom"]));
                }

                if (layerDict.ContainsKey("id"))
                {
                    layer.ID = plainifyJson(layerDict["id"]) as string;
                }

                if (layerDict.ContainsKey("type"))
                {
                    layer.Type = plainifyJson(layerDict["type"]) as string;
                }

                if (layerDict.ContainsKey("source"))
                {
                    layer.SourceName = plainifyJson(layerDict["source"]) as string;
                    layer.Source = Sources[layer.SourceName];
                }

                if (layerDict.ContainsKey("source-layer"))
                {
                    layer.SourceLayer = plainifyJson(layerDict["source-layer"]) as string;
                }

                if (layerDict.ContainsKey("paint"))
                {
                    layer.Paint = plainifyJson(layerDict["paint"]) as Dictionary<string, object>;
                }

                if (layerDict.ContainsKey("layout"))
                {
                    layer.Layout = plainifyJson(layerDict["layout"]) as Dictionary<string, object>;
                }

                if (layerDict.ContainsKey("filter"))
                {
                    var filterArray = layerDict["filter"] as JArray;
                    layer.Filter = plainifyJson(filterArray) as object[];
                }

                Layers.Add(layer);

                i++;
            }

            Hash = Utils.Sha256(json);
        }


        public void SetSourceProvider(int index, Sources.ITileSource provider)
        {
            int i = 0;
            foreach (var pair in Sources)
            {
                if (index == i)
                {
                    pair.Value.Provider = provider;
                    return;
                }
                i++;
            }
        }

        public void SetSourceProvider(string name, Sources.ITileSource provider)
        {
            Sources[name].Provider = provider;
        }

        object plainifyJson(JToken token)
        {
            if (token.Type == JTokenType.Object)
            {
                IDictionary<string, JToken> dict = token as JObject;
                return dict.Select(pair => new KeyValuePair<string, object>(pair.Key, plainifyJson(pair.Value)))
                        .ToDictionary(key => key.Key, value => value.Value);
            }
            else if (token.Type == JTokenType.Array)
            {
                var array = token as JArray;
                return array.Select(item => plainifyJson(item)).ToArray();
            }
            else
            {
                return token.ToObject<object>();
            }

            return null;
        }

        public Brush[] GetStyleByType(string type, double zoom, double scale = 1)
        {
            List<Brush> results = new List<Brush>();

            int i = 0;
            foreach (var layer in Layers)
            {
                if (layer.Type == type)
                {
                    var attributes = new Dictionary<string, object>();
                    attributes["$type"] = "";
                    attributes["$id"] = "";
                    attributes["$zoom"] = zoom;

                    results.Add(ParseStyle(layer, scale, attributes));
                }
                i++;
            }

            return results.ToArray();
        }

        public Color GetBackgroundColor(double zoom)
        {
            var brushes = GetStyleByType("background", zoom, 1);

            foreach (var brush in brushes)
            {
                var newColor = Color.FromArgb((byte)Math.Max(0, Math.Min(255, brush.Paint.BackgroundOpacity * brush.Paint.BackgroundColor.A)), brush.Paint.BackgroundColor.R, brush.Paint.BackgroundColor.G, brush.Paint.BackgroundColor.B);
                return newColor;
            }

            return Colors.White;
        }

        //public Brush[] GetBrushesCached(double zoom, double scale, string type, string id, Dictionary<string, object> attributes)
        //{
        //    // check if the brush is cached or not
        //    // uses a cache key and stores brushes
        //    // 200ms saved on 3x3 512x512px tiles
        //    StringBuilder builder = new StringBuilder();

        //    builder.Append(zoom);
        //    builder.Append(',');
        //    builder.Append(scale);
        //    builder.Append(',');
        //    builder.Append(type);
        //    builder.Append(',');
        //    builder.Append(id);
        //    builder.Append(',');

        //    foreach(var attribute in attributes)
        //    {
        //        builder.Append(attribute.Key);
        //        builder.Append(';');
        //        builder.Append(attribute.Value);
        //        builder.Append('%');
        //    }

        //    var key = builder.ToString();

        //    if(brushesCache.ContainsKey(key))
        //    {
        //        return brushesCache[key];
        //    }

        //    var brushes = GetBrushes(zoom, scale, type, id, attributes);
        //    brushesCache[key] = brushes;

        //    return brushes;
        //}

        //public Brush[] GetBrushes(double zoom, double scale, string type, string id, Dictionary<string, object> attributes)
        //{
        //    attributes["$type"] = type;
        //    attributes["$id"] = id;
        //    attributes["$zoom"] = zoom;

        //    var layers = findLayers(zoom, attributes);

        //    Brush[] result = new Brush[layers.Count()];

        //    int i = 0;
        //    foreach(var layer in layers)
        //    {
        //        result[i] = ParseStyle(layer, scale, attributes);
        //        i++;
        //    }

        //    return result;
        //}

        public Brush ParseStyle(Layer layer, double scale, Dictionary<string, object> attributes)
        {
            var paintData = layer.Paint;
            var layoutData = layer.Layout;
            var index = layer.Index;

            var brush = new Brush();
            brush.ZIndex = index;
            brush._layer = layer;
            brush.GlyphsDirectory = this.FontDirectory;

            var paint = new Paint();
            brush.Paint = paint;

            if (layer.ID == "country_label")
            {

            }

            if (paintData != null)
            {
                // --

                if (paintData.ContainsKey("fill-color"))
                {
                    paint.FillColor = parseColor(getValue(paintData["fill-color"], attributes));
                }

                if (paintData.ContainsKey("background-color"))
                {
                    paint.BackgroundColor = parseColor(getValue(paintData["background-color"], attributes));
                }

                if (paintData.ContainsKey("text-color"))
                {
                    paint.TextColor = parseColor(getValue(paintData["text-color"], attributes));
                }

                if (paintData.ContainsKey("line-color"))
                {
                    paint.LineColor = parseColor(getValue(paintData["line-color"], attributes));
                }

                // --

                if (paintData.ContainsKey("line-pattern"))
                {
                    paint.LinePattern = (string)getValue(paintData["line-pattern"], attributes);
                }

                if (paintData.ContainsKey("background-pattern"))
                {
                    paint.BackgroundPattern = (string)getValue(paintData["background-pattern"], attributes);
                }

                if (paintData.ContainsKey("fill-pattern"))
                {
                    paint.FillPattern = (string)getValue(paintData["fill-pattern"], attributes);
                }

                // --

                if (paintData.ContainsKey("text-opacity"))
                {
                    paint.TextOpacity = Convert.ToDouble(getValue(paintData["text-opacity"], attributes));
                }

                if (paintData.ContainsKey("icon-opacity"))
                {
                    paint.IconOpacity = Convert.ToDouble(getValue(paintData["icon-opacity"], attributes));
                }

                if (paintData.ContainsKey("line-opacity"))
                {
                    paint.LineOpacity = Convert.ToDouble(getValue(paintData["line-opacity"], attributes));
                }

                if (paintData.ContainsKey("fill-opacity"))
                {
                    paint.FillOpacity = Convert.ToDouble(getValue(paintData["fill-opacity"], attributes));
                }

                if (paintData.ContainsKey("background-opacity"))
                {
                    paint.BackgroundOpacity = Convert.ToDouble(getValue(paintData["background-opacity"], attributes));
                }

                // --

                if (paintData.ContainsKey("line-width"))
                {
                    paint.LineWidth = Convert.ToDouble(getValue(paintData["line-width"], attributes)) * scale; // * screenScale;
                }

                if (paintData.ContainsKey("line-offset"))
                {
                    paint.LineOffset = Convert.ToDouble(getValue(paintData["line-offset"], attributes)) * scale;// * screenScale;
                }

                if (paintData.ContainsKey("line-dasharray"))
                {
                    var array = (getValue(paintData["line-dasharray"], attributes) as object[]);
                    paint.LineDashArray = array.Select(item => Convert.ToDouble(item) * scale).ToArray();
                }

                // --

                if (paintData.ContainsKey("text-halo-color"))
                {
                    paint.TextStrokeColor = parseColor(getValue(paintData["text-halo-color"], attributes));
                }

                if (paintData.ContainsKey("text-halo-width"))
                {
                    paint.TextStrokeWidth = Convert.ToDouble(getValue(paintData["text-halo-width"], attributes)) * scale;
                }

                if (paintData.ContainsKey("text-halo-blur"))
                {
                    paint.TextStrokeBlur = Convert.ToDouble(getValue(paintData["text-halo-blur"], attributes)) * scale;
                }

                // --

                //Console.WriteLine("paint");
                //Console.WriteLine(paintData.ToString());

                //foreach (var keyName in ((JObject)paintData).Properties().Select(p => p.Name))
                //{
                //    Console.WriteLine(keyName);
                //}
            }

            if (layoutData != null)
            {
                if (layoutData.ContainsKey("line-cap"))
                {
                    var value = (string)getValue(layoutData["line-cap"], attributes);
                    if (value == "butt")
                    {
                        paint.LineCap = PenLineCap.Flat;
                    }
                    else if (value == "round")
                    {
                        paint.LineCap = PenLineCap.Round;
                    }
                    else if (value == "square")
                    {
                        paint.LineCap = PenLineCap.Square;
                    }
                }

                if (layoutData.ContainsKey("visibility"))
                {
                    paint.Visibility = ((string)getValue(layoutData["visibility"], attributes)) == "visible";
                }

                if (layoutData.ContainsKey("text-field"))
                {
                    brush.TextField = (string)getValue(layoutData["text-field"], attributes);

                    // TODO check performance implications of Regex.Replace
                    brush.Text = Regex.Replace(brush.TextField, @"\{([A-Za-z0-9\-\:_]+)\}", (Match m) =>
                    {
                        var key = stripBraces(m.Value);
                        if (attributes.ContainsKey(key))
                        {
                            return attributes[key].ToString();
                        }

                        return "";
                    }).Trim();
                }

                if (layoutData.ContainsKey("text-font"))
                {
                    paint.TextFont = ((object[])getValue(layoutData["text-font"], attributes)).Select(item => (string)item).ToArray();
                }

                if (layoutData.ContainsKey("text-size"))
                {
                    paint.TextSize = Convert.ToDouble(getValue(layoutData["text-size"], attributes)) * scale;
                }

                if (layoutData.ContainsKey("text-max-width"))
                {
                    paint.TextMaxWidth = Convert.ToDouble(getValue(layoutData["text-max-width"], attributes)) * scale;// * screenScale;
                }

                if (layoutData.ContainsKey("text-offset"))
                {
                    var value = (object[])getValue(layoutData["text-offset"], attributes);
                    paint.TextOffset = new Point(Convert.ToDouble(value[0]) * scale, Convert.ToDouble(value[1]) * scale);
                }

                if (layoutData.ContainsKey("text-optional"))
                {
                    paint.TextOptional = (bool)(getValue(layoutData["text-optional"], attributes));
                }

                if (layoutData.ContainsKey("text-transform"))
                {
                    var value = (string)getValue(layoutData["text-transform"], attributes);
                    if (value == "none")
                    {
                        paint.TextTransform = TextTransform.None;
                    }
                    else if (value == "uppercase")
                    {
                        paint.TextTransform = TextTransform.Uppercase;
                    }
                    else if (value == "lowercase")
                    {
                        paint.TextTransform = TextTransform.Lowercase;
                    }
                }

                if (layoutData.ContainsKey("icon-size"))
                {
                    paint.IconScale = Convert.ToDouble(getValue(layoutData["icon-size"], attributes)) * scale;
                }

                if (layoutData.ContainsKey("icon-image"))
                {
                    paint.IconImage = (string)getValue(layoutData["icon-image"], attributes);
                }

                //Console.WriteLine("layout");
                //Console.WriteLine(layoutData.ToString());
            }

            return brush;
        }

        private unsafe string stripBraces(string s)
        {
            int len = s.Length;
            char* newChars = stackalloc char[len];
            char* currentChar = newChars;

            for (int i = 0; i < len; ++i)
            {
                char c = s[i];
                switch (c)
                {
                    case '{':
                    case '}':
                        continue;
                    default:
                        *currentChar++ = c;
                        break;
                }
            }
            return new string(newChars, 0, (int)(currentChar - newChars));
        }

        private Color parseColor(object iColor)
        {
            if (iColor.GetType() == typeof(Color))
            {
                return (Color)iColor;
            }

            if (iColor.GetType() != typeof(string))
            {

            }

            var colorString = (string)iColor;

            if (colorString[0] == '#')
            {
                return (Color)ColorConverter.ConvertFromString(colorString);
            }

            if (colorString.StartsWith("hsl("))
            {
                var segments = colorString.Replace('%', '\0').Split(',', '(', ')');
                double h = double.Parse(segments[1]);
                double s = double.Parse(segments[2]);
                double l = double.Parse(segments[3]);

                var color = (new ColorMine.ColorSpaces.Hsl()
                {
                    H = h,
                    S = s,
                    L = l,
                }).ToRgb();

                return Color.FromRgb((byte)color.R, (byte)color.G, (byte)color.B);
            }

            if (colorString.StartsWith("hsla("))
            {
                var segments = colorString.Replace('%', '\0').Split(',', '(', ')');
                double h = double.Parse(segments[1]);
                double s = double.Parse(segments[2]);
                double l = double.Parse(segments[3]);
                double a = double.Parse(segments[4]) * 255;

                var color = (new ColorMine.ColorSpaces.Hsl()
                {
                    H = h,
                    S = s,
                    L = l,
                }).ToRgb();

                return Color.FromArgb((byte)(a), (byte)color.R, (byte)color.G, (byte)color.B);
            }

            if (colorString.StartsWith("rgba("))
            {
                var segments = colorString.Replace('%', '\0').Split(',', '(', ')');
                double r = double.Parse(segments[1]);
                double g = double.Parse(segments[2]);
                double b = double.Parse(segments[3]);
                double a = double.Parse(segments[4]) * 255;

                return Color.FromArgb((byte)a, (byte)r, (byte)g, (byte)b);
            }

            if (colorString.StartsWith("rgb("))
            {
                var segments = colorString.Replace('%', '\0').Split(',', '(', ')');
                double r = double.Parse(segments[1]);
                double g = double.Parse(segments[2]);
                double b = double.Parse(segments[3]);

                return Color.FromRgb((byte)r, (byte)g, (byte)b);
            }

            try
            {
                return (Color)ColorConverter.ConvertFromString(colorString);
            }
            catch (Exception e)
            {
                throw new NotImplementedException("Not implemented color format: " + colorString);
            }
            //return Colors.Violet;
        }

        public bool ValidateLayer(Layer layer, double zoom, Dictionary<string, object> attributes)
        {
            if (layer.MinZoom != null)
            {
                if (zoom < layer.MinZoom.Value)
                {
                    return false;
                }
            }

            if (layer.MaxZoom != null)
            {
                if (zoom > layer.MaxZoom.Value)
                {
                    return false;
                }
            }

            if (attributes != null && layer.Filter.Count() > 0)
            {
                // TODO make this more performant
                if (!validateUsingFilter(layer.Filter, attributes))
                {
                    return false;
                }
            }

            return true;
        }

        private Layer[] findLayers(double zoom, string layerName, Dictionary<string, object> attributes)
        {
            ////Console.WriteLine(layerName);
            List<Layer> result = new List<Layer>();

            foreach (var layer in Layers)
            {
                //if (attributes.ContainsKey("class"))
                //{
                //    if (id == "highway-trunk" && (string)attributes["class"] == "primary")
                //    {

                //    }
                //}

                if (layer.SourceLayer == layerName)
                {
                    bool valid = true;

                    if (layer.Filter.Count() > 0)
                    {
                        if (!validateUsingFilter(layer.Filter, attributes))
                        {
                            valid = false;
                        }
                    }

                    if (layer.MinZoom != null)
                    {
                        if (zoom < layer.MinZoom.Value)
                        {
                            valid = false;
                        }
                    }

                    if (layer.MaxZoom != null)
                    {
                        if (zoom > layer.MaxZoom.Value)
                        {
                            valid = false;
                        }
                    }

                    if (valid)
                    {
                        //return layer;
                        result.Add(layer);
                    }
                }
            }

            return result.ToArray();
        }

        private bool validateUsingFilter(object[] filterArray, Dictionary<string, object> attributes)
        {
            if (filterArray.Count() == 0)
            {
            }
            var operation = filterArray[0] as string;
            bool result;

            if (operation == "all")
            {
                foreach (object[] subFilter in filterArray.Skip(1))
                {
                    if (!validateUsingFilter(subFilter, attributes))
                    {
                        return false;
                    }
                }
                return true;
            }
            else if (operation == "any")
            {
                foreach (object[] subFilter in filterArray.Skip(1))
                {
                    if (validateUsingFilter(subFilter, attributes))
                    {
                        return true;
                    }
                }
                return false;
            }
            else if (operation == "none")
            {
                result = false;
                foreach (object[] subFilter in filterArray.Skip(1))
                {
                    if (validateUsingFilter(subFilter, attributes))
                    {
                        result = true;
                    }
                }
                return !result;
            }

            switch (operation)
            {
                case "==":
                case "!=":
                case ">":
                case ">=":
                case "<":
                case "<=":

                    var key = (string)filterArray[1];

                    if (operation == "==")
                    {
                        if (!attributes.ContainsKey(key))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        // special case, comparing inequality with non existent attribute
                        if (!attributes.ContainsKey(key))
                        {
                            return true;
                        }
                    }

                    if (!(attributes[key] is IComparable))
                    {
                        throw new NotImplementedException("Comparing colors probably");
                        return false;
                    }

                    var valueA = (IComparable)attributes[key];
                    var valueB = getValue(filterArray[2], attributes);

                    if (isNumber(valueA) && isNumber(valueB))
                    {
                        valueA = Convert.ToDouble(valueA);
                        valueB = Convert.ToDouble(valueB);
                    }

                    if (key is string)
                    {
                        if (key == "capital")
                        {

                        }
                    }

                    if (valueA.GetType() != valueB.GetType())
                    {
                        return false;
                    }

                    var comparison = valueA.CompareTo(valueB);

                    if (operation == "==")
                    {
                        return comparison == 0;
                    }
                    else if (operation == "!=")
                    {
                        return comparison != 0;
                    }
                    else if (operation == ">")
                    {
                        return comparison > 0;
                    }
                    else if (operation == "<")
                    {
                        return comparison < 0;
                    }
                    else if (operation == ">=")
                    {
                        return comparison >= 0;
                    }
                    else if (operation == "<=")
                    {
                        return comparison <= 0;
                    }

                    break;
            }

            if (operation == "has")
            {
                return attributes.ContainsKey(filterArray[1] as string);
            }
            else if (operation == "!has")
            {
                return !attributes.ContainsKey(filterArray[1] as string);
            }


            if (operation == "in")
            {
                var key = filterArray[1] as string;
                if (!attributes.ContainsKey(key))
                {
                    return false;
                }

                var value = attributes[key];

                foreach (object item in filterArray.Skip(2))
                {
                    if (getValue(item, attributes).Equals(value))
                    {
                        return true;
                    }
                }
                return false;
            }
            else if (operation == "!in")
            {
                var key = filterArray[1] as string;
                if (!attributes.ContainsKey(key))
                {
                    return true;
                }

                var value = attributes[key];

                foreach (object item in filterArray.Skip(2))
                {
                    if (getValue(item, attributes).Equals(value))
                    {
                        return false;
                    }
                }
                return true;
            }

            return false;
        }

        object getValue(object token, Dictionary<string, object> attributes = null)
        {

            if (token is string && attributes != null)
            {
                string value = token as string;
                if (value.Length == 0)
                {
                    return "";
                }
                if (value[0] == '$')
                {
                    return getValue(attributes[value]);
                }
            }

            if (token.GetType().IsArray)
            {
                var array = token as object[];
                //List<object> result = new List<object>();

                //foreach (object item in array)
                //{
                //    var obj = getValue(item, attributes);
                //    result.Add(obj);
                //}

                //return result.ToArray();

                return array.Select(item => getValue(item, attributes)).ToArray();
            }
            else if (token is Dictionary<string, object>)
            {
                var dict = token as Dictionary<string, object>;
                if (dict.ContainsKey("stops"))
                {
                    var stops = dict["stops"] as object[];
                    // if it has stops, it's interpolation domain now :P
                    //var pointStops = stops.Select(item => new Tuple<double, JToken>((item as JArray)[0].Value<double>(), (item as JArray)[1])).ToList();
                    var pointStops = stops.Select(item => new Tuple<double, object>(Convert.ToDouble((item as object[])[0]), (item as object[])[1])).ToList();

                    var zoom = (double)attributes["$zoom"];
                    var minZoom = pointStops.First().Item1;
                    var maxZoom = pointStops.Last().Item1;
                    double power = 1;

                    if (minZoom == 5 && maxZoom == 10)
                    {

                    }

                    double zoomA = minZoom;
                    double zoomB = maxZoom;
                    int zoomAIndex = 0;
                    int zoomBIndex = pointStops.Count() - 1;

                    // get min max zoom bounds from array
                    if (zoom <= minZoom)
                    {
                        //zoomA = minZoom;
                        //zoomB = pointStops[1].Item1;
                        return pointStops.First().Item2;
                    }
                    else if (zoom >= maxZoom)
                    {
                        //zoomA = pointStops[pointStops.Count - 2].Item1;
                        //zoomB = maxZoom;
                        return pointStops.Last().Item2;
                    }
                    else
                    {
                        // checking for consecutive values
                        for (int i = 1; i < pointStops.Count(); i++)
                        {
                            var previousZoom = pointStops[i - 1].Item1;
                            var thisZoom = pointStops[i].Item1;

                            if (zoom >= previousZoom && zoom <= thisZoom)
                            {
                                zoomA = previousZoom;
                                zoomB = thisZoom;

                                zoomAIndex = i - 1;
                                zoomBIndex = i;
                                break;
                            }
                        }
                    }


                    if (dict.ContainsKey("base"))
                    {
                        power = Convert.ToDouble(getValue(dict["base"], attributes));
                    }

                    //var referenceElement = (stops[0] as object[])[1];

                    return interpolateValues(pointStops[zoomAIndex].Item2, pointStops[zoomBIndex].Item2, zoomA, zoomB, zoom, power, false);

                }
            }


            //if (token is string)
            //{
            //    return token as string;
            //}
            //else if (token is bool)
            //{
            //    return (bool)token;
            //}
            //else if (token is float)
            //{
            //    return token as float;
            //}
            //else if (token.Type == JTokenType.Integer)
            //{
            //    return token.Value<int>();
            //}
            //else if (token.Type == JTokenType.None || token.Type == JTokenType.Null)
            //{
            //    return null;
            //}


            return token;
        }

        bool isNumber(object value)
        {
            return value is sbyte
                    || value is byte
                    || value is short
                    || value is ushort
                    || value is int
                    || value is uint
                    || value is long
                    || value is ulong
                    || value is float
                    || value is double
                    || value is decimal;
        }

        private object interpolateValues(object startValue, object endValue, double zoomA, double zoomB, double zoom, double power, bool clamp = false)
        {
            if (startValue is string)
            {
                // TODO implement color mappings
                //var minValue = parseColor(startValue.Value<string>());
                //var maxValue = parseColor(endValue.Value<string>());


                //var newR = convertRange(zoom, zoomA, zoomB, minValue.ScR, maxValue.ScR, power, false);
                //var newG = convertRange(zoom, zoomA, zoomB, minValue.ScG, maxValue.ScG, power, false);
                //var newB = convertRange(zoom, zoomA, zoomB, minValue.ScB, maxValue.ScB, power, false);
                //var newA = convertRange(zoom, zoomA, zoomB, minValue.ScA, maxValue.ScA, power, false);

                //return Color.FromScRgb((float)newA, (float)newR, (float)newG, (float)newB);

                var minValue = startValue as string;
                var maxValue = endValue as string;

                if (Math.Abs(zoomA - zoom) <= Math.Abs(zoomB - zoom))
                {
                    return minValue;
                }
                else
                {
                    return maxValue;
                }

            }
            else if (startValue.GetType().IsArray)
            {
                List<object> result = new List<object>();
                var startArray = startValue as object[];
                var endArray = endValue as object[];

                for (int i = 0; i < startArray.Count(); i++)
                {
                    var minValue = startArray[i];
                    var maxValue = endArray[i];

                    var value = interpolateValues(minValue, maxValue, zoomA, zoomB, zoom, power, clamp);

                    result.Add(value);
                }

                return result.ToArray();
            }
            else if (isNumber(startValue))
            {
                var minValue = Convert.ToDouble(startValue);
                var maxValue = Convert.ToDouble(endValue);

                return interpolateRange(zoom, zoomA, zoomB, minValue, maxValue, power, clamp);
            }
            else
            {
                throw new NotImplementedException("Unimplemented interpolation");
            }
        }

        private double interpolateRange(double oldValue, double oldMin, double oldMax, double newMin, double newMax, double power, bool clamp = false)
        {
            double difference = oldMax - oldMin;
            double progress = oldValue - oldMin;

            double normalized = 0;

            if (difference == 0)
            {
                normalized = 0;
            }
            else if (power == 1)
            {
                normalized = progress / difference;
            }
            else
            {
                normalized = (Math.Pow(power, progress) - 1f) / (Math.Pow(power, difference) - 1f);
            }

            var result = (normalized * (newMax - newMin)) + newMin;


            return result;
        }
    }
}
