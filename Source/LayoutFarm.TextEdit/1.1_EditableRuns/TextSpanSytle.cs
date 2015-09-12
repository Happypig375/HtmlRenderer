﻿// 2015,2014 ,Apache2, WinterDev

using PixelFarm.Drawing;

namespace LayoutFarm.Text
{

    public struct TextSpanStyle
    {

        public Color FontColor
        {
            get;
            set;
        }
        public FontInfo FontInfo;
        public byte ContentHAlign;

        public static readonly TextSpanStyle Empty = new TextSpanStyle();
        public bool IsEmpty()
        {
            return this.FontInfo == null;
        }
    }
}