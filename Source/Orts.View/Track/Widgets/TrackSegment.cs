﻿
using System;

using Microsoft.Xna.Framework;

using Orts.Common.Position;
using Orts.Formats.Msts.Models;
using Orts.View.Track.Shapes;

namespace Orts.View.Track.Widgets
{
    internal class TrackSegment: VectorWidget
    {
        internal readonly bool Curved;
        
        internal readonly float Direction;
        internal readonly float Length;

        internal readonly float Angle;

        [ThreadStatic]
        private static Color color;

        public static void UpdateColor(Color color)
        {
            TrackSegment.color = color;
        }

        public TrackSegment(TrackVectorSection trackVectorSection, TrackSection trackSection)
        {
            ref readonly WorldLocation location = ref trackVectorSection.Location;
            double cosA = Math.Cos(trackVectorSection.Direction.Y);
            double sinA = Math.Sin(trackVectorSection.Direction.Y);

            if (trackSection.Curved)
            {
                Angle = trackSection.Angle;
                Length = trackSection.Radius;

                double length = trackSection.Radius * Math.Abs(MathHelper.ToRadians(trackSection.Angle));

                int sign = -Math.Sign(trackSection.Angle);
                double angleRadians = -length / trackSection.Radius;
                double cosArotated = Math.Cos(trackVectorSection.Direction.Y + sign * angleRadians);
                double sinArotated = Math.Sin(trackVectorSection.Direction.Y + sign * angleRadians);
                double deltaX = sign * trackSection.Radius * (cosA - cosArotated);
                double deltaZ = sign * trackSection.Radius * (sinA - sinArotated);
                base.vector = new PointD(location.TileX * WorldLocation.TileSize + location.Location.X - deltaX, location.TileZ * WorldLocation.TileSize + location.Location.Z + deltaZ);
            }
            else
            {
                Length = trackSection.Length;

                // note, angle is 90 degrees off, and different sign. 
                // So Delta X = cos(90-A)=sin(A); Delta Y,Z = sin(90-A) = cos(A)    
                base.vector = new PointD(location.TileX * WorldLocation.TileSize + location.Location.X + sinA * Length, location.TileZ * WorldLocation.TileSize + location.Location.Z +cosA * Length);
            }

            base.location = PointD.FromWorldLocation(location);
            base.tile = new Tile(location.TileX, location.TileZ);
            Size = trackSection.Width;
            Curved = trackSection.Curved;
            Direction = trackVectorSection.Direction.Y - MathHelper.PiOver2;
        }

        internal override void Draw(ContentArea contentArea, bool highlight = false)
        {
            if (Curved)
                BasicShapes.DrawArc(contentArea.WorldToScreenSize(Size), color, contentArea.WorldToScreenCoordinates(in Location), contentArea.WorldToScreenSize(Length), Direction, Angle, 0, contentArea.SpriteBatch);
            else
                BasicShapes.DrawLine(contentArea.WorldToScreenSize(Size), color, contentArea.WorldToScreenCoordinates(in Location), contentArea.WorldToScreenSize(Length), Direction, contentArea.SpriteBatch);
        }
    }

    internal class RoadTrackSegment : TrackSegment
    {
        [ThreadStatic]
        private static Color color;

        public static void UpdateColor(Color color)
        {
            RoadTrackSegment.color = color;
        }

        public RoadTrackSegment(TrackVectorSection trackVectorSection, TrackSection trackSection) : base(trackVectorSection, trackSection)
        {
        }

        internal override void Draw(ContentArea contentArea, bool highlight = false)
        {
            if (Curved)
                BasicShapes.DrawArc(contentArea.WorldToScreenSize(Size), color, contentArea.WorldToScreenCoordinates(in Location), contentArea.WorldToScreenSize(Length), Direction, Angle, 0, contentArea.SpriteBatch);
            else
                BasicShapes.DrawLine(contentArea.WorldToScreenSize(Size), color, contentArea.WorldToScreenCoordinates(in Location), contentArea.WorldToScreenSize(Length), Direction, contentArea.SpriteBatch);
        }
    }
}
