﻿//2014 Apache2, WinterDev
using System;
using System.Collections.Generic;
using System.Text;

using System.Drawing;



using LayoutFarm;

namespace LayoutFarm
{
    public static class Clipboard
    {
        static string textdata;
        public static void Clear()
        {
            textdata = null;
        }
        public static void SetText(string text)
        {
            textdata = text;
        }
        public static bool ContainUnicodeText()
        {
            return textdata != null;
        }
        public static string GetUnicodeText()
        {
            return textdata;
        }
    }
}