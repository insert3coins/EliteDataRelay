using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using EliteDataRelay.Configuration;
using EliteDataRelay.Models;
using EliteDataRelay.Services;

namespace EliteDataRelay.UI
{
    public partial class OverlayForm
    {
        /// <summary>
        /// Updates jump overlay data and marks frame as stale.
        /// </summary>
        public void UpdateJumpOverlay(NextJumpOverlayData? data)
        {
            if (_position != OverlayPosition.JumpInfo) return;

            _currentJumpData = data;
            _stale = true;

            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateJumpOverlay(data)));
                return;
            }

            _renderPanel?.Invalidate();
        }

        private void OnJumpPanelPaint(object? sender, PaintEventArgs e)
        {
            if (_renderPanel == null) return;

            try
            {
                if (_stale || _frameCache == null)
                {
                    RenderJumpFrame();
                    _stale = false;
                }

                if (_frameCache != null)
                {
                    e.Graphics.DrawImageUnscaled(_frameCache, 0, 0);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OverlayForm.Jump] Paint error: {ex.Message}");
            }
        }

        /// <summary>
        /// Renders the Next Jump overlay (SrvSurvey-style) with target, route, traffic, and hop summaries.
        /// </summary>
        private void RenderJumpFrame()
        {
            if (_renderPanel == null) return;

            int width = _renderPanel.Width;
            int height = _renderPanel.Height;
            if (width <= 0 || height <= 0) return;

            _frameCache?.Dispose();
            _frameCache = new Bitmap(width, height);

            using (Graphics g = Graphics.FromImage(_frameCache))
            {
                GameColors.ConfigureHighQuality(g);
                g.Clear(Color.Transparent);

                var rect = new Rectangle(0, 0, width - 1, height - 1);
                using (var path = DrawingUtils.CreateRoundedRectPath(rect, 12))
                using (var bgBrush = new SolidBrush(GameColors.BackgroundDark))
                using (var borderPen = GameColors.PenBorder2)
                {
                    g.FillPath(bgBrush, path);
                    if (AppConfiguration.OverlayShowBorderJump)
                    {
                        g.DrawPath(borderPen, path);
                    }
                }

                int padding = 12;
                int y = padding;
                var data = _currentJumpData;

                // Target system header
                string target = string.IsNullOrWhiteSpace(data?.TargetSystemName) ? "Awaiting next jump" : data.TargetSystemName!;
                target = TruncateText(g, target, GameColors.FontHeader, width - (padding * 2));
                g.DrawString(target, GameColors.FontHeader, GameColors.BrushOrange, padding, y);
                y += GameColors.FontHeader.Height + 4;

                // Metric row: class, progress, jumps left, next distance, remaining ly
                int pillX = padding;
                int lineHeight = GameColors.FontSmall.Height + 6;
                // Prefer explicit star class; fall back to first hop star class to avoid "?" badges.
                string? resolvedStarClass = data?.StarClass;
                if (string.IsNullOrWhiteSpace(resolvedStarClass))
                {
                    var firstHop = data?.Hops?.FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(firstHop?.StarClass))
                    {
                        resolvedStarClass = firstHop!.StarClass;
                    }
                }

                var starInfo = StarClassHelper.FromCode(resolvedStarClass);
                string starLabel = string.IsNullOrWhiteSpace(resolvedStarClass) ? "CLASS ?" : resolvedStarClass!;
                Color starColor = ChooseStarColor(starInfo);
                pillX += DrawPill(g, starLabel, pillX, y, starColor) + 6;

                if (data != null)
                {
                    int? totalSystems = data.TotalJumps;
                    int? totalJumps = totalSystems.HasValue ? Math.Max(0, totalSystems.Value - 1) : (int?)null; // NavRoute total includes current system
                    int? currentIndex = (data.CurrentJumpIndex.HasValue && data.CurrentJumpIndex.Value >= 0) ? data.CurrentJumpIndex : null;
                    int? currentJumpNumber = null;
                    if (currentIndex.HasValue)
                    {
                        currentJumpNumber = Math.Max(1, currentIndex.Value + 1);
                    }
                    else if (totalJumps.HasValue && data.RemainingJumps.HasValue)
                    {
                        currentJumpNumber = Math.Max(1, totalJumps.Value - data.RemainingJumps.Value);
                    }

                    int? remainingJumps = data.RemainingJumps;
                    if (!remainingJumps.HasValue && totalJumps.HasValue && currentJumpNumber.HasValue)
                    {
                        remainingJumps = Math.Max(0, totalJumps.Value - currentJumpNumber.Value);
                    }

                    if (totalJumps.HasValue && currentJumpNumber.HasValue)
                    {
                        pillX += DrawPill(g, $"Jump {currentJumpNumber}/{totalJumps.Value}", pillX, y, GameColors.Orange) + 6;
                    }
                    else if (currentIndex.HasValue || totalSystems.HasValue)
                    {
                        int current = currentIndex.HasValue ? currentIndex.Value + 1 : 1;
                        int total = totalSystems ?? current;
                        pillX += DrawPill(g, $"Jump {current}/{total}", pillX, y, GameColors.Orange) + 6;
                    }

                    if (AppConfiguration.ShowNextJumpJumpsLeft && remainingJumps.HasValue)
                    {
                        pillX += DrawPill(g, $"{remainingJumps.Value} left", pillX, y, GameColors.GrayText) + 6;
                    }

                    double? nextLy = data.NextDistanceLy ?? data.JumpDistanceLy;
                    if (nextLy.HasValue)
                    {
                        pillX += DrawPill(g, $"{nextLy.Value:0.0} ly", pillX, y, GameColors.BrushWhite.Color) + 6;
                    }

                    if (data.TotalRemainingLy.HasValue)
                    {
                        DrawPill(g, $"{data.TotalRemainingLy.Value:0.0} ly remain", pillX, y, GameColors.GrayText);
                    }
                }
                else
                {
                    DrawPill(g, "No route", pillX, y, GameColors.GrayText);
                }
                y += lineHeight;

                // Upcoming hops preview
                if (data?.Hops?.Any() == true)
                {
                    int maxHops = 4;
                    int count = Math.Min(maxHops, data.Hops.Count);
                    for (int i = 0; i < count; i++)
                    {
                        var hop = data.Hops[i];
                        var hopInfo = StarClassHelper.FromCode(hop.StarClass);
                        Color hopColor = ChooseStarColor(hopInfo);
                        string hopText = hop.Name;
                        if (hop.DistanceLy.HasValue) hopText += $" ({hop.DistanceLy.Value:0.0} ly)";
                        hopText = TruncateText(g, hopText, GameColors.FontSmall, width - (padding * 2));
                        using (var brush = new SolidBrush(hopColor))
                        {
                            g.DrawString($"-> {hopText}", GameColors.FontSmall, brush, padding, y);
                        }
                        y += GameColors.FontSmall.Height + 1;
                    }

                    if (data.Hops.Count > maxHops)
                    {
                        g.DrawString($"+{data.Hops.Count - maxHops} more", GameColors.FontSmall, GameColors.BrushGrayText, padding, y);
                        y += GameColors.FontSmall.Height + 1;
                    }
                }
                else
                {
                    g.DrawString("Waiting for NavRoute / FSD charge...", GameColors.FontSmall, GameColors.BrushGrayText, padding, y);
                }
            }
        }

        private int DrawPill(Graphics g, string text, int x, int y, Color tint)
        {
            var size = g.MeasureString(text, GameColors.FontSmall);
            int width = (int)Math.Ceiling(size.Width) + 10;
            int height = (int)Math.Ceiling(size.Height) + 4;

            var rect = new Rectangle(x, y, width, height);
            using (var bg = new SolidBrush(Color.FromArgb(70, tint)))
            using (var pen = new Pen(Color.FromArgb(120, tint), 1f))
            using (var textBrush = new SolidBrush(tint))
            {
                g.FillRectangle(bg, rect);
                g.DrawRectangle(pen, rect);
                g.DrawString(text, GameColors.FontSmall, textBrush, x + 5, y + 2);
            }

            return width;
        }

        private Color ChooseStarColor(StarClassInfo info)
        {
            if (info.IsHazard) return GameColors.Gold;
            if (info.IsBoostStar) return GameColors.Orange;
            if (info.IsScoopable) return GameColors.Cyan;
            return GameColors.GrayText;
        }
    }
}
