using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ScreenshotTool
{
    #region Enums

    enum DrawTool { None, Rect, Ellipse, Arrow, Pen, Text, Highlight, Mosaic }

    enum HandlePos { None, TL, T, TR, R, BR, B, BL, L }

    enum OverlayState { Waiting, Selecting, Selected, Moving, Resizing, Drawing, TextInput,
        AnnotationDragging, AnnotationResizing, AnnotationEditing }

    #endregion

    #region Annotation Classes

    abstract class Annotation
    {
        public Color Color = Color.Red;
        public int PenWidth = 2;
        public abstract void Draw(Graphics g, Bitmap src);

        // 标注选择/拖动/调整接口
        public virtual Rectangle GetBounds() { return Rectangle.Empty; }
        public virtual void Move(int dx, int dy) { }
        public virtual bool HasHandles { get { return false; } }
        public virtual Rectangle[] GetHandles() { return new Rectangle[0]; }
        public virtual int HitTestHandle(Point p) { return -1; }
        public virtual void ResizeHandle(int handleIndex, Point p) { }
        public virtual bool HitTest(Point p) { return GetBounds().Contains(p); }
    }

    class RectAnnotation : Annotation
    {
        public Rectangle Rect;
        public override void Draw(Graphics g, Bitmap src)
        {
            if (Rect.Width == 0 || Rect.Height == 0) return;
            using (Pen p = new Pen(Color, PenWidth))
                g.DrawRectangle(p, Rect);
        }
        public override Rectangle GetBounds() { return Rect; }
        public override void Move(int dx, int dy) { Rect = new Rectangle(Rect.X + dx, Rect.Y + dy, Rect.Width, Rect.Height); }
        public override bool HasHandles { get { return true; } }
        public override Rectangle[] GetHandles()
        {
            int hs = 8, half = hs / 2;
            Rectangle s = Rect;
            return new Rectangle[] {
                new Rectangle(s.X - half, s.Y - half, hs, hs),
                new Rectangle(s.X + s.Width / 2 - half, s.Y - half, hs, hs),
                new Rectangle(s.Right - half, s.Y - half, hs, hs),
                new Rectangle(s.Right - half, s.Y + s.Height / 2 - half, hs, hs),
                new Rectangle(s.Right - half, s.Bottom - half, hs, hs),
                new Rectangle(s.X + s.Width / 2 - half, s.Bottom - half, hs, hs),
                new Rectangle(s.X - half, s.Bottom - half, hs, hs),
                new Rectangle(s.X - half, s.Y + s.Height / 2 - half, hs, hs),
            };
        }
        public override int HitTestHandle(Point p)
        {
            Rectangle[] handles = GetHandles();
            for (int i = 0; i < handles.Length; i++)
            {
                Rectangle hit = handles[i];
                hit.Inflate(5, 5);
                if (hit.Contains(p)) return i;
            }
            return -1;
        }
        public override void ResizeHandle(int handleIndex, Point p)
        {
            int left = Rect.X, top = Rect.Y, right = Rect.Right, bottom = Rect.Bottom;
            switch (handleIndex)
            {
                case 0: left = p.X; top = p.Y; break;
                case 1: top = p.Y; break;
                case 2: right = p.X; top = p.Y; break;
                case 3: right = p.X; break;
                case 4: right = p.X; bottom = p.Y; break;
                case 5: bottom = p.Y; break;
                case 6: left = p.X; bottom = p.Y; break;
                case 7: left = p.X; break;
            }
            if (right - left < 5) right = left + 5;
            if (bottom - top < 5) bottom = top + 5;
            Rect = Rectangle.FromLTRB(Math.Min(left, right), Math.Min(top, bottom), Math.Max(left, right), Math.Max(top, bottom));
        }
    }

    class EllipseAnnotation : Annotation
    {
        public Rectangle Rect;
        public override void Draw(Graphics g, Bitmap src)
        {
            if (Rect.Width == 0 || Rect.Height == 0) return;
            using (Pen p = new Pen(Color, PenWidth))
                g.DrawEllipse(p, Rect);
        }
        public override Rectangle GetBounds() { return Rect; }
        public override void Move(int dx, int dy) { Rect = new Rectangle(Rect.X + dx, Rect.Y + dy, Rect.Width, Rect.Height); }
        public override bool HasHandles { get { return true; } }
        public override Rectangle[] GetHandles()
        {
            int hs = 8, half = hs / 2;
            Rectangle s = Rect;
            return new Rectangle[] {
                new Rectangle(s.X - half, s.Y - half, hs, hs),
                new Rectangle(s.X + s.Width / 2 - half, s.Y - half, hs, hs),
                new Rectangle(s.Right - half, s.Y - half, hs, hs),
                new Rectangle(s.Right - half, s.Y + s.Height / 2 - half, hs, hs),
                new Rectangle(s.Right - half, s.Bottom - half, hs, hs),
                new Rectangle(s.X + s.Width / 2 - half, s.Bottom - half, hs, hs),
                new Rectangle(s.X - half, s.Bottom - half, hs, hs),
                new Rectangle(s.X - half, s.Y + s.Height / 2 - half, hs, hs),
            };
        }
        public override int HitTestHandle(Point p)
        {
            Rectangle[] handles = GetHandles();
            for (int i = 0; i < handles.Length; i++)
            {
                Rectangle hit = handles[i];
                hit.Inflate(5, 5);
                if (hit.Contains(p)) return i;
            }
            return -1;
        }
        public override void ResizeHandle(int handleIndex, Point p)
        {
            int left = Rect.X, top = Rect.Y, right = Rect.Right, bottom = Rect.Bottom;
            switch (handleIndex)
            {
                case 0: left = p.X; top = p.Y; break;
                case 1: top = p.Y; break;
                case 2: right = p.X; top = p.Y; break;
                case 3: right = p.X; break;
                case 4: right = p.X; bottom = p.Y; break;
                case 5: bottom = p.Y; break;
                case 6: left = p.X; bottom = p.Y; break;
                case 7: left = p.X; break;
            }
            if (right - left < 5) right = left + 5;
            if (bottom - top < 5) bottom = top + 5;
            Rect = Rectangle.FromLTRB(Math.Min(left, right), Math.Min(top, bottom), Math.Max(left, right), Math.Max(top, bottom));
        }
    }

    class ArrowAnnotation : Annotation
    {
        public Point Start;
        public Point End;

        // 箭头头部参数
        private const float MaxArrowLength = 500f; // 超过此长度后箭头头部不再放大
        private const float MinHeadSize = 10f;     // 最小头部大小
        private const float MaxHeadSize = 30f;     // 最大头部大小（长度=300px时）
        private const float HeadHalfAngle = 0.4f;  // 箭头半角（弧度，约23度）

        public override void Draw(Graphics g, Bitmap src)
        {
            if (Start == End) return;

            float dx = End.X - Start.X;
            float dy = End.Y - Start.Y;
            float length = (float)Math.Sqrt(dx * dx + dy * dy);
            if (length < 1) return;

            // 箭头方向单位向量
            float ux = dx / length;
            float uy = dy / length;
            // 垂直方向单位向量
            float vx = -uy;
            float vy = ux;

            // 计算箭头头部大小：长度0~300px等比放大，300px后封顶
            float scaleFactor = Math.Min(length, MaxArrowLength) / MaxArrowLength;
            float headLen = MinHeadSize + (MaxHeadSize - MinHeadSize) * scaleFactor;

            // 箭头头部不超过总长度的1/3
            headLen = Math.Min(headLen, length * 0.33f);
            if (headLen < 3) headLen = 3;

            // 箭头头部两翼端点
            float headBase = headLen * (float)Math.Tan(HeadHalfAngle);

            // 拖尾宽度：在箭头底部位置，宽度为箭头底部宽度的1/3
            float tailWidthAtHead = headBase * 2f / 3f;

            // 箭头底部中心点
            PointF headBaseCenter = new PointF(End.X - ux * headLen, End.Y - uy * headLen);

            // 箭头头部三角形
            PointF arrowTip = new PointF(End.X, End.Y);
            PointF wing1 = new PointF(
                End.X - ux * headLen - uy * headBase,
                End.Y - uy * headLen + ux * headBase);
            PointF wing2 = new PointF(
                End.X - ux * headLen + uy * headBase,
                End.Y - uy * headLen - ux * headBase);

            // 拖尾：从起始点(宽度0)到箭头底部(宽度tailWidthAtHead)的锥形
            PointF tailSide1 = new PointF(
                headBaseCenter.X + vx * tailWidthAtHead / 2f,
                headBaseCenter.Y + vy * tailWidthAtHead / 2f);
            PointF tailSide2 = new PointF(
                headBaseCenter.X - vx * tailWidthAtHead / 2f,
                headBaseCenter.Y - vy * tailWidthAtHead / 2f);

            // 画拖尾（填充三角形：Start尖端 → tailSide1 → tailSide2）
            using (SolidBrush brush = new SolidBrush(Color))
            using (GraphicsPath tailPath = new GraphicsPath())
            {
                tailPath.AddLine(new PointF(Start.X, Start.Y), tailSide1);
                tailPath.AddLine(tailSide1, tailSide2);
                tailPath.CloseFigure();
                g.FillPath(brush, tailPath);
            }

            // 画箭头头部（填充三角形，覆盖拖尾末端）
            using (SolidBrush brush2 = new SolidBrush(Color))
            using (GraphicsPath headPath = new GraphicsPath())
            {
                headPath.AddLine(arrowTip, wing1);
                headPath.AddLine(wing1, wing2);
                headPath.CloseFigure();
                g.FillPath(brush2, headPath);
            }
        }
        public override Rectangle GetBounds()
        {
            int x = Math.Min(Start.X, End.X);
            int y = Math.Min(Start.Y, End.Y);
            return new Rectangle(x, y, Math.Abs(End.X - Start.X), Math.Abs(End.Y - Start.Y));
        }
        public override void Move(int dx, int dy)
        {
            Start = new Point(Start.X + dx, Start.Y + dy);
            End = new Point(End.X + dx, End.Y + dy);
        }
        public override bool HitTest(Point p)
        {
            // 点到线段距离 < 8px
            return DistToSegment(p, Start, End) < 8;
        }
        static double DistToSegment(Point p, Point a, Point b)
        {
            double ddx = b.X - a.X, ddy = b.Y - a.Y;
            double len2 = ddx * ddx + ddy * ddy;
            if (len2 == 0) return Math.Sqrt((p.X - a.X) * (p.X - a.X) + (p.Y - a.Y) * (p.Y - a.Y));
            double t = Math.Max(0, Math.Min(1, ((p.X - a.X) * ddx + (p.Y - a.Y) * ddy) / len2));
            double projX = a.X + t * ddx, projY = a.Y + t * ddy;
            return Math.Sqrt((p.X - projX) * (p.X - projX) + (p.Y - projY) * (p.Y - projY));
        }
    }

    class PenAnnotation : Annotation
    {
        public List<Point> Points = new List<Point>();
        public override void Draw(Graphics g, Bitmap src)
        {
            if (Points.Count < 2) return;
            using (Pen p = new Pen(Color, PenWidth))
            {
                p.LineJoin = LineJoin.Round;
                p.StartCap = LineCap.Round;
                p.EndCap = LineCap.Round;
                g.DrawLines(p, Points.ToArray());
            }
        }
        public override Rectangle GetBounds()
        {
            if (Points.Count == 0) return Rectangle.Empty;
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            foreach (Point pt in Points)
            {
                if (pt.X < minX) minX = pt.X;
                if (pt.Y < minY) minY = pt.Y;
                if (pt.X > maxX) maxX = pt.X;
                if (pt.Y > maxY) maxY = pt.Y;
            }
            return new Rectangle(minX, minY, maxX - minX, maxY - minY);
        }
        public override void Move(int dx, int dy)
        {
            for (int i = 0; i < Points.Count; i++)
                Points[i] = new Point(Points[i].X + dx, Points[i].Y + dy);
        }
        public override bool HitTest(Point p)
        {
            for (int i = 1; i < Points.Count; i++)
            {
                if (DistToSeg(p, Points[i - 1], Points[i]) < 8)
                    return true;
            }
            return false;
        }
        static double DistToSeg(Point p, Point a, Point b)
        {
            double dx = b.X - a.X, dy = b.Y - a.Y;
            double len2 = dx * dx + dy * dy;
            if (len2 == 0) return Math.Sqrt((p.X - a.X) * (p.X - a.X) + (p.Y - a.Y) * (p.Y - a.Y));
            double t = Math.Max(0, Math.Min(1, ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / len2));
            double projX = a.X + t * dx, projY = a.Y + t * dy;
            return Math.Sqrt((p.X - projX) * (p.X - projX) + (p.Y - projY) * (p.Y - projY));
        }
    }

    class TextAnnotation : Annotation
    {
        public Point Position;
        public string Text = "";
        public Font Font;
        private Size _textSize = Size.Empty;

        public TextAnnotation(Font font)
        {
            Font = (Font)font.Clone();
        }

        public void UpdateTextSize(Graphics g)
        {
            if (string.IsNullOrEmpty(Text)) { _textSize = Size.Empty; return; }
            _textSize = g.MeasureString(Text, Font).ToSize();
            _textSize.Width += 4;
            _textSize.Height += 4;
        }

        public override void Draw(Graphics g, Bitmap src)
        {
            if (string.IsNullOrEmpty(Text)) return;
            UpdateTextSize(g);
            // Shadow
            using (SolidBrush shadow = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
                g.DrawString(Text, Font, shadow, Position.X + 1, Position.Y + 1);
            // Main text
            using (SolidBrush b = new SolidBrush(Color))
                g.DrawString(Text, Font, b, Position);
        }
        public override Rectangle GetBounds()
        {
            return new Rectangle(Position, _textSize);
        }
        public override void Move(int dx, int dy)
        {
            Position = new Point(Position.X + dx, Position.Y + dy);
        }
        public override bool HasHandles { get { return true; } }
        public override Rectangle[] GetHandles()
        {
            int hs = 8, half = hs / 2;
            Rectangle s = GetBounds();
            return new Rectangle[] {
                new Rectangle(s.X - half, s.Y - half, hs, hs),
                new Rectangle(s.X + s.Width / 2 - half, s.Y - half, hs, hs),
                new Rectangle(s.Right - half, s.Y - half, hs, hs),
                new Rectangle(s.Right - half, s.Y + s.Height / 2 - half, hs, hs),
                new Rectangle(s.Right - half, s.Bottom - half, hs, hs),
                new Rectangle(s.X + s.Width / 2 - half, s.Bottom - half, hs, hs),
                new Rectangle(s.X - half, s.Bottom - half, hs, hs),
                new Rectangle(s.X - half, s.Y + s.Height / 2 - half, hs, hs),
            };
        }
        public override int HitTestHandle(Point p)
        {
            Rectangle[] handles = GetHandles();
            for (int i = 0; i < handles.Length; i++)
            {
                Rectangle hit = handles[i];
                hit.Inflate(5, 5);
                if (hit.Contains(p)) return i;
            }
            return -1;
        }
        public override void ResizeHandle(int handleIndex, Point p)
        {
            // 文字缩放：根据拖动距离调整字号
            Rectangle bounds = GetBounds();
            float oldSize = Font.Size;
            float newSize = oldSize;

            switch (handleIndex)
            {
                case 2: case 3: case 4: // 右侧
                    if (bounds.Width > 0) newSize = oldSize * (float)p.X / bounds.Right;
                    break;
                case 5: case 6: // 下方
                    if (bounds.Height > 0) newSize = oldSize * (float)p.Y / bounds.Bottom;
                    break;
                case 0: case 7: // 左侧
                    if (bounds.Width > 0) newSize = oldSize * (float)(bounds.Right - p.X) / bounds.Width;
                    break;
                case 1: // 上方
                    if (bounds.Height > 0) newSize = oldSize * (float)(bounds.Bottom - p.Y) / bounds.Height;
                    break;
            }

            newSize = Math.Max(8f, Math.Min(72f, newSize));
            if (Math.Abs(newSize - oldSize) > 0.5f)
            {
                Font.Dispose();
                Font = new Font(Font.FontFamily, newSize, Font.Style);
            }
        }
    }

    class HighlightAnnotation : Annotation
    {
        public Rectangle Rect;
        public override void Draw(Graphics g, Bitmap src)
        {
            if (Rect.Width == 0 || Rect.Height == 0) return;
            using (SolidBrush b = new SolidBrush(Color.FromArgb(80, Color.Yellow)))
                g.FillRectangle(b, Rect);
        }
        public override Rectangle GetBounds() { return Rect; }
        public override void Move(int dx, int dy) { Rect = new Rectangle(Rect.X + dx, Rect.Y + dy, Rect.Width, Rect.Height); }
        public override bool HasHandles { get { return true; } }
        public override Rectangle[] GetHandles()
        {
            int hs = 8, half = hs / 2;
            Rectangle s = Rect;
            return new Rectangle[] {
                new Rectangle(s.X - half, s.Y - half, hs, hs),
                new Rectangle(s.X + s.Width / 2 - half, s.Y - half, hs, hs),
                new Rectangle(s.Right - half, s.Y - half, hs, hs),
                new Rectangle(s.Right - half, s.Y + s.Height / 2 - half, hs, hs),
                new Rectangle(s.Right - half, s.Bottom - half, hs, hs),
                new Rectangle(s.X + s.Width / 2 - half, s.Bottom - half, hs, hs),
                new Rectangle(s.X - half, s.Bottom - half, hs, hs),
                new Rectangle(s.X - half, s.Y + s.Height / 2 - half, hs, hs),
            };
        }
        public override int HitTestHandle(Point p)
        {
            Rectangle[] handles = GetHandles();
            for (int i = 0; i < handles.Length; i++)
            {
                Rectangle hit = handles[i];
                hit.Inflate(5, 5);
                if (hit.Contains(p)) return i;
            }
            return -1;
        }
        public override void ResizeHandle(int handleIndex, Point p)
        {
            int left = Rect.X, top = Rect.Y, right = Rect.Right, bottom = Rect.Bottom;
            switch (handleIndex)
            {
                case 0: left = p.X; top = p.Y; break;
                case 1: top = p.Y; break;
                case 2: right = p.X; top = p.Y; break;
                case 3: right = p.X; break;
                case 4: right = p.X; bottom = p.Y; break;
                case 5: bottom = p.Y; break;
                case 6: left = p.X; bottom = p.Y; break;
                case 7: left = p.X; break;
            }
            if (right - left < 5) right = left + 5;
            if (bottom - top < 5) bottom = top + 5;
            Rect = Rectangle.FromLTRB(Math.Min(left, right), Math.Min(top, bottom), Math.Max(left, right), Math.Max(top, bottom));
        }
    }

    class MosaicAnnotation : Annotation
    {
        public Rectangle Rect;
        private Bitmap _mosaicImg;
        private Bitmap _srcRef; // 保留源图引用，用于重新生成

        public MosaicAnnotation(Rectangle rect, Bitmap src)
        {
            Rect = rect;
            _srcRef = src;
            _mosaicImg = CreateMosaic(src, rect, 10);
        }

        static Bitmap CreateMosaic(Bitmap src, Rectangle rect, int blockSize)
        {
            // Clamp to bitmap bounds
            rect.Intersect(new Rectangle(0, 0, src.Width, src.Height));
            if (rect.Width <= 0 || rect.Height <= 0) return null;

            Bitmap bmp = new Bitmap(rect.Width, rect.Height);
            using (Graphics gBmp = Graphics.FromImage(bmp))
            {
                for (int y = 0; y < rect.Height; y += blockSize)
                {
                    for (int x = 0; x < rect.Width; x += blockSize)
                    {
                        int sx = Math.Min(Math.Max(rect.X + x + blockSize / 2, 0), src.Width - 1);
                        int sy = Math.Min(Math.Max(rect.Y + y + blockSize / 2, 0), src.Height - 1);
                        Color c = src.GetPixel(sx, sy);
                        int bw = Math.Min(blockSize, rect.Width - x);
                        int bh = Math.Min(blockSize, rect.Height - y);
                        using (SolidBrush b = new SolidBrush(c))
                            gBmp.FillRectangle(b, x, y, bw, bh);
                    }
                }
            }
            return bmp;
        }

        public override void Draw(Graphics g, Bitmap src)
        {
            if (_mosaicImg != null)
                g.DrawImage(_mosaicImg, Rect.Location);
        }
        public override Rectangle GetBounds() { return Rect; }
        public override void Move(int dx, int dy)
        {
            Rect = new Rectangle(Rect.X + dx, Rect.Y + dy, Rect.Width, Rect.Height);
            RebuildMosaic();
        }
        public override bool HasHandles { get { return true; } }
        public override Rectangle[] GetHandles()
        {
            int hs = 8, half = hs / 2;
            Rectangle s = Rect;
            return new Rectangle[] {
                new Rectangle(s.X - half, s.Y - half, hs, hs),
                new Rectangle(s.X + s.Width / 2 - half, s.Y - half, hs, hs),
                new Rectangle(s.Right - half, s.Y - half, hs, hs),
                new Rectangle(s.Right - half, s.Y + s.Height / 2 - half, hs, hs),
                new Rectangle(s.Right - half, s.Bottom - half, hs, hs),
                new Rectangle(s.X + s.Width / 2 - half, s.Bottom - half, hs, hs),
                new Rectangle(s.X - half, s.Bottom - half, hs, hs),
                new Rectangle(s.X - half, s.Y + s.Height / 2 - half, hs, hs),
            };
        }
        public override int HitTestHandle(Point p)
        {
            Rectangle[] handles = GetHandles();
            for (int i = 0; i < handles.Length; i++)
            {
                Rectangle hit = handles[i];
                hit.Inflate(5, 5);
                if (hit.Contains(p)) return i;
            }
            return -1;
        }
        public override void ResizeHandle(int handleIndex, Point p)
        {
            int left = Rect.X, top = Rect.Y, right = Rect.Right, bottom = Rect.Bottom;
            switch (handleIndex)
            {
                case 0: left = p.X; top = p.Y; break;
                case 1: top = p.Y; break;
                case 2: right = p.X; top = p.Y; break;
                case 3: right = p.X; break;
                case 4: right = p.X; bottom = p.Y; break;
                case 5: bottom = p.Y; break;
                case 6: left = p.X; bottom = p.Y; break;
                case 7: left = p.X; break;
            }
            if (right - left < 5) right = left + 5;
            if (bottom - top < 5) bottom = top + 5;
            Rect = Rectangle.FromLTRB(Math.Min(left, right), Math.Min(top, bottom), Math.Max(left, right), Math.Max(top, bottom));
            RebuildMosaic();
        }
        private void RebuildMosaic()
        {
            if (_mosaicImg != null) { _mosaicImg.Dispose(); _mosaicImg = null; }
            if (_srcRef != null)
                _mosaicImg = CreateMosaic(_srcRef, Rect, 10);
        }
    }

    #endregion

    #region ScreenshotOverlay

    class ScreenshotOverlay : Form
    {
        // Core fields
        private Bitmap _screenCapture;
        private OverlayState _state = OverlayState.Waiting;
        private Rectangle _selection = Rectangle.Empty;
        private Point _mouseStart;
        private Point _lastMouse;
        private HandlePos _activeHandle = HandlePos.None;

        // Tool & annotation
        private DrawTool _currentTool = DrawTool.None;
        private Color _drawColor = Color.Red;
        private int _drawWidth = 2;
        private List<Annotation> _annotations = new List<Annotation>();
        private Annotation _currentAnnotation;

        // Selected annotation (for drag/resize/edit)
        private Annotation _selectedAnnotation = null;
        private int _selectedAnnotationHandle = -1;
        private Point _annotationDragStart = Point.Empty; // position before drag for double-click revert

        // Toolbar
        private Panel _toolbar;
        private List<Button> _toolBtns = new List<Button>();
        private List<Panel> _colorPanels = new List<Panel>();
        private Panel _colorIndicator;
        private ToolTip _toolbarTip;
        private TrackBar _sizeTrackBar;
        private Label _sizeLabel;
        private Label _sizeValueLabel;

        // Text input
        private TextBox _textBox;

        // Text font settings
        private float _fontSize = 14f;

        // OCR PSM mode
        private int _ocrPsm = 4;
        private Button _ocrBtn;
        private Button _ocrArrowBtn;
        private ContextMenu _ocrMenu;

        // Context menu
        private ContextMenu _contextMenu;

        // Constants
        private const int HandleSize = 8;
        private const int MinSelection = 5;

        // Predefined palette
        private static readonly Color[] Palette = {
            Color.Red, Color.FromArgb(0,120,215), Color.Green, Color.Yellow,
            Color.Orange, Color.Purple, Color.Black, Color.White
        };

        // Size slider mapping:
        // Non-text tools (粗细): slider 1~20 → 1px~8px
        // Text tool (字号): slider 1~20 → 10pt~64pt

        public ScreenshotOverlay()
        {
            CaptureScreen();

            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.AutoScaleMode = AutoScaleMode.None;
            this.Bounds = SystemInformation.VirtualScreen;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.DoubleBuffered = true;
            this.KeyPreview = true;
            this.Cursor = Cursors.Cross;

            CreateToolbar();
            CreateContextMenu();

            this.MouseDown += OnMouseDown;
            this.MouseMove += OnMouseMove;
            this.MouseUp += OnMouseUp;
            this.MouseDoubleClick += OnMouseDoubleClick;
            this.KeyDown += OnKeyDown;
            this.Paint += OnPaint;
            this.FormClosing += OnFormClosing;
        }

        private void CaptureScreen()
        {
            Rectangle bounds = SystemInformation.VirtualScreen;
            _screenCapture = new Bitmap(bounds.Width, bounds.Height);
            using (Graphics g = Graphics.FromImage(_screenCapture))
                g.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size);
        }

        #region Toolbar

        private Font _toolbarFont;

        // 创建图标位图（白色线条在透明背景上，用于暗色工具栏按钮）
        private static Bitmap CreateIconBitmap(int size, Action<Graphics> draw)
        {
            Bitmap bmp = new Bitmap(size, size);
            bmp.MakeTransparent();
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                draw(g);
            }
            return bmp;
        }

        private static Bitmap IconRect()
        {
            return CreateIconBitmap(16, g => {
                g.DrawRectangle(new Pen(Color.White, 1.5f), 2, 2, 12, 12);
            });
        }

        private static Bitmap IconEllipse()
        {
            return CreateIconBitmap(16, g => {
                g.DrawEllipse(new Pen(Color.White, 1.5f), 2, 3, 12, 10);
            });
        }

        private static Bitmap IconArrow()
        {
            return CreateIconBitmap(16, g => {
                // 简洁的右箭头
                Point[] pts = { new Point(2, 8), new Point(12, 8) };
                g.DrawLines(new Pen(Color.White, 2f), pts);
                Point[] head = { new Point(9, 4), new Point(13, 8), new Point(9, 12) };
                g.FillPolygon(Brushes.White, head);
            });
        }

        private static Bitmap IconPen()
        {
            return CreateIconBitmap(16, g => {
                // 铅笔：斜线+笔尖
                Point[] shaft = { new Point(3, 13), new Point(12, 4) };
                g.DrawLines(new Pen(Color.White, 1.8f), shaft);
                // 笔尖
                g.FillPolygon(Brushes.White, new Point[] {
                    new Point(1, 15), new Point(3, 13), new Point(5, 15)
                });
                // 笔尾小横线
                g.DrawLine(new Pen(Color.White, 1.2f), 11, 5, 14, 2);
            });
        }

        private static Bitmap IconText()
        {
            return CreateIconBitmap(16, g => {
                // 单独的大写A
                using (Font f = new Font("Segoe UI", 12f, FontStyle.Bold))
                {
                    SizeF sz = g.MeasureString("A", f);
                    float x = (16 - sz.Width) / 2;
                    float y = (16 - sz.Height) / 2;
                    g.DrawString("A", f, Brushes.White, x, y);
                }
            });
        }

        private static Bitmap IconHighlight()
        {
            return CreateIconBitmap(16, g => {
                // 高亮：半透明扁矩形
                using (SolidBrush b = new SolidBrush(Color.FromArgb(180, 255, 255, 0)))
                {
                    g.FillRectangle(b, 1, 5, 14, 6);
                }
                g.DrawRectangle(new Pen(Color.FromArgb(200, 255, 255, 0), 1f), 1, 5, 14, 6);
            });
        }

        private static Bitmap IconMosaic()
        {
            return CreateIconBitmap(16, g => {
                // 马赛克：2×2 四个黑白方块
                g.FillRectangle(Brushes.White, 2, 2, 6, 6);
                g.DrawRectangle(new Pen(Color.White, 1f), 8, 2, 6, 6);
                g.DrawRectangle(new Pen(Color.White, 1f), 2, 8, 6, 6);
                g.FillRectangle(Brushes.White, 8, 8, 6, 6);
            });
        }

        private static Bitmap IconUndo()
        {
            return CreateIconBitmap(16, g => {
                // 撤销：逆时针圆弧箭头
                using (Pen p = new Pen(Color.White, 2f))
                {
                    // 圆心(8,7) 半径5 → 外接矩形(3,2,12,12)，加上线宽余量后最大到15
                    g.DrawArc(p, 3, 1, 11, 13, -15, 285);
                    // 箭头头：尖端在12点钟位置(圆弧顶端)，指向右边
                    Point[] head = { new Point(8, -3), new Point(14, 1), new Point(8, 4) };
                    g.FillPolygon(Brushes.White, head);
                }
            });
        }

        private static Bitmap IconOCR()
        {
            return CreateIconBitmap(16, g => {
                // 识别：四角直角括号包裹大写A
                using (Font f = new Font("Segoe UI", 9f, FontStyle.Bold))
                {
                    // A字居中
                    SizeF sz = g.MeasureString("A", f);
                    float tx = (16 - sz.Width) / 2;
                    float ty = (16 - sz.Height) / 2;
                    g.DrawString("A", f, Brushes.White, tx, ty);
                }
                // 四角直角括号
                using (Pen p = new Pen(Color.White, 1.2f))
                {
                    // 左上
                    g.DrawLine(p, 1, 2, 1, 1); g.DrawLine(p, 1, 1, 3, 1);
                    // 右上
                    g.DrawLine(p, 13, 1, 15, 1); g.DrawLine(p, 15, 1, 15, 2);
                    // 左下
                    g.DrawLine(p, 1, 14, 1, 15); g.DrawLine(p, 1, 15, 3, 15);
                    // 右下
                    g.DrawLine(p, 13, 15, 15, 15); g.DrawLine(p, 15, 14, 15, 15);
                }
            });
        }

        private static Bitmap IconSave()
        {
            return CreateIconBitmap(16, g => {
                // 另存为：下载图标——向下箭头+底部托盘
                // 向下箭头
                g.DrawLine(new Pen(Color.White, 2f), 8, 1, 8, 11);
                Point[] arrowHead = { new Point(4, 8), new Point(8, 13), new Point(12, 8) };
                g.FillPolygon(Brushes.White, arrowHead);
                // 底部托盘
                g.DrawLine(new Pen(Color.White, 1.5f), 2, 14, 14, 14);
                g.DrawLine(new Pen(Color.White, 1.5f), 2, 14, 2, 11);
                g.DrawLine(new Pen(Color.White, 1.5f), 14, 14, 14, 11);
            });
        }

        private static Bitmap IconCopy()
        {
            return CreateIconBitmap(16, g => {
                // 复制：打勾
                using (Pen p = new Pen(Color.White, 2f))
                {
                    g.DrawLine(p, 2, 8, 6, 13);
                    g.DrawLine(p, 6, 13, 14, 3);
                }
            });
        }

        private static Bitmap IconCancel()
        {
            return CreateIconBitmap(16, g => {
                // 取消：X
                g.DrawLine(new Pen(Color.White, 2f), 3, 3, 13, 13);
                g.DrawLine(new Pen(Color.White, 2f), 13, 3, 3, 13);
            });
        }

        private void CreateToolbar()
        {
            _toolbarFont = new Font("微软雅黑", 9f);
            _toolbarTip = new ToolTip();
            _toolbarTip.ShowAlways = true;

            _toolbar = new Panel();
            _toolbar.BackColor = Color.FromArgb(45, 45, 48);
            _toolbar.Height = 42;
            _toolbar.Visible = false;
            _toolbar.SuspendLayout();

            // Color indicator (white border behind selected color)
            _colorIndicator = new Panel();
            _colorIndicator.BackColor = Color.White;
            _colorIndicator.Size = new Size(24, 24);
            _colorIndicator.Visible = false;
            _toolbar.Controls.Add(_colorIndicator);

            int x = 8;

            // === Tool buttons (图标) ===
            string[] toolNames = { "矩形", "椭圆", "箭头", "画笔", "文字", "高亮", "马赛克" };
            DrawTool[] toolEnums = {
                DrawTool.Rect, DrawTool.Ellipse, DrawTool.Arrow,
                DrawTool.Pen, DrawTool.Text, DrawTool.Highlight, DrawTool.Mosaic
            };
            Bitmap[] toolIcons = {
                IconRect(), IconEllipse(), IconArrow(),
                IconPen(), IconText(), IconHighlight(), IconMosaic()
            };

            for (int i = 0; i < toolNames.Length; i++)
            {
                int idx = i;
                x = AddIconBtn(toolIcons[i], toolNames[idx], x, delegate { SelectTool(toolEnums[idx]); }, _toolBtns);
            }

            // Separator
            AddSep(x, 6); x += 8;

            // === Color palette ===
            for (int i = 0; i < Palette.Length; i++)
            {
                Panel cp = new Panel();
                cp.Size = new Size(20, 20);
                cp.Location = new Point(x + 1, 10);
                cp.BackColor = Palette[i];
                cp.BorderStyle = BorderStyle.FixedSingle;
                cp.Cursor = Cursors.Hand;

                Color c = Palette[i];
                int ci = i;
                cp.Click += delegate {
                    _drawColor = c;
                    UpdateColorIndicator(ci);
                    // 同步更新正在编辑的TextBox颜色
                    if (_textBox != null)
                        _textBox.ForeColor = c;
                    // 同步更新选中标注的颜色
                    if (_selectedAnnotation != null)
                    {
                        _selectedAnnotation.Color = c;
                        this.Invalidate();
                    }
                };

                _toolbar.Controls.Add(cp);
                _colorPanels.Add(cp);
                x += 24;
            }

            AddSep(x, 6); x += 8;

            // === Size slider (粗细/字号) ===
            _sizeLabel = new Label();
            _sizeLabel.Text = "粗细";
            _sizeLabel.ForeColor = Color.White;
            _sizeLabel.BackColor = Color.FromArgb(45, 45, 48);
            _sizeLabel.Font = _toolbarFont;
            _sizeLabel.AutoSize = true;
            _sizeLabel.Location = new Point(x, 14);
            _toolbar.Controls.Add(_sizeLabel);
            x += _sizeLabel.PreferredWidth + 4;

            _sizeTrackBar = new TrackBar();
            _sizeTrackBar.Minimum = 1;
            _sizeTrackBar.Maximum = 20;
            _sizeTrackBar.Value = SliderFromWidth(_drawWidth);
            _sizeTrackBar.TickStyle = TickStyle.None;
            _sizeTrackBar.Size = new Size(120, 30);
            _sizeTrackBar.Location = new Point(x, 6);
            _sizeTrackBar.BackColor = Color.FromArgb(45, 45, 48);
            _sizeTrackBar.Scroll += delegate {
                OnSizeSliderChanged();
            };
            _toolbar.Controls.Add(_sizeTrackBar);
            x += _sizeTrackBar.Width + 2;

            _sizeValueLabel = new Label();
            _sizeValueLabel.Text = _sizeTrackBar.Value.ToString();
            _sizeValueLabel.ForeColor = Color.White;
            _sizeValueLabel.BackColor = Color.FromArgb(45, 45, 48);
            _sizeValueLabel.Font = _toolbarFont;
            _sizeValueLabel.AutoSize = true;
            _sizeValueLabel.Location = new Point(x, 14);
            _toolbar.Controls.Add(_sizeValueLabel);
            x += _sizeValueLabel.PreferredWidth + 6;

            AddSep(x, 6); x += 8;

            // === Action buttons (图标) ===
            x = AddIconBtn(IconUndo(), "撤销", x, delegate { Undo(); }, null);

            // OCR button with dropdown arrow
            _ocrBtn = new Button();
            _ocrBtn.Image = IconOCR();
            _ocrBtn.Tag = "识别";
            _ocrBtn.FlatStyle = FlatStyle.Flat;
            _ocrBtn.FlatAppearance.BorderSize = 0;
            _ocrBtn.Size = new Size(28, 28);
            _ocrBtn.Location = new Point(x, 7);
            _ocrBtn.Cursor = Cursors.Hand;
            _ocrBtn.Click += delegate { DoOCR(); };
            _toolbar.Controls.Add(_ocrBtn);
            _toolbarTip.SetToolTip(_ocrBtn, "识别");
            x += _ocrBtn.Width;

            // OCR dropdown arrow
            _ocrArrowBtn = new Button();
            _ocrArrowBtn.Font = new Font("Marlett", 8f);
            _ocrArrowBtn.Text = "6"; // Marlett 6 = down triangle
            _ocrArrowBtn.ForeColor = Color.White;
            _ocrArrowBtn.BackColor = Color.FromArgb(70, 70, 74);
            _ocrArrowBtn.FlatStyle = FlatStyle.Flat;
            _ocrArrowBtn.FlatAppearance.BorderSize = 0;
            _ocrArrowBtn.Size = new Size(16, 28);
            _ocrArrowBtn.Location = new Point(x, 7);
            _ocrArrowBtn.Cursor = Cursors.Hand;
            _ocrArrowBtn.Click += delegate { ShowOcrDropdown(); };
            _toolbar.Controls.Add(_ocrArrowBtn);
            x += _ocrArrowBtn.Width + 2;

            // Build OCR dropdown menu
            _ocrMenu = new ContextMenu();
            MenuItem miHelp = new MenuItem("模式说明…");
            miHelp.Click += delegate { ShowOcrModeHelp(); };
            _ocrMenu.MenuItems.Add(miHelp);
            _ocrMenu.MenuItems.Add("-"); // separator

            // PSM modes
            int[] psmValues = { 3, 4, 6, 7, 8, 11 };
            string[] psmLabels = {
                "PSM 3 - 全自动分页",
                "PSM 4 - 单列文本（默认）",
                "PSM 6 - 统一文本块",
                "PSM 7 - 单行文本",
                "PSM 8 - 单个词",
                "PSM 11 - 稀疏文本"
            };
            for (int i = 0; i < psmValues.Length; i++)
            {
                int psm = psmValues[i];
                MenuItem mi = new MenuItem(psmLabels[i]);
                mi.Checked = (psm == _ocrPsm);
                mi.RadioCheck = true;
                mi.Click += delegate {
                    _ocrPsm = psm;
                    // 更新菜单打勾
                    foreach (MenuItem m in _ocrMenu.MenuItems)
                        m.Checked = (m.Text == psmLabels[Array.IndexOf(psmValues, psm)]);
                };
                _ocrMenu.MenuItems.Add(mi);
            }

            x = AddIconBtn(IconSave(), "另存为", x, delegate { Save(); }, null);
            x = AddIconBtn(IconCancel(), "取消", x, delegate { Cancel(); }, null);
            x = AddIconBtn(IconCopy(), "复制", x, delegate { CopyAndClose(); }, null);

            _toolbar.Width = x + 8;

            UpdateColorIndicator(0);
            UpdateToolSelection();

            _toolbar.ResumeLayout(false);
            _toolbar.PerformLayout();

            this.Controls.Add(_toolbar);
        }

        private Font BuildFont()
        {
            return new Font("微软雅黑", _fontSize);
        }

        private void UpdateTextBoxFont()
        {
            if (_textBox != null)
            {
                _textBox.Font = BuildFont();
                _textBox.ForeColor = _drawColor;
                // 跟随字号调整 TextBox 大小，确保文本不被遮挡
                using (Graphics g = this.CreateGraphics())
                {
                    string text = _textBox.Text;
                    if (string.IsNullOrEmpty(text)) text = "A";
                    SizeF sz = g.MeasureString(text, _textBox.Font);
                    _textBox.Width = Math.Max(200, (int)sz.Width + 20);
                    _textBox.Height = Math.Max(30, (int)sz.Height + 10);
                }
            }
        }

        // Slider mapping: 粗细 slider 1~20 → 1px~8px (等比)
        private int SliderFromWidth(int w)
        {
            // w: 1~8, slider: 1~20
            // Linear: slider = (w - 1) * 19 / 7 + 1
            if (w <= 1) return 1;
            if (w >= 8) return 20;
            return (w - 1) * 19 / 7 + 1;
        }

        private int WidthFromSlider(int s)
        {
            // s: 1~20, w: 1~8
            // Linear: w = (s - 1) * 7 / 19 + 1
            return Math.Max(1, Math.Min(8, (s - 1) * 7 / 19 + 1));
        }

        // Slider mapping: 字号 slider 1~20 → 10pt~64pt (等比)
        private int SliderFromFontSize(float fs)
        {
            // fs: 10~64, slider: 1~20
            // Linear: slider = (int)((fs - 10) * 19 / 54 + 1)
            int s = (int)Math.Round((fs - 10f) * 19f / 54f) + 1;
            return Math.Max(1, Math.Min(20, s));
        }

        private float FontSizeFromSlider(int s)
        {
            // s: 1~20, fs: 10~64
            // Linear: fs = (s - 1) * 54 / 19 + 10
            return (s - 1) * 54f / 19f + 10f;
        }

        private void OnSizeSliderChanged()
        {
            int val = _sizeTrackBar.Value;

            // 更新数值显示
            if (_sizeValueLabel != null)
                _sizeValueLabel.Text = val.ToString();

            // 判断是否应该调字号：编辑文字/文字工具/选中了文字标注
            bool isTextMode = (_textBox != null) || _currentTool == DrawTool.Text
                || (_selectedAnnotation is TextAnnotation);

            if (isTextMode)
            {
                _fontSize = FontSizeFromSlider(val);
                UpdateTextBoxFont();

                // 同步更新选中的文字标注
                if (_selectedAnnotation is TextAnnotation)
                {
                    TextAnnotation ta = (TextAnnotation)_selectedAnnotation;
                    ta.Font.Dispose();
                    ta.Font = new Font(ta.Font.FontFamily, _fontSize, ta.Font.Style);
                    this.Invalidate();
                }
            }
            else
            {
                _drawWidth = WidthFromSlider(val);

                // 同步更新选中的非文字标注的粗细
                if (_selectedAnnotation != null && !(_selectedAnnotation is TextAnnotation))
                {
                    _selectedAnnotation.PenWidth = _drawWidth;
                    this.Invalidate();
                }
            }
        }

        private void SyncSizeSlider()
        {
            if (_sizeTrackBar == null) return;

            int val;
            // 选中标注时：同步到标注的属性
            if (_selectedAnnotation is TextAnnotation)
            {
                TextAnnotation ta = (TextAnnotation)_selectedAnnotation;
                _sizeLabel.Text = "字号";
                _fontSize = ta.Font.Size;
                val = SliderFromFontSize(_fontSize);
            }
            else if (_selectedAnnotation != null)
            {
                _sizeLabel.Text = "粗细";
                _drawWidth = _selectedAnnotation.PenWidth;
                val = SliderFromWidth(_drawWidth);
            }
            else if (_currentTool == DrawTool.Text)
            {
                _sizeLabel.Text = "字号";
                val = SliderFromFontSize(_fontSize);
            }
            else
            {
                _sizeLabel.Text = "粗细";
                val = SliderFromWidth(_drawWidth);
            }

            _sizeTrackBar.Value = val;
            if (_sizeValueLabel != null)
                _sizeValueLabel.Text = val.ToString();
        }

        private int AddIconBtn(Bitmap icon, string tooltip, int x, EventHandler handler, List<Button> list)
        {
            int btnSize = 30;
            Button btn = new Button();
            btn.Image = icon;
            btn.Tag = tooltip;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderColor = Color.FromArgb(70, 70, 70);
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(70, 70, 74);
            btn.BackColor = Color.FromArgb(45, 45, 48);
            btn.Size = new Size(btnSize, btnSize);
            btn.Location = new Point(x, (42 - btnSize) / 2);
            btn.Cursor = Cursors.Hand;
            btn.Click += handler;
            _toolbar.Controls.Add(btn);
            _toolbarTip.SetToolTip(btn, tooltip);
            if (list != null) list.Add(btn);
            return x + btnSize + 3;
        }

        private int AddTextBtn(string text, int x, Font font, EventHandler handler, List<Button> list)
        {
            return AddTextBtn(text, x, font, handler, list, _toolbar, 42);
        }

        private int AddTextBtn(string text, int x, Font font, EventHandler handler, List<Button> list, Panel parent, int barHeight)
        {
            Size textSize = TextRenderer.MeasureText(text, font);
            int btnW = textSize.Width + 16;
            int btnH = textSize.Height + 8;

            Button btn = new Button();
            btn.Text = text;
            btn.Tag = text; // 用于识别按钮
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderColor = Color.FromArgb(70, 70, 70);
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(70, 70, 74);
            btn.ForeColor = Color.White;
            btn.BackColor = Color.FromArgb(45, 45, 48);
            btn.Size = new Size(btnW, btnH);
            btn.Location = new Point(x, (barHeight - btnH) / 2);
            btn.Font = font;
            btn.Cursor = Cursors.Hand;
            btn.Click += handler;
            parent.Controls.Add(btn);
            if (list != null) list.Add(btn);
            return x + btnW + 4;
        }

        private void AddSep(int x, int y)
        {
            AddSep(x, y, _toolbar);
        }

        private void AddSep(int x, int y, Panel parent)
        {
            Panel sep = new Panel();
            sep.BackColor = Color.FromArgb(80, 80, 80);
            sep.Size = new Size(1, 28);
            sep.Location = new Point(x, y);
            parent.Controls.Add(sep);
        }

        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
        private const int WM_SETREDRAW = 0x000B;

        private void PositionToolbar()
        {
            if (_selection.Width < MinSelection || _selection.Height < MinSelection) return;

            int tbW = _toolbar.Width;
            int tbH = _toolbar.Height;

            // Center horizontally relative to selection
            int x = _selection.X + (_selection.Width - tbW) / 2;
            // Clamp to screen bounds (toolbar can extend beyond selection)
            x = Math.Max(2, Math.Min(x, this.ClientSize.Width - tbW - 2));

            // Below selection if space, otherwise above
            int y = _selection.Bottom + 4;
            if (y + tbH > this.ClientSize.Height)
                y = _selection.Y - tbH - 4;

            // If still above screen top, place inside selection at bottom
            if (y < 2)
                y = _selection.Bottom - tbH - 4;

            _toolbar.Location = new Point(x, y);

            if (!_toolbar.Visible)
            {
                // 暂停重绘，避免控件逐个渲染造成闪烁
                SendMessage(_toolbar.Handle, WM_SETREDRAW, (IntPtr)0, IntPtr.Zero);
                _toolbar.Visible = true;
                _toolbar.BringToFront();
                // 恢复重绘并一次性刷新
                SendMessage(_toolbar.Handle, WM_SETREDRAW, (IntPtr)1, IntPtr.Zero);
                _toolbar.Refresh();
            }
            else
            {
                _toolbar.BringToFront();
            }
        }

        private void SelectTool(DrawTool tool)
        {
            // 如果正在编辑文字（新建或编辑已有），先提交
            if (_textBox != null && (_state == OverlayState.AnnotationEditing || _state == OverlayState.TextInput))
                CommitText();

            _currentTool = (_currentTool == tool) ? DrawTool.None : tool;
            UpdateToolSelection();
            SyncSizeSlider();
            this.Focus();
        }

        private void UpdateToolSelection()
        {
            // 工具按钮顺序与 DrawTool 枚举对应：Rect=1, Ellipse=2, ...
            DrawTool[] toolOrder = {
                DrawTool.Rect, DrawTool.Ellipse, DrawTool.Arrow,
                DrawTool.Pen, DrawTool.Text, DrawTool.Highlight, DrawTool.Mosaic
            };
            for (int i = 0; i < _toolBtns.Count && i < toolOrder.Length; i++)
            {
                if (toolOrder[i] == _currentTool)
                {
                    _toolBtns[i].BackColor = Color.FromArgb(0, 120, 215);
                    _toolBtns[i].FlatAppearance.BorderColor = Color.FromArgb(0, 120, 215);
                }
                else
                {
                    _toolBtns[i].BackColor = Color.FromArgb(45, 45, 48);
                    _toolBtns[i].FlatAppearance.BorderColor = Color.FromArgb(70, 70, 70);
                }
            }
        }

        private void UpdateColorIndicator(int selectedIndex)
        {
            if (selectedIndex >= 0 && selectedIndex < _colorPanels.Count)
            {
                Panel cp = _colorPanels[selectedIndex];
                _colorIndicator.Location = new Point(cp.Left - 3, cp.Top - 3);
                _colorIndicator.Size = new Size(24, 24);
                _colorIndicator.Visible = true;
                _colorIndicator.BringToFront();
                cp.BringToFront();
            }
        }

        #endregion

        #region Context Menu

        private void CreateContextMenu()
        {
            _contextMenu = new ContextMenu();
            _contextMenu.MenuItems.Add("重新截图", delegate { ResetSelection(); });
            _contextMenu.MenuItems.Add("-");
            _contextMenu.MenuItems.Add("复制  Ctrl+C", delegate { CopyAndClose(); });
            _contextMenu.MenuItems.Add("保存  Ctrl+S", delegate { Save(); });
            _contextMenu.MenuItems.Add("-");
            _contextMenu.MenuItems.Add("取消  Esc", delegate { Cancel(); });
        }

        #endregion

        #region Handle Management

        private Rectangle[] GetHandles()
        {
            Rectangle s = _selection;
            int hs = HandleSize;
            int half = hs / 2;
            return new Rectangle[] {
                new Rectangle(s.X - half, s.Y - half, hs, hs),
                new Rectangle(s.X + s.Width / 2 - half, s.Y - half, hs, hs),
                new Rectangle(s.Right - half, s.Y - half, hs, hs),
                new Rectangle(s.Right - half, s.Y + s.Height / 2 - half, hs, hs),
                new Rectangle(s.Right - half, s.Bottom - half, hs, hs),
                new Rectangle(s.X + s.Width / 2 - half, s.Bottom - half, hs, hs),
                new Rectangle(s.X - half, s.Bottom - half, hs, hs),
                new Rectangle(s.X - half, s.Y + s.Height / 2 - half, hs, hs),
            };
        }

        private HandlePos HitTestHandle(Point p)
        {
            Rectangle[] handles = GetHandles();
            HandlePos[] positions = {
                HandlePos.TL, HandlePos.T, HandlePos.TR, HandlePos.R,
                HandlePos.BR, HandlePos.B, HandlePos.BL, HandlePos.L
            };
            for (int i = 0; i < handles.Length; i++)
            {
                Rectangle hit = handles[i];
                hit.Inflate(5, 5);
                if (hit.Contains(p)) return positions[i];
            }
            return HandlePos.None;
        }

        private Cursor GetHandleCursor(HandlePos h)
        {
            switch (h)
            {
                case HandlePos.TL: case HandlePos.BR: return Cursors.SizeNWSE;
                case HandlePos.TR: case HandlePos.BL: return Cursors.SizeNESW;
                case HandlePos.T: case HandlePos.B: return Cursors.SizeNS;
                case HandlePos.L: case HandlePos.R: return Cursors.SizeWE;
                default: return Cursors.Cross;
            }
        }

        #endregion

        #region Mouse Handlers

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (_state == OverlayState.Selected || _state == OverlayState.TextInput
                    || _state == OverlayState.AnnotationDragging || _state == OverlayState.AnnotationResizing)
                    _contextMenu.Show(this, e.Location);
                return;
            }
            if (e.Button != MouseButtons.Left) return;

            Point p = e.Location;

            switch (_state)
            {
                case OverlayState.Waiting:
                    _state = OverlayState.Selecting;
                    _selection = new Rectangle(p, Size.Empty);
                    break;

                case OverlayState.TextInput:
                case OverlayState.AnnotationEditing:
                    CommitText();
                    break;

                case OverlayState.Selected:
                    // 1. Check annotation handles first
                    if (_selectedAnnotation != null && _selectedAnnotation.HasHandles)
                    {
                        int hIdx = _selectedAnnotation.HitTestHandle(p);
                        if (hIdx >= 0)
                        {
                            _state = OverlayState.AnnotationResizing;
                            _selectedAnnotationHandle = hIdx;
                            break;
                        }
                    }
                    // 2. Check annotation hit (reverse order = topmost first)
                    bool hitAnnotation = false;
                    for (int i = _annotations.Count - 1; i >= 0; i--)
                    {
                        Annotation a = _annotations[i];
                        if (a.HitTest(p))
                        {
                            _selectedAnnotation = a;
                            _selectedAnnotationHandle = -1;
                            hitAnnotation = true;
                            _state = OverlayState.AnnotationDragging;
                            // Save position before drag for potential double-click revert
                            _annotationDragStart = a.GetBounds().Location;
                            // Sync slider to annotation's properties
                            SyncSizeSlider();
                            break;
                        }
                    }
                    if (!hitAnnotation)
                    {
                        _selectedAnnotation = null;
                        // Sync slider back to tool settings
                        SyncSizeSlider();

                        // 3. Selection handles
                        HandlePos handle = HitTestHandle(p);
                        if (handle != HandlePos.None)
                        {
                            _state = OverlayState.Resizing;
                            _activeHandle = handle;
                            break;
                        }
                        // 4. Inside selection
                        if (_selection.Contains(p))
                        {
                            if (_currentTool == DrawTool.None)
                                _state = OverlayState.Moving;
                            else if (_currentTool == DrawTool.Text)
                                StartTextInput(p);
                            else
                            {
                                _state = OverlayState.Drawing;
                                CreateAnnotation(p);
                            }
                        }
                    }
                    break;

                case OverlayState.AnnotationDragging:
                case OverlayState.AnnotationResizing:
                    // Click outside annotation -> deselect
                    _state = OverlayState.Selected;
                    _selectedAnnotation = null;
                    SyncSizeSlider();
                    this.Invalidate();
                    break;
            }

            _mouseStart = p;
            _lastMouse = p;
            this.Invalidate();
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            Point p = e.Location;

            switch (_state)
            {
                case OverlayState.Selecting:
                    _selection = MakeRect(_mouseStart, p);
                    break;

                case OverlayState.Moving:
                    int dx = p.X - _lastMouse.X;
                    int dy = p.Y - _lastMouse.Y;
                    _selection.X += dx;
                    _selection.Y += dy;
                    // Clamp
                    _selection.X = Math.Max(0, Math.Min(_selection.X, this.ClientSize.Width - _selection.Width));
                    _selection.Y = Math.Max(0, Math.Min(_selection.Y, this.ClientSize.Height - _selection.Height));
                    PositionToolbar();
                    break;

                case OverlayState.Resizing:
                    ResizeSelection(p);
                    PositionToolbar();
                    break;

                case OverlayState.Drawing:
                    UpdateAnnotation(p);
                    break;

                case OverlayState.AnnotationDragging:
                    if (_selectedAnnotation != null)
                    {
                        int adx = p.X - _lastMouse.X;
                        int ady = p.Y - _lastMouse.Y;
                        _selectedAnnotation.Move(adx, ady);
                    }
                    break;

                case OverlayState.AnnotationResizing:
                    if (_selectedAnnotation != null && _selectedAnnotationHandle >= 0)
                    {
                        _selectedAnnotation.ResizeHandle(_selectedAnnotationHandle, p);
                    }
                    break;

                case OverlayState.Selected:
                    UpdateCursorAt(p);
                    break;

                case OverlayState.Waiting:
                    this.Cursor = Cursors.Cross;
                    break;
            }

            _lastMouse = p;
            this.Invalidate();
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            switch (_state)
            {
                case OverlayState.Selecting:
                    NormalizeSelection();
                    if (_selection.Width < MinSelection || _selection.Height < MinSelection)
                    {
                        _selection = Rectangle.Empty;
                        _state = OverlayState.Waiting;
                    }
                    else
                    {
                        _state = OverlayState.Selected;
                        PositionToolbar();
                    }
                    break;

                case OverlayState.Moving:
                    _state = OverlayState.Selected;
                    break;

                case OverlayState.Resizing:
                    NormalizeSelection();
                    _state = OverlayState.Selected;
                    PositionToolbar();
                    break;

                case OverlayState.Drawing:
                    CommitAnnotation();
                    _state = OverlayState.Selected;
                    break;

                case OverlayState.AnnotationDragging:
                case OverlayState.AnnotationResizing:
                    _state = OverlayState.Selected;
                    break;
            }

            this.Invalidate();
        }

        private void OnMouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            // Double-click on text annotation -> edit
            // Check both Selected and AnnotationDragging states (double-click triggers MouseDown first)
            if (_state == OverlayState.Selected || _state == OverlayState.AnnotationDragging)
            {
                for (int i = _annotations.Count - 1; i >= 0; i--)
                {
                    if (_annotations[i] is TextAnnotation && _annotations[i].HitTest(e.Location))
                    {
                        // Revert drag offset caused by the 2nd MouseDown
                        if (_state == OverlayState.AnnotationDragging && _selectedAnnotation != null)
                        {
                            Rectangle curBounds = _selectedAnnotation.GetBounds();
                            _selectedAnnotation.Move(
                                _annotationDragStart.X - curBounds.X,
                                _annotationDragStart.Y - curBounds.Y);
                        }
                        _state = OverlayState.Selected;
                        EditTextAnnotation((TextAnnotation)_annotations[i]);
                        return;
                    }
                }
            }

            // Double-click on selection (but NOT on any annotation) -> copy
            if (_state == OverlayState.Selected
                && _selectedAnnotation == null
                && _selection.Width > MinSelection && _selection.Height > MinSelection
                && _selection.Contains(e.Location))
            {
                CopyAndClose();
            }
        }

        #endregion

        #region Keyboard

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (_state == OverlayState.TextInput || _state == OverlayState.AnnotationEditing) return;

            if (e.KeyCode == Keys.Escape)
                Cancel();
            else if (e.Control && e.KeyCode == Keys.Z)
                Undo();
            else if (e.Control && e.KeyCode == Keys.C)
                CopyAndClose();
            else if (e.Control && e.KeyCode == Keys.S)
                Save();
            else if (e.KeyCode == Keys.Delete && _selectedAnnotation != null)
            {
                _annotations.Remove(_selectedAnnotation);
                _selectedAnnotation = null;
                this.Invalidate();
            }
        }

        #endregion

        #region Selection

        private void NormalizeSelection()
        {
            if (_selection.Width < 0)
            {
                _selection.X += _selection.Width;
                _selection.Width = -_selection.Width;
            }
            if (_selection.Height < 0)
            {
                _selection.Y += _selection.Height;
                _selection.Height = -_selection.Height;
            }
        }

        private void ResizeSelection(Point p)
        {
            int left = _selection.X;
            int top = _selection.Y;
            int right = _selection.Right;
            int bottom = _selection.Bottom;

            switch (_activeHandle)
            {
                case HandlePos.TL: left = p.X; top = p.Y; break;
                case HandlePos.T: top = p.Y; break;
                case HandlePos.TR: right = p.X; top = p.Y; break;
                case HandlePos.R: right = p.X; break;
                case HandlePos.BR: right = p.X; bottom = p.Y; break;
                case HandlePos.B: bottom = p.Y; break;
                case HandlePos.BL: left = p.X; bottom = p.Y; break;
                case HandlePos.L: left = p.X; break;
            }

            if (right - left < MinSelection) right = left + MinSelection;
            if (bottom - top < MinSelection) bottom = top + MinSelection;

            _selection = Rectangle.FromLTRB(
                Math.Min(left, right), Math.Min(top, bottom),
                Math.Max(left, right), Math.Max(top, bottom));
        }

        private void ResetSelection()
        {
            _selection = Rectangle.Empty;
            _annotations.Clear();
            _currentAnnotation = null;
            _currentTool = DrawTool.None;
            _state = OverlayState.Waiting;
            _selectedAnnotation = null;
            _toolbar.Visible = false;
            RemoveTextBox();
            UpdateToolSelection();
            this.Invalidate();
        }

        private void UpdateCursorAt(Point p)
        {
            // Check annotation handles
            if (_selectedAnnotation != null && _selectedAnnotation.HasHandles)
            {
                int hIdx = _selectedAnnotation.HitTestHandle(p);
                if (hIdx >= 0)
                {
                    if (hIdx == 0 || hIdx == 4) this.Cursor = Cursors.SizeNWSE;
                    else if (hIdx == 2 || hIdx == 6) this.Cursor = Cursors.SizeNESW;
                    else if (hIdx == 1 || hIdx == 5) this.Cursor = Cursors.SizeNS;
                    else this.Cursor = Cursors.SizeWE;
                    return;
                }
            }

            // Check annotation hover
            for (int i = _annotations.Count - 1; i >= 0; i--)
            {
                if (_annotations[i].HitTest(p))
                {
                    this.Cursor = Cursors.SizeAll;
                    return;
                }
            }

            HandlePos handle = HitTestHandle(p);
            if (handle != HandlePos.None)
            {
                this.Cursor = GetHandleCursor(handle);
            }
            else if (_selection.Contains(p))
            {
                this.Cursor = (_currentTool == DrawTool.None) ? Cursors.SizeAll : Cursors.Cross;
            }
            else
            {
                this.Cursor = Cursors.Cross;
            }
        }

        #endregion

        #region Annotations

        private void CreateAnnotation(Point p)
        {
            switch (_currentTool)
            {
                case DrawTool.Rect:
                    _currentAnnotation = new RectAnnotation();
                    ((RectAnnotation)_currentAnnotation).Rect = new Rectangle(p, Size.Empty);
                    break;
                case DrawTool.Ellipse:
                    _currentAnnotation = new EllipseAnnotation();
                    ((EllipseAnnotation)_currentAnnotation).Rect = new Rectangle(p, Size.Empty);
                    break;
                case DrawTool.Arrow:
                    _currentAnnotation = new ArrowAnnotation();
                    ((ArrowAnnotation)_currentAnnotation).Start = p;
                    ((ArrowAnnotation)_currentAnnotation).End = p;
                    break;
                case DrawTool.Pen:
                    _currentAnnotation = new PenAnnotation();
                    ((PenAnnotation)_currentAnnotation).Points.Add(p);
                    break;
                case DrawTool.Highlight:
                    _currentAnnotation = new HighlightAnnotation();
                    ((HighlightAnnotation)_currentAnnotation).Rect = new Rectangle(p, Size.Empty);
                    break;
                case DrawTool.Mosaic:
                    _currentAnnotation = new MosaicAnnotation(Rectangle.Empty, _screenCapture);
                    break;
            }
            if (_currentAnnotation != null)
            {
                _currentAnnotation.Color = _drawColor;
                _currentAnnotation.PenWidth = _drawWidth;
            }
        }

        private void UpdateAnnotation(Point p)
        {
            if (_currentAnnotation == null) return;

            switch (_currentTool)
            {
                case DrawTool.Rect:
                    ((RectAnnotation)_currentAnnotation).Rect = MakeRect(_mouseStart, p);
                    break;
                case DrawTool.Ellipse:
                    ((EllipseAnnotation)_currentAnnotation).Rect = MakeRect(_mouseStart, p);
                    break;
                case DrawTool.Highlight:
                    ((HighlightAnnotation)_currentAnnotation).Rect = MakeRect(_mouseStart, p);
                    break;
                case DrawTool.Arrow:
                    ((ArrowAnnotation)_currentAnnotation).End = p;
                    break;
                case DrawTool.Pen:
                    ((PenAnnotation)_currentAnnotation).Points.Add(p);
                    break;
                // Mosaic: preview handled in Paint
            }
        }

        private void CommitAnnotation()
        {
            if (_currentAnnotation == null) return;

            if (_currentTool == DrawTool.Mosaic)
            {
                Rectangle mosaicRect = MakeRect(_mouseStart, _lastMouse);
                if (mosaicRect.Width > 2 && mosaicRect.Height > 2)
                {
                    _currentAnnotation = new MosaicAnnotation(mosaicRect, _screenCapture);
                    _currentAnnotation.Color = _drawColor;
                    _currentAnnotation.PenWidth = _drawWidth;
                    _annotations.Add(_currentAnnotation);
                }
            }
            else
            {
                _annotations.Add(_currentAnnotation);
            }
            _currentAnnotation = null;
        }

        #endregion

        #region Text Input

        private TextAnnotation _editingAnnotation = null; // 正在编辑的已有标注

        private void StartTextInput(Point p)
        {
            _state = OverlayState.TextInput;
            _editingAnnotation = null;

            _textBox = new TextBox();
            _textBox.Location = p;
            _textBox.Font = BuildFont();
            _textBox.ForeColor = _drawColor;
            _textBox.BackColor = Color.White;
            _textBox.BorderStyle = BorderStyle.FixedSingle;
            _textBox.Multiline = true;
            _textBox.Width = 200;
            _textBox.Height = 30;
            _textBox.AcceptsReturn = true;

            _textBox.KeyDown += delegate(object s, KeyEventArgs ev)
            {
                if (ev.KeyCode == Keys.Enter && !ev.Control)
                {
                    CommitText();
                    ev.SuppressKeyPress = true;
                }
                else if (ev.KeyCode == Keys.Escape)
                {
                    CancelText();
                    ev.SuppressKeyPress = true;
                }
            };

            _textBox.LostFocus += delegate
            {
                // 延迟检查：焦点转移到工具栏控件时不提交，也不抢焦点
                // （让工具栏按钮的Click事件正常触发，SelectTool中会CommitText）
                BeginInvoke((Action)delegate
                {
                    if (_textBox == null) return;
                    // Check if focus is now on the toolbar (slider, color palette, tool buttons, etc.)
                    bool focusOnToolbar = false;
                    if (_toolbar != null && _toolbar.Visible)
                    {
                        Control walk = this.ActiveControl;
                        while (walk != null)
                        {
                            if (walk == _toolbar) { focusOnToolbar = true; break; }
                            walk = walk.Parent;
                        }
                    }
                    if (focusOnToolbar)
                        return; // 不提交，不抢焦点，让按钮Click事件正常处理
                    CommitText();
                });
            };

            this.Controls.Add(_textBox);
            _textBox.BringToFront();
            // 确保工具栏在TextBox之上，否则TextBox会遮挡工具栏按钮的点击
            if (_toolbar != null) _toolbar.BringToFront();
            _textBox.Focus();
        }

        private void EditTextAnnotation(TextAnnotation ta)
        {
            _state = OverlayState.AnnotationEditing;
            _editingAnnotation = ta;
            _selectedAnnotation = ta;

            // Sync slider state from annotation's font
            _fontSize = ta.Font.Size;
            SyncSizeSlider();

            _textBox = new TextBox();
            _textBox.Location = ta.Position;
            _textBox.Text = ta.Text;
            _textBox.Font = (Font)ta.Font.Clone();
            _textBox.ForeColor = ta.Color;
            _textBox.BackColor = Color.White;
            _textBox.BorderStyle = BorderStyle.FixedSingle;
            _textBox.Multiline = true;
            // Auto-size to text
            using (Graphics g = this.CreateGraphics())
            {
                SizeF sz = g.MeasureString(ta.Text, ta.Font);
                _textBox.Width = Math.Max(200, (int)sz.Width + 20);
                _textBox.Height = Math.Max(30, (int)sz.Height + 10);
            }
            _textBox.AcceptsReturn = true;
            _textBox.SelectAll();

            _textBox.KeyDown += delegate(object s, KeyEventArgs ev)
            {
                if (ev.KeyCode == Keys.Enter && !ev.Control)
                {
                    CommitText();
                    ev.SuppressKeyPress = true;
                }
                else if (ev.KeyCode == Keys.Escape)
                {
                    CancelText();
                    ev.SuppressKeyPress = true;
                }
            };

            _textBox.LostFocus += delegate
            {
                BeginInvoke((Action)delegate
                {
                    if (_textBox == null) return;
                    // Check if focus moved to toolbar (slider, buttons, etc.)
                    bool focusOnToolbar = false;
                    if (_toolbar != null && _toolbar.Visible)
                    {
                        Control walk = this.ActiveControl;
                        while (walk != null)
                        {
                            if (walk == _toolbar) { focusOnToolbar = true; break; }
                            walk = walk.Parent;
                        }
                    }
                    if (focusOnToolbar)
                        return; // 不提交，不抢焦点，让按钮Click正常触发
                    CommitText();
                });
            };

            this.Controls.Add(_textBox);
            _textBox.BringToFront();
            // 确保工具栏在TextBox之上，否则TextBox会遮挡工具栏按钮的点击
            if (_toolbar != null) _toolbar.BringToFront();
            _textBox.Focus();
        }

        private void CommitText()
        {
            if (_textBox == null) return;

            if (_editingAnnotation != null)
            {
                // Editing existing annotation
                if (string.IsNullOrEmpty(_textBox.Text))
                {
                    _annotations.Remove(_editingAnnotation);
                }
                else
                {
                    _editingAnnotation.Text = _textBox.Text;
                    _editingAnnotation.Position = _textBox.Location;
                    _editingAnnotation.Font = (Font)_textBox.Font.Clone();
                    _editingAnnotation.Color = _textBox.ForeColor;
                }
                _editingAnnotation = null;
            }
            else
            {
                // New annotation
                if (!string.IsNullOrEmpty(_textBox.Text))
                {
                    TextAnnotation ta = new TextAnnotation(BuildFont());
                    ta.Position = _textBox.Location;
                    ta.Text = _textBox.Text;
                    ta.Color = _drawColor;
                    _annotations.Add(ta);
                }
            }
            RemoveTextBox();
            _state = OverlayState.Selected;
            this.Invalidate();
        }

        private void CancelText()
        {
            _editingAnnotation = null;
            RemoveTextBox();
            _state = OverlayState.Selected;
            this.Invalidate();
        }

        private void RemoveTextBox()
        {
            if (_textBox != null)
            {
                this.Controls.Remove(_textBox);
                _textBox.Dispose();
                _textBox = null;
            }
        }

        #endregion

        #region Paint

        private void OnPaint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Rectangle client = this.ClientRectangle;

            // 1. Original screenshot
            g.DrawImage(_screenCapture, 0, 0);

            // 2. Dim overlay
            using (SolidBrush dim = new SolidBrush(Color.FromArgb(120, 0, 0, 0)))
                g.FillRectangle(dim, client);

            // 3. Bright selection + annotations
            if (!_selection.IsEmpty && _selection.Width > 0 && _selection.Height > 0)
            {
                Rectangle sel = _selection;
                NormalizeSelection();

                // Bright area
                g.DrawImage(_screenCapture,
                    sel,
                    sel.X, sel.Y, sel.Width, sel.Height,
                    GraphicsUnit.Pixel);

                // Annotations (clipped to selection)
                g.SetClip(sel);
                foreach (Annotation a in _annotations)
                    a.Draw(g, _screenCapture);
                if (_currentAnnotation != null)
                    _currentAnnotation.Draw(g, _screenCapture);

                // Mosaic preview
                if (_state == OverlayState.Drawing && _currentTool == DrawTool.Mosaic)
                {
                    Rectangle mr = MakeRect(_mouseStart, _lastMouse);
                    using (SolidBrush mb = new SolidBrush(Color.FromArgb(150, 180, 180, 180)))
                        g.FillRectangle(mb, mr);
                }
                g.ResetClip();

                // Selection border
                using (Pen bp = new Pen(Color.FromArgb(0, 120, 215), 1))
                {
                    bp.DashStyle = DashStyle.Solid;
                    g.DrawRectangle(bp, sel);
                }

                // Resize handles
                if (_state == OverlayState.Selected || _state == OverlayState.Drawing ||
                    _state == OverlayState.TextInput || _state == OverlayState.AnnotationDragging
                    || _state == OverlayState.AnnotationResizing || _state == OverlayState.AnnotationEditing)
                {
                    Rectangle[] handles = GetHandles();
                    foreach (Rectangle h in handles)
                    {
                        g.FillRectangle(Brushes.White, h);
                        g.DrawRectangle(Pens.DodgerBlue, h);
                    }
                }

                // Selected annotation highlight + handles
                if (_selectedAnnotation != null && (_state == OverlayState.Selected
                    || _state == OverlayState.AnnotationDragging
                    || _state == OverlayState.AnnotationResizing
                    || _state == OverlayState.AnnotationEditing))
                {
                    Rectangle annBounds = _selectedAnnotation.GetBounds();
                    if (annBounds.Width > 0 && annBounds.Height > 0)
                    {
                        // Highlight border
                        using (Pen hp = new Pen(Color.FromArgb(0, 120, 215), 1))
                        {
                            hp.DashStyle = DashStyle.Dash;
                            g.DrawRectangle(hp, annBounds);
                        }

                        // Handles
                        if (_selectedAnnotation.HasHandles)
                        {
                            Rectangle[] annHandles = _selectedAnnotation.GetHandles();
                            foreach (Rectangle h in annHandles)
                            {
                                g.FillRectangle(Brushes.White, h);
                                g.DrawRectangle(Pens.DodgerBlue, h);
                            }
                        }
                    }
                }

                // Dimension text (during selection)
                if (_state == OverlayState.Selecting)
                {
                    string dimText = sel.Width + " x " + sel.Height;
                    using (Font f = new Font("微软雅黑", 10f))
                    using (SolidBrush bg = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
                    using (SolidBrush fg = new SolidBrush(Color.White))
                    {
                        SizeF sz = g.MeasureString(dimText, f);
                        float tx = sel.Right - sz.Width - 4;
                        float ty = sel.Bottom + 4;
                        if (ty + sz.Height > client.Height) ty = sel.Y - sz.Height - 4;
                        g.FillRectangle(bg, tx - 4, ty - 2, sz.Width + 8, sz.Height + 4);
                        g.DrawString(dimText, f, fg, tx, ty);
                    }
                }
            }

            // 4. Cursor info (RGB) during selection
            if (_state == OverlayState.Selecting || _state == OverlayState.Waiting)
            {
                Point mp = PointToClient(MousePosition);
                if (mp.X >= 0 && mp.Y >= 0 && mp.X < _screenCapture.Width && mp.Y < _screenCapture.Height)
                {
                    Color c = _screenCapture.GetPixel(mp.X, mp.Y);
                    string info = "RGB(" + c.R + "," + c.G + "," + c.B + ")";
                    using (Font f = new Font("Consolas", 9f))
                    using (SolidBrush bg = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
                    using (SolidBrush fg = new SolidBrush(Color.White))
                    using (SolidBrush cb = new SolidBrush(c))
                    {
                        SizeF sz = g.MeasureString(info, f);
                        float ix = mp.X + 18;
                        float iy = mp.Y + 18;
                        if (ix + sz.Width + 20 > client.Width) ix = mp.X - sz.Width - 24;
                        if (iy + sz.Height + 10 > client.Height) iy = mp.Y - sz.Height - 18;

                        g.FillRectangle(bg, ix - 4, iy - 2, sz.Width + 22, sz.Height + 6);
                        // Color preview
                        g.FillRectangle(cb, ix, iy + 2, 12, 12);
                        g.DrawRectangle(Pens.White, ix, iy + 2, 12, 12);
                        // Text
                        g.DrawString(info, f, fg, ix + 16, iy + 1);
                    }
                }
            }
        }

        #endregion

        #region Actions

        private void CopyAndClose()
        {
            NormalizeSelection();
            if (_textBox != null) CommitText();
            if (_selection.IsEmpty || _selection.Width <= 0 || _selection.Height <= 0) return;

            Bitmap result = RenderResult();
            try { Clipboard.SetImage(result); }
            catch { }
            this.Close();
        }

        private void Save()
        {
            if (_textBox != null) CommitText();
            NormalizeSelection();
            if (_selection.IsEmpty || _selection.Width <= 0 || _selection.Height <= 0) return;

            Bitmap result = RenderResult();
            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                dlg.Filter = "PNG 图片|*.png|JPEG 图片|*.jpg|BMP 图片|*.bmp";
                dlg.DefaultExt = "png";
                dlg.FileName = "截图_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    ImageFormat fmt = ImageFormat.Png;
                    if (dlg.FileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
                        fmt = ImageFormat.Jpeg;
                    else if (dlg.FileName.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase))
                        fmt = ImageFormat.Bmp;
                    result.Save(dlg.FileName, fmt);
                }
            }
        }

        private void ShowOcrDropdown()
        {
            _ocrMenu.Show(_ocrArrowBtn, new Point(0, _ocrArrowBtn.Height));
        }

        private void ShowOcrModeHelp()
        {
            Form helpForm = new Form();
            helpForm.Text = "OCR 识别模式说明";
            helpForm.Size = new Size(560, 520);
            helpForm.StartPosition = FormStartPosition.CenterParent;
            helpForm.Font = new Font("微软雅黑", 9f);
            helpForm.MaximizeBox = false;
            helpForm.MinimizeBox = false;
            helpForm.FormBorderStyle = FormBorderStyle.FixedDialog;

            TextBox txtHelp = new TextBox();
            txtHelp.Multiline = true;
            txtHelp.ReadOnly = true;
            txtHelp.BackColor = Color.White;
            txtHelp.BorderStyle = BorderStyle.None;
            txtHelp.Font = new Font("微软雅黑", 9.5f);
            txtHelp.Dock = DockStyle.Fill;
            txtHelp.Text =
                "OCR（光学字符识别）使用 Tesseract 引擎，" +
                "不同的「页面分割模式」（PSM）决定了引擎如何分析图片中的文字布局。" +
                "选对模式可以大幅提升识别准确率。\r\n\r\n" +
                "━━━ PSM 3 — 全自动分页 ━━━\r\n" +
                "引擎会把整张图片当作一个完整页面来分析，自动检测文本区域、段落和列。" +
                "适合：完整的文档页面截图、含有多个段落的文章截图。" +
                "如果你截的是一整页文档，这个模式通常效果最好。\r\n\r\n" +
                "━━━ PSM 4 — 单列可变大小文本（默认） ━━━\r\n" +
                "假设图片中只有一列文字，但每段文字大小可以不同。" +
                "适合：文档中的某一段落、聊天记录、竖向排列的文本。" +
                "这是最通用的模式，也是默认选项，大多数情况下都好用。\r\n\r\n" +
                "━━━ PSM 6 — 统一文本块 ━━━\r\n" +
                "假设图片中是一整块格式统一的文字，大小和排版一致。" +
                "适合：表格内容、代码截图、对齐排列的列表。" +
                "如果文字排列很整齐，这个模式比 PSM 4 更准确。\r\n\r\n" +
                "━━━ PSM 7 — 单行文本 ━━━\r\n" +
                "只识别图片中的一行文字，忽略其他内容。" +
                "适合：浏览器地址栏、标题栏、单个输入框、状态栏文字。" +
                "如果图片里只有一行字，用这个模式最精确。\r\n\r\n" +
                "━━━ PSM 8 — 单个词 ━━━\r\n识别图片中的一个单词或短语。" +
                "适合：按钮上的文字、界面标签、图标旁的短文字。" +
                "注意：中文场景下「词」的边界模糊，效果可能不如 PSM 7。\r\n\r\n" +
                "━━━ PSM 11 — 稀疏文本 ━━━\r\n" +
                "不假设任何排版结构，把图片中所有能识别的文字都找出来，" +
                "即使它们零散分布在各个位置。" +
                "适合：软件界面截图、图标文字、弹窗截图等文字散落各处的场景。" +
                "如果其他模式都漏字，试试这个模式。\r\n\r\n" +
                "━━━ 选择建议 ━━━\r\n" +
                "• 不确定选哪个 → PSM 4（默认）\r\n" +
                "• 截了一整页文档 → PSM 3\r\n" +
                "• 只有一行字 → PSM 7\r\n" +
                "• 界面截图，文字到处都是 → PSM 11\r\n" +
                "• 排列整齐的表格/代码 → PSM 6\r\n" +
                "• 只有一个按钮上的词 → PSM 8";

            helpForm.Controls.Add(txtHelp);
            helpForm.ShowDialog(this);
        }

        private string FindTesseractExe()
        {
            // 1. 程序同目录 ocr/tesseract.exe（零安装模式）
            string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ocr", "tesseract.exe");
            if (File.Exists(localPath)) return localPath;

            // 2. 系统 PATH 中查找
            try
            {
                string pathEnv = Environment.GetEnvironmentVariable("PATH");
                if (pathEnv != null)
                {
                    foreach (string dir in pathEnv.Split(';'))
                    {
                        try
                        {
                            string p = Path.Combine(dir.Trim(), "tesseract.exe");
                            if (File.Exists(p)) return p;
                        }
                        catch { }
                    }
                }
            }
            catch { }

            return null;
        }

        private void DoOCR()
        {
            if (_textBox != null) CommitText();
            NormalizeSelection();
            if (_selection.IsEmpty || _selection.Width <= 0 || _selection.Height <= 0) return;

            string tessExe = FindTesseractExe();
            if (tessExe == null)
            {
                MessageBox.Show(this,
                    "未找到 Tesseract OCR，请按以下步骤配置：\n\n" +
                    "第一步：下载 Tesseract\n" +
                    "  64位：github.com/tesseract-ocr/tesseract/releases\n" +
                    "  下载 tesseract-ocr-w64-setup 开头的 exe\n\n" +
                    "第二步：安装时勾选 Chinese Simplified 语言包\n\n" +
                    "第三步：复制文件到程序目录\n" +
                    "  在 截图工具.exe 同目录下创建 ocr 文件夹\n" +
                    "  把 tesseract.exe 复制进去\n" +
                    "  再创建 ocr/tessdata 文件夹\n" +
                    "  把 chi_sim.traineddata 复制进去\n\n" +
                    "完成后再点\"识别\"即可，无需重启。",
                    "OCR 文字识别", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Bitmap result = RenderResult();
            // 图像预处理：放大 + 灰度 + 二值化，提升 OCR 识别率
            Bitmap processed = PreprocessForOcr(result);
            result.Dispose();

            string tempPath = Path.Combine(Path.GetTempPath(), "screenshot_ocr_temp.png");
            processed.Save(tempPath, ImageFormat.Png);
            processed.Dispose();

            try
            {
                // 设置 TESSDATA_PREFIX 指向本地 tessdata 目录
                string tessDir = Path.GetDirectoryName(tessExe);
                string tessdataDir = Path.Combine(tessDir, "tessdata");
                string origPrefix = Environment.GetEnvironmentVariable("TESSDATA_PREFIX");

                // 如果本地 ocr 目录下有 tessdata，优先使用
                if (Directory.Exists(tessdataDir))
                    Environment.SetEnvironmentVariable("TESSDATA_PREFIX", tessdataDir + "\\");

                try
                {
                    string outputPath = Path.Combine(Path.GetTempPath(), "screenshot_ocr_out");
                    Process p = new Process();
                    p.StartInfo.FileName = tessExe;
                    p.StartInfo.Arguments = "\"" + tempPath + "\" \"" + outputPath + "\" -l chi_sim+eng --psm " + _ocrPsm + " --oem 1";
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.CreateNoWindow = true;
                    p.Start();
                    p.WaitForExit(30000);

                    if (p.ExitCode == 0)
                    {
                        string txtFile = outputPath + ".txt";
                        if (File.Exists(txtFile))
                        {
                            string text = File.ReadAllText(txtFile, System.Text.Encoding.UTF8).Trim();
                            File.Delete(txtFile);
                            text = CleanOcrText(text);
                            if (!string.IsNullOrEmpty(text))
                            {
                                ShowOcrResult(text);
                            }
                            else
                            {
                                MessageBox.Show(this, "未能识别到文字内容",
                                    "OCR 文字识别", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show(this,
                            "OCR 识别失败，可能原因：\n" +
                            "1. 缺少中文语言包 chi_sim.traineddata\n" +
                            "2. tessdata 目录位置不正确\n\n" +
                            "请确认 ocr/tessdata/chi_sim.traineddata 文件存在",
                            "OCR 文字识别", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                finally
                {
                    // 还原环境变量
                    if (origPrefix != null)
                        Environment.SetEnvironmentVariable("TESSDATA_PREFIX", origPrefix);
                    else
                        Environment.SetEnvironmentVariable("TESSDATA_PREFIX", null);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    "OCR 运行出错：" + ex.Message,
                    "OCR 文字识别", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
            }
        }

        private void ShowOcrResult(string text)
        {
            // 计算窗口位置：水平方向优先右侧，垂直方向优先下方，空间不足则反向
            Rectangle selScreen = this.RectangleToScreen(_selection);
            int winW = 480;
            int winH = 400;
            int x, y;

            // 水平：优先放右侧
            if (selScreen.Right + winW <= SystemInformation.VirtualScreen.Right)
                x = selScreen.Right + 8;
            // 否则放左侧
            else if (selScreen.Left - winW - 8 >= SystemInformation.VirtualScreen.Left)
                x = selScreen.Left - winW - 8;
            // 都放不下就重叠居中
            else
                x = selScreen.X + (selScreen.Width - winW) / 2;

            // 垂直：优先放下方
            if (selScreen.Bottom + winH <= SystemInformation.VirtualScreen.Bottom)
                y = selScreen.Bottom + 8;
            // 下方放不下就放上方
            else if (selScreen.Top - winH - 8 >= SystemInformation.VirtualScreen.Top)
                y = selScreen.Top - winH - 8;
            // 都放不下就对齐顶部
            else
                y = Math.Max(SystemInformation.VirtualScreen.Top, selScreen.Top);

            Form frm = new Form();
            frm.Text = "OCR 识别结果";
            frm.Size = new Size(winW, winH);
            frm.StartPosition = FormStartPosition.Manual;
            frm.Location = new Point(x, y);
            frm.Font = new Font("微软雅黑", 9f);
            // TopMost保证在截图覆盖层之上，ShowInTaskbar=false避免Alt+Tab残留
            frm.TopMost = true;
            frm.ShowInTaskbar = false;
            // 关闭时Dispose
            frm.FormClosed += delegate { frm.Dispose(); };

            TextBox txtBox = new TextBox();
            txtBox.Multiline = true;
            txtBox.ReadOnly = true;
            txtBox.Text = text;
            txtBox.Font = new Font("微软雅黑", 10f);
            txtBox.ScrollBars = ScrollBars.Both;
            txtBox.WordWrap = true;
            txtBox.Dock = DockStyle.Fill;
            txtBox.BorderStyle = BorderStyle.None;
            txtBox.BackColor = Color.White;
            txtBox.Select(0, 0); // 不默认全选

            frm.Controls.Add(txtBox);
            frm.Show();
        }

        private Bitmap RenderResult()
        {
            NormalizeSelection();
            Bitmap bmp = new Bitmap(_selection.Width, _selection.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.DrawImage(_screenCapture,
                    new Rectangle(0, 0, _selection.Width, _selection.Height),
                    _selection.X, _selection.Y, _selection.Width, _selection.Height,
                    GraphicsUnit.Pixel);

                g.TranslateTransform(-_selection.X, -_selection.Y);
                g.SetClip(_selection);
                foreach (Annotation a in _annotations)
                    a.Draw(g, _screenCapture);
            }
            return bmp;
        }

        private static Bitmap PreprocessForOcr(Bitmap src)
        {
            // 1. 放大2倍（Tesseract 对大字号识别更好，至少 30px 以上）
            int newW = src.Width * 2;
            int newH = src.Height * 2;
            Bitmap scaled = new Bitmap(newW, newH, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            using (Graphics g = Graphics.FromImage(scaled))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(src, 0, 0, newW, newH);
            }

            // 2. 灰度化（保留所有细节，不做二值化避免丢失小标点）
            Bitmap gray = new Bitmap(newW, newH, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            for (int y = 0; y < newH; y++)
            {
                for (int x = 0; x < newW; x++)
                {
                    Color c = scaled.GetPixel(x, y);
                    int grayVal = (int)(0.299 * c.R + 0.587 * c.G + 0.114 * c.B);
                    gray.SetPixel(x, y, Color.FromArgb(grayVal, grayVal, grayVal));
                }
            }

            scaled.Dispose();
            return gray;
        }

        private static string CleanOcrText(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            // 1. 中文字符之间的空格去掉（Tesseract 常见误识别）
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == ' ')
                {
                    bool prevCjk = (i > 0 && IsCjkOrFullwidth(text[i - 1]));
                    bool nextCjk = (i + 1 < text.Length && IsCjkOrFullwidth(text[i + 1]));
                    if (prevCjk && nextCjk)
                        continue;
                }
                sb.Append(c);
            }
            text = sb.ToString();

            // 2. 多个连续空格合并为一个
            while (text.Contains("  "))
                text = text.Replace("  ", " ");

            // 3. 逐行处理：过滤垃圾行 + 行尾空格清理
            string[] lines = text.Split(new char[] { '\n' });
            System.Collections.ArrayList goodLines = new System.Collections.ArrayList();
            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();
                if (string.IsNullOrEmpty(line)) continue;

                // 过滤：全是特殊字符/符号，没有可读字符
                if (!HasReadableChar(line)) continue;

                // 过滤：单字符行且不是常见标点（图标常被识别成单个乱码字符）
                if (line.Length == 1 && !IsLikelyPunctuation(line[0])) continue;

                goodLines.Add(line);
            }

            if (goodLines.Count == 0) return "";
            string[] result = (string[])goodLines.ToArray(typeof(string));
            return string.Join("\n", result);
        }

        private static bool HasReadableChar(string line)
        {
            int cjkCount = 0;
            int letterCount = 0;
            int digitCount = 0;
            int otherCount = 0;

            foreach (char c in line)
            {
                if (c >= 0x4E00 && c <= 0x9FFF) cjkCount++;
                else if (c >= 0x3400 && c <= 0x4DBF) cjkCount++;
                else if (c >= 'a' && c <= 'z') letterCount++;
                else if (c >= 'A' && c <= 'Z') letterCount++;
                else if (c >= '0' && c <= '9') digitCount++;
                else otherCount++;
            }

            int readableCount = cjkCount + letterCount + digitCount;
            // 可读字符占比必须超过 30%，且至少有 1 个可读字符
            if (readableCount == 0) return false;
            return readableCount * 100 / line.Length >= 30;
        }

        private static bool IsLikelyPunctuation(char c)
        {
            // 常见可能单独出现的标点
            if (c == ',' || c == '.' || c == '?' || c == '!') return true;
            if (c == '\u3001' || c == '\u3002' || c == '\uFF01' || c == '\uFF1F') return true;
            return false;
        }

        private static bool IsCjkOrFullwidth(char c)
        {
            // CJK 统一汉字
            if (c >= 0x4E00 && c <= 0x9FFF) return true;
            // CJK 扩展A
            if (c >= 0x3400 && c <= 0x4DBF) return true;
            // 全角标点
            if (c >= 0xFF00 && c <= 0xFFEF) return true;
            // 中文标点范围
            if (c >= 0x3000 && c <= 0x303F) return true;
            // CJK 兼容
            if (c >= 0xF900 && c <= 0xFAFF) return true;
            return false;
        }

        private void Undo()
        {
            if (_textBox != null) CommitText();
            if (_annotations.Count > 0)
            {
                _annotations.RemoveAt(_annotations.Count - 1);
                _selectedAnnotation = null;
                this.Invalidate();
            }
        }

        private void Cancel()
        {
            this.Close();
        }

        #endregion

        #region Helpers

        static Rectangle MakeRect(Point a, Point b)
        {
            return new Rectangle(
                Math.Min(a.X, b.X), Math.Min(a.Y, b.Y),
                Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            RemoveTextBox();
            if (_screenCapture != null) { _screenCapture.Dispose(); _screenCapture = null; }
        }

        #endregion
    }

    #endregion

    #region Global Hotkey

    class GlobalHotkey
    {
        [DllImport("user32.dll")]
        static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private IntPtr _handle;
        private int _id;
        public event Action HotkeyPressed;

        const int WM_HOTKEY = 0x0312;

        public GlobalHotkey(IntPtr handle, int id, uint modifiers, uint key)
        {
            _handle = handle;
            _id = id;
            RegisterHotKey(_handle, _id, modifiers, key);
        }

        public void ProcessMessage(ref Message m)
        {
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == _id)
            {
                if (HotkeyPressed != null) HotkeyPressed();
            }
        }

        public void Unregister()
        {
            UnregisterHotKey(_handle, _id);
        }
    }

    #endregion

    #region Hotkey Dialog

    class HotkeyDialog : Form
    {
        private Label _prompt;
        private TextBox _display;
        private Label _note;
        private Button _btnReset;
        private Button _btnOk;
        private Button _btnCancel;

        private uint _modifiers;
        private uint _key;
        private bool _hasCaptured;

        public uint Modifiers { get { return _modifiers; } }
        public uint Key { get { return _key; } }
        public bool HasCaptured { get { return _hasCaptured; } }

        public HotkeyDialog(uint currentMods, uint currentKey)
        {
            this.Text = "设置截图快捷键";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ClientSize = new Size(380, 190);
            this.KeyPreview = true;
            this.Font = new Font("微软雅黑", 9f);

            _prompt = new Label();
            _prompt.Text = "请按下新的快捷键组合：";
            _prompt.Location = new Point(24, 18);
            _prompt.AutoSize = true;

            _display = new TextBox();
            _display.Location = new Point(24, 45);
            _display.Size = new Size(332, 40);
            _display.ReadOnly = true;
            _display.Font = new Font("微软雅黑", 14f, FontStyle.Bold);
            _display.TextAlign = HorizontalAlignment.Center;
            _display.Text = HotkeyToString(currentMods, currentKey);
            _display.BackColor = Color.White;
            _display.TabStop = false;

            _note = new Label();
            _note.Text = "支持 Ctrl+Alt / Alt / Ctrl / Shift + 字母/数字/F1-F12";
            _note.Location = new Point(24, 88);
            _note.AutoSize = true;
            _note.ForeColor = Color.Gray;

            _btnReset = new Button();
            _btnReset.Text = "恢复默认";
            _btnReset.Size = new Size(85, 30);
            _btnReset.Location = new Point(24, 140);

            _btnOk = new Button();
            _btnOk.Text = "确定";
            _btnOk.Size = new Size(85, 30);
            _btnOk.Location = new Point(185, 140);
            _btnOk.Enabled = false;
            _btnOk.DialogResult = DialogResult.OK;

            _btnCancel = new Button();
            _btnCancel.Text = "取消";
            _btnCancel.Size = new Size(85, 30);
            _btnCancel.Location = new Point(276, 140);
            _btnCancel.DialogResult = DialogResult.Cancel;

            this.Controls.Add(_prompt);
            this.Controls.Add(_display);
            this.Controls.Add(_note);
            this.Controls.Add(_btnReset);
            this.Controls.Add(_btnOk);
            this.Controls.Add(_btnCancel);

            this.AcceptButton = _btnOk;
            this.CancelButton = _btnCancel;

            this.KeyDown += CaptureKey;

            _btnReset.Click += delegate
            {
                _modifiers = 0x0002; // MOD_CTRL
                _key = (uint)Keys.Q;
                _hasCaptured = true;
                _display.Text = HotkeyToString(_modifiers, _key);
                _btnOk.Enabled = true;
            };
        }

        private void CaptureKey(object sender, KeyEventArgs e)
        {
            uint mods = 0;
            if (e.Alt) mods |= 0x0001;
            if (e.Control) mods |= 0x0002;
            if (e.Shift) mods |= 0x0004;

            uint vk = (uint)e.KeyCode;

            // 忽略单独的修饰键（等待组合键的第二下）
            if (e.KeyCode == Keys.Menu || e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.ShiftKey)
            {
                e.SuppressKeyPress = true;
                return;
            }

            // 至少需要一个修饰键（功能键除外）
            bool isFuncKey = (vk >= (uint)Keys.F1 && vk <= (uint)Keys.F12);
            if (mods == 0 && !isFuncKey)
                return; // 不拦截，让对话框正常导航（Tab/Enter/Esc）

            // 有效快捷键组合 — 捕获
            e.SuppressKeyPress = true;
            _modifiers = mods;
            _key = vk;
            _hasCaptured = true;
            _display.Text = HotkeyToString(mods, vk);
            _btnOk.Enabled = true;
        }

        public static string HotkeyToString(uint modifiers, uint key)
        {
            string s = "";
            if ((modifiers & 0x0002) != 0) s += "Ctrl + ";
            if ((modifiers & 0x0001) != 0) s += "Alt + ";
            if ((modifiers & 0x0004) != 0) s += "Shift + ";
            s += ((Keys)key).ToString();
            return s;
        }
    }

    #endregion

    #region Tray Application

    class TrayApp : Form
    {
        private NotifyIcon _trayIcon;
        private GlobalHotkey _hotkey1;
        private GlobalHotkey _hotkey2; // F1 固定备用
        private ScreenshotOverlay _overlay;
        private bool _closing = false;
        private uint _hotkey1Mods = 0x0002; // MOD_CTRL
        private uint _hotkey1Key = (uint)Keys.Q;

        public TrayApp()
        {
            // 加载保存的快捷键配置
            LoadHotkeyConfig();

            this.ShowInTaskbar = false;
            this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            this.Size = new Size(0, 0);
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(-32000, -32000);
            this.Opacity = 0;

            // System tray icon
            string hkName = HotkeyDialog.HotkeyToString(_hotkey1Mods, _hotkey1Key);
            _trayIcon = new NotifyIcon();
            _trayIcon.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            _trayIcon.Text = "截图工具 - " + hkName + " / F1";
            _trayIcon.Visible = true;

            ContextMenu menu = new ContextMenu();
            menu.MenuItems.Add("截图 (" + hkName + ")", delegate { StartScreenshot(); });
            menu.MenuItems.Add("设置快捷键...", delegate { ShowHotkeyDialog(); });
            menu.MenuItems.Add("-");
            menu.MenuItems.Add("退出", delegate { Application.Exit(); });
            _trayIcon.ContextMenu = menu;
            _trayIcon.DoubleClick += delegate { StartScreenshot(); };

            // 延迟注册热键，等窗口句柄创建完毕
            this.HandleCreated += delegate { RegisterHotkeys(); };
        }

        // 重写CreateParams加上WS_EX_TOOLWINDOW，让Windows不把主窗口列入Alt+Tab
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW
                return cp;
            }
        }

        private void RegisterHotkeys()
        {
            if (this.IsHandleCreated)
            {
                try
                {
                    _hotkey1 = new GlobalHotkey(this.Handle, 1, _hotkey1Mods, _hotkey1Key);
                    _hotkey1.HotkeyPressed += StartScreenshot;
                    _hotkey2 = new GlobalHotkey(this.Handle, 2, 0x0000 /*NONE*/, (uint)Keys.F1);
                    _hotkey2.HotkeyPressed += StartScreenshot;
                }
                catch { }
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (_closing) { base.WndProc(ref m); return; }
            try
            {
                if (_hotkey1 != null) _hotkey1.ProcessMessage(ref m);
                if (_hotkey2 != null) _hotkey2.ProcessMessage(ref m);
            }
            catch { }
            base.WndProc(ref m);
        }

        private string GetConfigPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hotkey.cfg");
        }

        private void LoadHotkeyConfig()
        {
            try
            {
                string path = GetConfigPath();
                if (!File.Exists(path)) return;
                string content = File.ReadAllText(path, Encoding.UTF8).Trim();
                string[] parts = content.Split(',');
                if (parts.Length == 2)
                {
                    _hotkey1Mods = uint.Parse(parts[0]);
                    _hotkey1Key = uint.Parse(parts[1]);
                }
            }
            catch { }
        }

        private void SaveHotkeyConfig()
        {
            try
            {
                File.WriteAllText(GetConfigPath(), _hotkey1Mods + "," + _hotkey1Key, Encoding.UTF8);
            }
            catch { }
        }

        private void UpdateTrayText()
        {
            string hkName = HotkeyDialog.HotkeyToString(_hotkey1Mods, _hotkey1Key);
            if (_trayIcon != null)
                _trayIcon.Text = "截图工具 - " + hkName + " / F1";
            if (_trayIcon != null && _trayIcon.ContextMenu != null
                && _trayIcon.ContextMenu.MenuItems.Count > 0)
                _trayIcon.ContextMenu.MenuItems[0].Text = "截图 (" + hkName + ")";
        }

        private void ShowHotkeyDialog()
        {
            using (HotkeyDialog dlg = new HotkeyDialog(_hotkey1Mods, _hotkey1Key))
            {
                if (dlg.ShowDialog() == DialogResult.OK && dlg.HasCaptured)
                {
                    // 注销旧快捷键
                    if (_hotkey1 != null) { _hotkey1.Unregister(); _hotkey1 = null; }

                    uint newMods = dlg.Modifiers;
                    uint newKey = dlg.Key;

                    // 尝试注册新快捷键
                    try
                    {
                        _hotkey1 = new GlobalHotkey(this.Handle, 1, newMods, newKey);
                        _hotkey1.HotkeyPressed += StartScreenshot;
                        _hotkey1Mods = newMods;
                        _hotkey1Key = newKey;
                    }
                    catch
                    {
                        MessageBox.Show("快捷键注册失败，可能已被其他程序占用。\n已恢复默认快捷键 Ctrl+Q。",
                            "设置快捷键", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        // 回退到默认
                        newMods = 0x0002;
                        newKey = (uint)Keys.Q;
                        try
                        {
                            _hotkey1 = new GlobalHotkey(this.Handle, 1, newMods, newKey);
                            _hotkey1.HotkeyPressed += StartScreenshot;
                            _hotkey1Mods = newMods;
                            _hotkey1Key = newKey;
                        }
                        catch { }
                    }

                    SaveHotkeyConfig();
                    UpdateTrayText();
                }
            }
        }

        private void StartScreenshot()
        {
            if (_overlay != null) return;
            _overlay = new ScreenshotOverlay();
            _overlay.FormClosed += delegate { _overlay = null; };
            _overlay.Show();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _closing = true;
            if (_hotkey1 != null) { _hotkey1.Unregister(); _hotkey1 = null; }
            if (_hotkey2 != null) { _hotkey2.Unregister(); _hotkey2 = null; }
            if (_trayIcon != null) { _trayIcon.Visible = false; _trayIcon.Dispose(); _trayIcon = null; }
            base.OnFormClosing(e);
        }
    }

    #endregion

    static class Program
    {
        [DllImport("user32.dll")]
        static extern bool SetProcessDPIAware();

        [STAThread]
        static void Main()
        {
            // 声明 DPI 感知，让系统返回真实物理像素而非逻辑像素
            try { SetProcessDPIAware(); } catch { }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TrayApp());
        }
    }
}
