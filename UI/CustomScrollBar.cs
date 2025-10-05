using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace EliteDataRelay.UI

{
    public class CustomScrollBar : Control
    {
        private int _minimum = 0;
        private int _maximum = 100;
        private int _value = 0;
        private int _largeChange = 10;

        private Rectangle _thumbRectangle;
        private bool _isThumbDragging = false;
        private int _dragOffset;

        // Theming
        private Color _thumbColor = Color.FromArgb(99, 102, 241); // Accent
        private Color _trackColor = Color.FromArgb(18, 18, 22); // Primary BG

        public event EventHandler<ScrollEventArgs>? Scroll;

        [DefaultValue(0)]
        public int Minimum
        {
            get => _minimum;
            set { _minimum = value; Invalidate(); UpdateThumb(); }
        }

        [DefaultValue(100)]
        public int Maximum
        {
            get => _maximum;
            set { _maximum = value; Invalidate(); UpdateThumb(); }
        }

        [DefaultValue(0)]
        public int Value
        {
            get => _value;
            set
            {
                int oldValue = _value;
                _value = Math.Max(Minimum, Math.Min(value, Maximum - LargeChange + 1));
                if (_value != oldValue)
                {
                    UpdateThumb();
                    OnScroll(new ScrollEventArgs(ScrollEventType.ThumbPosition, oldValue, _value, ScrollOrientation.VerticalScroll));
                    Invalidate();
                }
            }
        }

        [DefaultValue(10)]
        public int LargeChange
        {
            get => _largeChange;
            set { _largeChange = value; Invalidate(); UpdateThumb(); }
        }

        public CustomScrollBar()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            this.Width = 12;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Draw Track
            using (var trackBrush = new SolidBrush(_trackColor))
            {
                e.Graphics.FillRectangle(trackBrush, ClientRectangle);
            }

            // Draw Thumb if needed
            if (Maximum > LargeChange - 1)
            {
                using (var thumbBrush = new SolidBrush(_thumbColor))
                {
                    e.Graphics.FillRectangle(thumbBrush, _thumbRectangle);
                }
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateThumb();
        }

        private void UpdateThumb()
        {
            if (Maximum <= Minimum || LargeChange > Maximum - Minimum)
            {
                _thumbRectangle = Rectangle.Empty;
                return;
            }

            float trackHeight = ClientSize.Height;
            float thumbHeight = Math.Max(10, trackHeight * LargeChange / (Maximum - Minimum + LargeChange));
            float scrollableRange = trackHeight - thumbHeight;
            float valueRange = Maximum - Minimum - LargeChange + 1;

            float thumbY = (Value - Minimum) / valueRange * scrollableRange;

            _thumbRectangle = new Rectangle(0, (int)thumbY, Width, (int)thumbHeight);
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                if (_thumbRectangle.Contains(e.Location))
                {
                    _isThumbDragging = true;
                    _dragOffset = e.Y - _thumbRectangle.Top;
                }
                else // Clicked on track
                {
                    int newValue = Value;
                    if (e.Y < _thumbRectangle.Top)
                        newValue -= LargeChange; // Page Up
                    else
                        newValue += LargeChange; // Page Down

                    Value = newValue;
                    OnScroll(new ScrollEventArgs(ScrollEventType.LargeDecrement, Value));
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_isThumbDragging)
            {
                float trackHeight = ClientSize.Height;
                float thumbHeight = _thumbRectangle.Height;
                float scrollableRange = trackHeight - thumbHeight;
                float valueRange = Maximum - Minimum - LargeChange + 1;

                float newThumbY = e.Y - _dragOffset;
                newThumbY = Math.Max(0, Math.Min(newThumbY, scrollableRange));

                int newValue = (int)(Minimum + (newThumbY / scrollableRange * valueRange));
                Value = newValue;
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left)
            {
                _isThumbDragging = false;
            }
        }

        protected virtual void OnScroll(ScrollEventArgs e)
        {
            Scroll?.Invoke(this, e);
        }

        public void SetTheme(Color thumbColor, Color trackColor)
        {
            _thumbColor = thumbColor;
            _trackColor = trackColor;
            Invalidate();
        }
    }
}