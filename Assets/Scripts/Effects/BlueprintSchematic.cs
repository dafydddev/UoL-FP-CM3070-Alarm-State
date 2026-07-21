using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Effects
{
    // A facility floor-plan schematic that drifts slowly behind the black menu:
    // rooms of varying sizes linked by corridors over a faint grid, in blueprint blue.
    [RequireComponent(typeof(RawImage))]
    public class BlueprintSchematic : MonoBehaviour
    {
        // Room and corridor lines, in schematic pixels.
        private const int LineThickness = 1;

        [Header("Resolution")]
        // Canvas units per schematic pixel. The canvas base is 640x360, so 1 draws the plan on the UI grid.
        [SerializeField, Range(1, 8)]
        private int pixelSize = 1;

        [Header("Layout")]
        // How many rooms to try to place.
        [SerializeField]
        private int roomCount = 14;

        // Room width and height as a fraction of the texture, picked at random per room.
        [SerializeField, Range(0.02f, 0.4f)] private float minRoomSize = 0.05f;
        [SerializeField, Range(0.02f, 0.4f)] private float maxRoomSize = 0.18f;

        // Minimum gap kept between rooms, as a fraction of the texture width.
        [SerializeField, Range(0f, 0.1f)] private float roomSpacing = 0.02f;

        [Header("Look")] [SerializeField] private Color lineColor = new Color(0.35f, 0.6f, 0.95f);

        // Opacity of the fine background grid.
        [SerializeField, Range(0f, 1f)] private float gridOpacity = 0.1f;

        // Opacity of the room and corridor lines.
        [SerializeField, Range(0f, 1f)] private float planOpacity = 0.45f;

        // Spacing of the fine background grid, in schematic pixels.
        // Snapped at build time to a spacing that divides the texture, so the grid keeps its rhythm across the wrap seam.
        [SerializeField] private int gridSpacing = 20;

        [Header("Motion")]
        // Schematic pixels drifted per second; the plan wraps as it scrolls.
        // Per-pixel rather than per-texture, so the drift keeps its speed and its angle.
        [SerializeField]
        private Vector2 driftSpeed = new(8f, 3f);

        private RawImage _image;
        private RectTransform _rect;
        private Vector2 _scroll;
        private Vector2Int _size;

        private void Awake()
        {
            _image = GetComponent<RawImage>();
            _rect = (RectTransform)transform;
            _image.color = Color.white;
            _image.uvRect = new Rect(0f, 0f, 1f, 1f);
        }

        // Forcing the update first means the plan is always built against 640x360 rather than whatever frame one held.
        private void Start()
        {
            Canvas.ForceUpdateCanvases();
            _size = Vector2Int.Max(Vector2Int.one, Vector2Int.RoundToInt(_rect.rect.size / pixelSize));
            _image.texture = BuildSchematic(_size.x, _size.y);
        }

        private void Update()
        {
            // Scroll the UV rect; Repeat wrapping makes the drift loop seamlessly.
            _scroll += driftSpeed * Time.unscaledDeltaTime;
            _scroll.x %= _size.x; // wrapped, or the accumulator loses precision over a long sit on the menu
            _scroll.y %= _size.y;
            _image.uvRect = new Rect(_scroll.x / _size.x, _scroll.y / _size.y, 1f, 1f);
        }

        private void OnDestroy()
        {
            if (_image && _image.texture) Destroy(_image.texture);
        }

        private Texture2D BuildSchematic(int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat
            };

            // Transparent everywhere by default, so the black background shows through gaps.
            var pixels = new Color[width * height];

            var grid = new Color(lineColor.r, lineColor.g, lineColor.b, gridOpacity);
            var plan = new Color(lineColor.r, lineColor.g, lineColor.b, planOpacity);

            // A spacing that divides both dimensions,
            // so the cells stay square and the row either side of the wrap seam matches the rest of the grid.
            var step = NearestDivisor(Gcd(width, height), gridSpacing);

            // Faint background grid, drawn first so the heavier plan lines sit on top.
            for (var x = 0; x < width; x += step) VLine(pixels, width, height, x, 0, height - 1, 1, grid);
            for (var y = 0; y < height; y += step) HLine(pixels, width, height, y, 0, width - 1, 1, grid);

            // Place rooms at random sizes and positions by rejection sampling,
            // so the layout varies every time and never settles into a repeating lattice.
            // Rooms stay clear of the texture edges, so the drifting plan still tiles seamlessly.
            var rooms = new List<RectInt>();
            var pad = Mathf.RoundToInt(roomSpacing * width);

            for (var attempt = 0; attempt < roomCount * 30 && rooms.Count < roomCount; attempt++)
            {
                var rw = Mathf.RoundToInt(Random.Range(minRoomSize, maxRoomSize) * width);
                var rh = Mathf.RoundToInt(Random.Range(minRoomSize, maxRoomSize) * height);
                if (rw > width - 2 * step || rh > height - 2 * step) continue;
                var rx = Random.Range(step, width - step - rw + 1);
                var ry = Random.Range(step, height - step - rh + 1);
                var candidate = new RectInt(rx, ry, rw, rh);
                var clear = rooms.All(room => !Overlaps(candidate, room, pad));
                if (!clear) continue;
                rooms.Add(candidate);
            }

            // Draw the room walls.
            foreach (var room in rooms)
            {
                RectOutline(pixels, width, height, room.xMin, room.yMin, room.xMax, room.yMax, LineThickness, plan);
            }

            // Connect each room to its nearest earlier room.
            // Corridors are clipped to room walls, so they meet the outlines instead of driving into the centres.
            for (var i = 1; i < rooms.Count; i++)
            {
                var a = Centre(rooms[i]);
                var nearest = rooms[0];
                var best = float.MaxValue;
                for (var j = 0; j < i; j++)
                {
                    var d = Vector2.Distance(Centre(rooms[j]), a);
                    if (!(d < best)) continue;
                    best = d;
                    nearest = rooms[j];
                }

                Corridor(pixels, width, height, a, Centre(nearest), rooms, LineThickness, plan);
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        // Nearest divisor of length to the desired spacing.
        private static int NearestDivisor(int length, int desired)
        {
            desired = Mathf.Max(1, desired);
            var best = 0;
            for (var d = 1; d <= length; d++)
            {
                if (length % d != 0) continue;
                if (best == 0 || Mathf.Abs(d - desired) < Mathf.Abs(best - desired)) best = d;
            }

            return best >= desired / 2 && best <= desired * 2 ? best : desired;
        }

        private static int Gcd(int a, int b)
        {
            while (b != 0) (a, b) = (b, a % b);
            return a;
        }

        // True if two rectangles overlap once expanded by pad.
        private static bool Overlaps(RectInt a, RectInt b, int pad)
        {
            return a.xMin - pad < b.xMax && a.xMax + pad > b.xMin && a.yMin - pad < b.yMax && a.yMax + pad > b.yMin;
        }

        // Centre point of a rectangle.
        private static Vector2Int Centre(RectInt r) => new(r.xMin + r.width / 2, r.yMin + r.height / 2);

        // True if the point lies strictly inside any room (not on its wall).
        private static bool InsideAnyRoom(List<RectInt> rooms, int x, int y)
        {
            return rooms.Any(r => x > r.xMin && x < r.xMax && y > r.yMin && y < r.yMax);
        }

        // An L-shaped corridor from a to b, skipping any pixel inside a room,
        // so it stops at the walls rather than running through to the centres.
        private static void Corridor(Color[] px, int w, int h, Vector2Int a, Vector2Int b, List<RectInt> rooms,
            int t, Color col)
        {
            HRun(px, w, h, a.y, Mathf.Min(a.x, b.x), Mathf.Max(a.x, b.x), t, rooms, col);
            VRun(px, w, h, b.x, Mathf.Min(a.y, b.y), Mathf.Max(a.y, b.y), t, rooms, col);
        }

        // Horizontal corridor run that skips pixels inside rooms.
        private static void HRun(Color[] px, int w, int h, int y, int xStart, int xEnd, int t, List<RectInt> rooms,
            Color col)
        {
            for (var ty = 0; ty < t; ty++)
            {
                var yy = y + ty;
                if (yy < 0 || yy >= h) continue;
                for (var x = xStart; x <= xEnd; x++)
                {
                    if (x < 0 || x >= w || InsideAnyRoom(rooms, x, yy)) continue;
                    px[yy * w + x] = col;
                }
            }
        }

        // Vertical corridor run that skips pixels inside rooms.
        private static void VRun(Color[] px, int w, int h, int x, int yStart, int yEnd, int t, List<RectInt> rooms,
            Color col)
        {
            for (var tx = 0; tx < t; tx++)
            {
                var xx = x + tx;
                if (xx < 0 || xx >= w) continue;
                for (var y = yStart; y <= yEnd; y++)
                {
                    if (y < 0 || y >= h || InsideAnyRoom(rooms, xx, y)) continue;
                    px[y * w + xx] = col;
                }
            }
        }

        // Draw the four edges of a rectangle.
        private static void RectOutline(Color[] px, int w, int h, int x0, int y0, int x1, int y1, int t, Color col)
        {
            HLine(px, w, h, y0, x0, x1, t, col);
            HLine(px, w, h, y1, x0, x1, t, col);
            VLine(px, w, h, x0, y0, y1, t, col);
            VLine(px, w, h, x1, y0, y1, t, col);
        }

        // Horizontal run at row y from xStart to xEnd, t pixels thick.
        private static void HLine(Color[] px, int w, int h, int y, int xStart, int xEnd, int t, Color col)
        {
            for (var ty = 0; ty < t; ty++)
            {
                var yy = y + ty;
                if (yy < 0 || yy >= h) continue;
                for (var x = xStart; x <= xEnd; x++)
                {
                    if (x >= 0 && x < w) px[yy * w + x] = col;
                }
            }
        }

        // Vertical run at column x from yStart to yEnd, t pixels thick.
        private static void VLine(Color[] px, int w, int h, int x, int yStart, int yEnd, int t, Color col)
        {
            for (var tx = 0; tx < t; tx++)
            {
                var xx = x + tx;
                if (xx < 0 || xx >= w) continue;
                for (var y = yStart; y <= yEnd; y++)
                {
                    if (y >= 0 && y < h) px[y * w + xx] = col;
                }
            }
        }
    }
}
