﻿// 2015,2014 ,Apache2, WinterDev
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using PixelFarm.Drawing;
using LayoutFarm.Composers;
using LayoutFarm.WebDom;
using LayoutFarm.WebDom.Extension;
using LayoutFarm.UI;
using LayoutFarm.HtmlBoxes;
using LayoutFarm.CustomWidgets;

namespace LayoutFarm.HtmlWidgets
{

<<<<<<< HEAD
    public class Button : LightHtmlBoxWidgetBase
=======
    public class Button : OldHtmlWidgetBase
>>>>>>> v_widget2
    {
        string buttonText = "";
        DomElement pnode;
        public Button(int w, int h)
            : base(w, h)
        {
        }
        //---------------------------------------------------------------------------
        public string Text
        {
            get { return this.buttonText; }
            set
            {
                this.buttonText = value;
            }
        }
<<<<<<< HEAD
        public override DomElement GetPresentationDomNode(HtmlDocument htmldoc)
=======
        public override DomElement GetPresentationDomNode(WebDom.Impl.HtmlDocument htmldoc)
>>>>>>> v_widget2
        {
            if (pnode != null) return pnode;
            //----------------------------------
            pnode = htmldoc.CreateElement("div");
            pnode.AddChild("div", div2 =>
            {
                //init
                div2.SetAttribute("style", "padding:5px;background-color:#dddddd;");
                div2.AddChild("span", span =>
                {
                    span.AddTextContent(this.buttonText);
                });
                //------------------------------

                div2.AttachMouseDownEvent(e =>
                {
#if DEBUG
                    div2.dbugMark = 1;
#endif
                    // div2.SetAttribute("style", "padding:5px;background-color:#aaaaaa;");
                    EaseScriptElement ee = new EaseScriptElement(div2);
                    ee.ChangeBackgroundColor(Color.FromArgb(0xaa, 0xaa, 0xaa));
                    //div2.SetAttribute("style", "padding:5px;background-color:#aaaaaa;");
                    e.StopPropagation();

                });
                div2.AttachMouseUpEvent(e =>
                {
#if DEBUG
                    div2.dbugMark = 2;
#endif
                    //div2.SetAttribute("style", "padding:5px;background-color:#dddddd;");

                    EaseScriptElement ee = new EaseScriptElement(div2);
                    ee.ChangeBackgroundColor(Color.FromArgb(0xdd, 0xdd, 0xdd));
                    e.StopPropagation();
                });

            });
            return pnode;
        }

    }
}