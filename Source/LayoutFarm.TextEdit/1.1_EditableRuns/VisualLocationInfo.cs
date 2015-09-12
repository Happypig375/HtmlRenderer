﻿// 2015,2014 ,Apache2, WinterDev 
namespace LayoutFarm.Text
{

    public struct VisualLocationInfo
    {
        public readonly int pixelOffset;
        public readonly int charIndex;

        public static VisualLocationInfo EmptyTextRunLocationInfo = new VisualLocationInfo();


        public VisualLocationInfo(int pixelOffset, int charIndex)
        {
            this.pixelOffset = pixelOffset;
            this.charIndex = charIndex;
        }

    }

}