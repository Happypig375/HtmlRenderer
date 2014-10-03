﻿//2014 BSD, WinterDev
//ArthurHub

// "Therefore those skilled at the unorthodox
// are infinite as heaven and earth,
// inexhaustible as the great rivers.
// When they come to an end,
// they begin again,
// like the days and months;
// they die and are reborn,
// like the four seasons."
// 
// - Sun Tsu,
// "The Art of War"

using System;
using System.Collections.Generic;
using System.Text;
using LayoutFarm.Drawing;


namespace LayoutFarm
{
    partial class MyCanvas
    {
        void IGraphics.SetCanvasOrigin(float x, float y)
        {
            ReleaseHdc();
            //-----------
            this.gx.TranslateTransform(-this.canvasOriginX, -this.canvasOriginY);
            this.gx.TranslateTransform(x, y);

            this.canvasOriginX = x;
            this.canvasOriginY = y;
        }
        float IGraphics.CanvasOriginX
        {
            get { return this.canvasOriginX; }
        }
        float IGraphics.CanvasOriginY
        {
            get { return this.canvasOriginY; }
        }


        public override void OffsetCanvasOrigin(int dx, int dy)
        {
            internalCanvasOriginX += dx;
            internalCanvasOriginY += dy;
            gx.TranslateTransform(dx, dy);
            currentClipRect.Offset(-dx, -dy);
        }
        public override void OffsetCanvasOriginX(int dx)
        {
            internalCanvasOriginX += dx;
            gx.TranslateTransform(dx, 0);
            currentClipRect.Offset(-dx, 0);
        }
        public override void OffsetCanvasOriginY(int dy)
        {
            internalCanvasOriginY += dy;
            gx.TranslateTransform(0, dy);
            currentClipRect.Offset(0, -dy);
        }

        
      

        /// <summary>
        /// Sets the clipping region of this <see cref="T:System.Drawing.Graphics"/> to the result of the specified operation combining the current clip region and the rectangle specified by a <see cref="T:System.Drawing.RectangleF"/> structure.
        /// </summary>
        /// <param name="rect"><see cref="T:System.Drawing.RectangleF"/> structure to combine. </param>
        /// <param name="combineMode">Member of the <see cref="T:System.Drawing.Drawing2D.CombineMode"/> enumeration that specifies the combining operation to use. </param>
        public override void SetClip(RectangleF rect, CombineMode combineMode = CombineMode.Replace)
        {
            ReleaseHdc();
            gx.SetClip(rect.ToRectF(), (System.Drawing.Drawing2D.CombineMode)combineMode);
        }
        public override bool IntersectsWith(InternalRect clientRect)
        {
            return clientRect.IntersectsWith(left, top, right, bottom);
        }
        public override bool PushClipAreaForNativeScrollableElement(InternalRect updateArea)
        {

            clipRectStack.Push(currentClipRect);

            System.Drawing.Rectangle intersectResult = System.Drawing.Rectangle.Intersect(
                currentClipRect,
                updateArea.ToRectangle().ToRect());

            if (intersectResult.Width <= 0 || intersectResult.Height <= 0)
            {
                currentClipRect = intersectResult;
                return false;
            }

            gx.SetClip(intersectResult);
            currentClipRect = intersectResult;
            return true;
        }


        public override bool PushClipArea(int width, int height, InternalRect updateArea)
        {
            clipRectStack.Push(currentClipRect);

            System.Drawing.Rectangle intersectResult =
                System.Drawing.Rectangle.Intersect(
                    currentClipRect,
                    System.Drawing.Rectangle.Intersect(
                    updateArea.ToRectangle().ToRect(),
                    new System.Drawing.Rectangle(0, 0, width, height)));


            currentClipRect = intersectResult;
            if (intersectResult.Width <= 0 || intersectResult.Height <= 0)
            {
                return false;
            }
            else
            {
                gx.SetClip(intersectResult); return true;
            }
        }

        public override void DisableClipArea()
        {
            gx.ResetClip();
        }
        public override void EnableClipArea()
        {
            gx.SetClip(currentClipRect);
        }

        public override Rectangle CurrentClipRect
        {
            get
            {
                return currentClipRect.ToRect();
            }
        }


        public override int InternalOriginX
        {
            get
            {
                return internalCanvasOriginX;
            }
        }
        public override int InternalOriginY
        {
            get
            {
                return internalCanvasOriginY;
            }
        }

        public override bool PushClipArea(int x, int y, int width, int height)
        {
            clipRectStack.Push(currentClipRect);
            System.Drawing.Rectangle intersectRect = System.Drawing.Rectangle.Intersect(
                currentClipRect,
                new System.Drawing.Rectangle(x, y, width, height));


            if (intersectRect.Width == 0 || intersectRect.Height == 0)
            {
                currentClipRect = intersectRect;
                return false;
            }
            else
            {
                gx.SetClip(intersectRect);
                currentClipRect = intersectRect;
                return true;
            }
        }

        public override void PopClipArea()
        {
            if (clipRectStack.Count > 0)
            {
                currentClipRect = clipRectStack.Pop();

                gx.SetClip(currentClipRect);
            }
        }


        public override int Top
        {
            get
            {
                return top;
            }
        }
        public override int Left
        {
            get
            {
                return left;
            }
        }

        public override int Width
        {
            get
            {
                return right - left;
            }
        }
        public override int Height
        {
            get
            {
                return bottom - top;
            }
        }
        public override int Bottom
        {
            get
            {
                return bottom;
            }
        }
        public override int Right
        {
            get
            {
                return right;
            }
        }
        public override Rectangle Rect
        {
            get
            {
                return Rectangle.FromLTRB(left, top, right, bottom);
            }
        }

    }
         
}