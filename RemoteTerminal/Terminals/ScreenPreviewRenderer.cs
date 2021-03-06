﻿// Remote Terminal, an SSH/Telnet terminal emulator for Microsoft Windows
// Copyright (C) 2012-2015 Stefan Podskubka
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using CommonDX;
using RemoteTerminal.Screens;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using Matrix = SharpDX.Matrix;

namespace RemoteTerminal.Terminals
{
    /// <summary>
    /// Draws a screen preview using DirectX (SharpDX).
    /// </summary>
    /// <remarks>
    /// This class is meant to be similar to the <see cref="ScreenDisplayRenderer"/> class.
    /// It should draw a small representation of a terminal screen to the <see cref="ScreenPreview"/> class.
    /// However, due to a non-working implementation it isn't used at the moment.
    /// </remarks>
    class ScreenPreviewRenderer : IDisposable
    {
        private readonly Dictionary<Color, Brush> brushes = new Dictionary<Color, Brush>();

        private readonly ScreenPreview screenPreview;
        private readonly IRenderableScreen screen;

        private static readonly Color TerminalBackgroundColor = Color.Black;
        private const string TerminalFontFamily = "Consolas";

        TextFormat textFormatNormal;
        TextFormat textFormatBold;

        private const float CellFontSize = 17.0f / 5f;
        private const float CellWidth = 9.35f / 5f;
        private const float CellHeight = 20.0f / 5f;

        //TextFormat textFormat;

        /// <summary>
        /// Initializes a new instance of <see cref="FpsRenderer"/> class.
        /// </summary>
        public ScreenPreviewRenderer(ScreenPreview screenPreview, IRenderableScreen screen)
        {
            Show = true;
            this.screenPreview = screenPreview;
            this.screen = screen;
        }

        public bool Show { get; set; }

        public virtual void Initialize(DeviceManager deviceManager)
        {
            //deviceManager.ContextDirect2D.TextAntialiasMode = TextAntialiasMode.Grayscale;
            deviceManager.ContextDirect2D.AntialiasMode = AntialiasMode.Aliased;
            this.textFormatNormal = new TextFormat(deviceManager.FactoryDirectWrite, TerminalFontFamily, FontWeight.Normal, FontStyle.Normal, CellFontSize) { TextAlignment = TextAlignment.Leading, ParagraphAlignment = ParagraphAlignment.Center };
            this.textFormatBold = new TextFormat(deviceManager.FactoryDirectWrite, TerminalFontFamily, FontWeight.Bold, FontStyle.Normal, CellFontSize) { TextAlignment = TextAlignment.Leading, ParagraphAlignment = ParagraphAlignment.Center };
        }

        public virtual void Render(TargetBase target)
        {
            if (!Show)
                return;

            IRenderableScreenCopy screenCopy = this.screen.GetScreenCopy();

            var context2D = target.DeviceManager.ContextDirect2D;

            context2D.BeginDraw();
            context2D.Transform = Matrix.Identity;
            context2D.Clear(TerminalBackgroundColor);

            RectangleF rect = new RectangleF();
            var lines = screenCopy.Cells;
            for (int y = 0; y < lines.Count(); y++)
            {
                var cols = lines[y];
                rect.Top = y * CellHeight;
                rect.Bottom = rect.Top + CellHeight;
                for (int x = 0; x < cols.Count(); x++)
                {
                    var cell = cols[x];
                    rect.Left = x * CellWidth;
                    rect.Right = rect.Left + CellWidth;

                    bool isCursor = !screenCopy.CursorHidden && y == screenCopy.CursorRow && x == screenCopy.CursorColumn;
                    this.DrawCell(target, rect, cell, isCursor, screenCopy.HasFocus);
                }
            }

            context2D.EndDraw();
        }

        private void DrawCell(TargetBase target, RectangleF rect, IRenderableScreenCell cell, bool isCursor, bool hasFocus)
        {
            var context2D = target.DeviceManager.ContextDirect2D;

            // 1. Paint background
            {
                Color backgroundColor;
                if (isCursor && hasFocus)
                {
                    var color = this.screenPreview.ColorTheme.ColorTable[ScreenColor.CursorBackground];
                    backgroundColor = new Color(color.R, color.G, color.B, color.A);
                }
                else
                {
                    var color = this.GetColor(cell.BackgroundColor);
                    backgroundColor = new Color(color.R, color.G, color.B, color.A);
                }

                if (backgroundColor != TerminalBackgroundColor)
                {
                    Brush backgroundBrush = GetBrush(context2D, backgroundColor);
                    context2D.FillRectangle(rect, backgroundBrush);
                }
            }

            // 2. Paint border
            {
                if (isCursor && !hasFocus)
                {
                    var color = this.screenPreview.ColorTheme.ColorTable[ScreenColor.CursorBackground];
                    Color borderColor = new Color(color.R, color.G, color.B, color.A);
                    Brush borderBrush = GetBrush(context2D, borderColor);
                    context2D.DrawRectangle(rect, borderBrush);
                }
            }

            // 3. Paint foreground (character)
            {
                Color foregroundColor;
                if (isCursor && hasFocus)
                {
                    var color = this.screenPreview.ColorTheme.ColorTable[ScreenColor.CursorForeground];
                    foregroundColor = new Color(color.R, color.G, color.B, color.A);
                }
                else
                {
                    var color = this.GetColor(cell.ForegroundColor);
                    foregroundColor = new Color(color.R, color.G, color.B, color.A);
                }

                var foregroundBrush = GetBrush(context2D, foregroundColor);

                if (cell.Character != ' ')
                {
                    TextFormat textFormat = this.textFormatNormal;
                    if (cell.Modifications.HasFlag(ScreenCellModifications.Bold))
                    {
                        textFormat = this.textFormatBold;
                    }

                    context2D.DrawText(cell.Character.ToString(), textFormat, rect, foregroundBrush, DrawTextOptions.Clip);
                }

                if (cell.Modifications.HasFlag(ScreenCellModifications.Underline))
                {
                    var point1 = new Vector2(rect.Left, rect.Bottom - 1.0f);
                    var point2 = new Vector2(rect.Right, rect.Bottom - 1.0f);
                    context2D.DrawLine(point1, point2, foregroundBrush);
                }
            }
        }

        private Color GetColor(ScreenColor screenColor)
        {
            var color = this.screenPreview.ColorTheme.ColorTable[screenColor];
            return new Color(color.R, color.G, color.B, color.A);
        }

        private Brush GetBrush(RenderTarget renderTarget, Color color)
        {
            Brush brush;

            lock (this.brushes)
            {
                if (!this.brushes.TryGetValue(color, out brush))
                {
                    brush = new SolidColorBrush(renderTarget, color);
                    this.brushes[color] = brush;
                }
            }

            return brush;
        }

        public void Dispose()
        {
            foreach (var brush in this.brushes.Values)
            {
                brush.Dispose();
            }
        }
    }
}
