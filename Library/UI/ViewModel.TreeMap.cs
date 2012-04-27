﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace XLibrary
{
    public partial class ViewModel
    {
        // border between outside/external panels
        public float PanelBorderWidth = 4;
        public float NodeBorderWidth = 4;

        public float LabelPadding = 2;


        public void DrawTreeMap(Graphics buffer, bool showingOutside, bool showingExternal)
        {
            if (DoRevalue ||
                (ShowLayout != ShowNodes.All && XRay.CoverChange) ||
                (ShowLayout == ShowNodes.Instances && XRay.InstanceChange))
            {
                RecalcCover(InternalRoot);
                RecalcCover(ExternalRoot);

                XRay.CoverChange = false;
                XRay.InstanceChange = false;

                DoRevalue = false;
                RevalueCount++;

                DoResize = true;
            }

            if (DoResize)
            {
                var drawArea = new RectangleF(ScreenOffset.X, ScreenOffset.Y, ScreenSize.Width, ScreenSize.Height);

                float offset = 0;
                float centerWidth = drawArea.Width;

                PositionMap.Clear();
                CenterMap.Clear();

                if (showingOutside)
                {
                    offset = drawArea.Width * 1.0f / 4.0f;
                    centerWidth -= offset;

                    InternalRoot.SetArea(new RectangleF(ScreenOffset.X, ScreenOffset.Y, offset - PanelBorderWidth, drawArea.Height));
                    PositionMap[InternalRoot.ID] = InternalRoot;
                    SizeNode(buffer, InternalRoot, CurrentRoot, false);
                }
                if (showingExternal)
                {
                    float extWidth = drawArea.Width * 1.0f / 4.0f;
                    centerWidth -= extWidth;

                    ExternalRoot.SetArea(new RectangleF(ScreenOffset.X + offset + centerWidth + PanelBorderWidth, ScreenOffset.Y, extWidth - PanelBorderWidth, drawArea.Height));
                    PositionMap[ExternalRoot.ID] = ExternalRoot;
                    SizeNode(buffer, ExternalRoot, null, false);
                }

                CurrentRoot.SetArea(new RectangleF(ScreenOffset.X + offset, ScreenOffset.Y, centerWidth, drawArea.Height));
                PositionMap[CurrentRoot.ID] = CurrentRoot;
                SizeNode(buffer, CurrentRoot, null, true);

                DoResize = false;
                ResizeCount++;
            }
        }

        private void SizeNode(Graphics buffer, XNodeIn root, XNodeIn exclude, bool center)
        {
            if (!root.Show)
                return;

            RectangleF insideArea = root.AreaF;

            if (ShowLabels)
            {
                // check if enough room in root box for label
                var labelSpace = root.AreaF;
                labelSpace.Width -= LabelPadding * 2.0f;
                labelSpace.Height -= LabelPadding * 2.0f;

                var labelSize = new RectangleF(root.AreaF.Location, buffer.MeasureString(root.Name, TextFont));
                float minHeight = (root.Nodes.Count > 0) ? labelSize.Height * 2.0f : labelSize.Height;

                if (minHeight < labelSpace.Height && labelSize.Width / 3f < labelSpace.Width)
                {
                    labelSize.X += LabelPadding;
                    labelSize.Y += LabelPadding;

                    if (labelSpace.Width < labelSize.Width)
                    {
                        root.LabelClipped = true;
                        labelSize.Width = labelSpace.Width;
                    }

                    insideArea.Y += labelSize.Height;
                    insideArea.Height -= labelSize.Height;

                    root.RoomForLabel = true;
                    root.LabelRect = labelSize;
                }
            }

            List<Sector> sectors = new TreeMap(root, exclude, insideArea.Size).Results;

            foreach (Sector sector in sectors)
            {
                XNodeIn node = sector.OriginalValue as XNodeIn;

                sector.Rect = RectangleExtensions.Contract(sector.Rect, NodeBorderWidth);

                if (sector.Rect.X < NodeBorderWidth) sector.Rect.X = NodeBorderWidth;
                if (sector.Rect.Y < NodeBorderWidth) sector.Rect.Y = NodeBorderWidth;
                if (sector.Rect.X > insideArea.Width - NodeBorderWidth) sector.Rect.X = insideArea.Width - NodeBorderWidth;
                if (sector.Rect.Y > insideArea.Height - NodeBorderWidth) sector.Rect.Y = insideArea.Height - NodeBorderWidth;

                sector.Rect.X += insideArea.X;
                sector.Rect.Y += insideArea.Y;

                node.SetArea(sector.Rect);
                PositionMap[node.ID] = node;

                node.RoomForLabel = false; // cant do above without graphic artifacts
                node.LabelClipped = false;

                if (center)
                    CenterMap[node.ID] = node;

                if (sector.Rect.Width > 1.0f && sector.Rect.Height > 1.0f)
                    SizeNode(buffer, node, exclude, center);
            }
        }
    }
}