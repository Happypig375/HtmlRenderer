// 2015,2014 ,BSD, WinterDev
//ArthurHub  , Jose Manuel Menendez Poo

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
using PixelFarm.Drawing;
using LayoutFarm.Css;

namespace LayoutFarm.HtmlBoxes
{

    enum LayoutContextMode
    {
        Inline,
        Block
    }

    class LayoutContext
    {
        CssBox hostBox;
        LayoutVisitor lay;
        LayoutContextMode mode;
        public LayoutContext()
        {
        }
        public CssBox HostBox
        {
            get { return this.hostBox; }

        }
        public void BeginHostBox(CssBox hostBox, LayoutVisitor lay)
        {
            this.hostBox = hostBox;
            this.lay = lay;
            this.mode = LayoutContextMode.Inline;
        }
        public void AddCssBox(CssBox box)
        {
            switch (mode)
            {
                case LayoutContextMode.Block:
                    {
                    } break;
                default:
                    {

                    } break;

            }
        }
    }




    /// <summary>
    /// Helps on CSS Layout.
    /// </summary>
    static class CssLayoutEngine
    {

        const float CSS_OFFSET_THRESHOLD = 0.1f;
        public static void PerformContentLayout(CssBox box, LayoutVisitor lay)
        {
            //recursive

            //this box has its own  container property
            //this box may use...
            // 1) line formatting context  , or
            // 2) block formatting context 

            var myContainingBlock = lay.LatestContainingBlock;
            CssBox prevSibling = lay.LatestSiblingBox;
            if (box.CssDisplay != Css.CssDisplay.TableCell)
            {
                //-------------------------------------------
                if (box.CssDisplay != Css.CssDisplay.Table)
                {
                    float availableWidth = myContainingBlock.GetClientWidth();
                    if (!box.Width.IsEmptyOrAuto)
                    {
                        availableWidth = CssValueParser.ConvertToPx(box.Width, availableWidth, box);
                    }

                    box.SetCssBoxWidth(availableWidth);
                }
                //-------------------------------------------

                float localLeft = myContainingBlock.GetClientLeft() + box.ActualMarginLeft;
                float localTop = 0;

                if (prevSibling == null)
                {
                    //this is first child of parent
                    if (box.ParentBox != null)
                    {
                        localTop = myContainingBlock.GetClientTop();
                    }
                }
                else
                {
                    localTop = prevSibling.LocalVisualBottom;
                }
                localTop += box.UpdateMarginTopCollapse(prevSibling);
                box.SetLocation(localLeft, localTop);
                box.SetHeightToZero();
            }
            //--------------------------------------------------------------------------

            switch (box.CssDisplay)
            {
                case Css.CssDisplay.Table:
                case Css.CssDisplay.InlineTable:
                    {
                        //If we're talking about a table here..

                        lay.PushContaingBlock(box);
                        var currentLevelLatestSibling = lay.LatestSiblingBox;
                        lay.LatestSiblingBox = null;//reset

                        CssTableLayoutEngine.PerformLayout(box, myContainingBlock.GetClientWidth(), lay);

                        lay.LatestSiblingBox = currentLevelLatestSibling;
                        lay.PopContainingBlock();
                        //TODO: check if this can have absolute layer? 
                    } break;
                default:
                    {
                        //formatting context for...
                        //1. line formatting context
                        //2. block formatting context 
                        if (box.IsCustomCssBox)
                        {
                            //has custom layout method
                            box.ReEvaluateComputedValues(lay.SampleIFonts, lay.LatestContainingBlock);
                            box.CustomRecomputedValue(lay.LatestContainingBlock, lay.GraphicsPlatform);
                        }
                        else
                        {

                            //new candidate technique
                            var layoutContext = new LayoutContext();
                            layoutContext.BeginHostBox(box, lay);

                            var children = CssBox.UnsafeGetChildren(box);
                            var cnode = children.GetFirstLinkedNode();

                            while (cnode != null)
                            {
                                layoutContext.AddCssBox(cnode.Value);
                                cnode = cnode.Next;
                            }
                            //----------------------------------------------

                            if (ContainsInlinesOnly(box))
                            {
                                //This will automatically set the bottom of this block
                                DoLayoutLinesContext(box, lay);
                            }
                            else if (box.ChildCount > 0)
                            {
                                DoLayoutBlocksContext(box, lay);
                            }



                            if (box.HasAbsoluteLayer)
                            {
                                LayoutContentInAbsoluteLayer(lay, box);
                            }
                        }
                        //---------------------
                        //again!
                        switch (box.CssDisplay)
                        {
                            case CssDisplay.Flex:
                            case CssDisplay.InlineFlex:
                                {
                                    //------------------------------------------------
                                    RearrangeWithFlexContext(box, lay);
                                    //------------------------------------------------
                                } break;
                            default:
                                {    //TODO: review here again
                                    if (box.Float != CssFloat.None)
                                    {
                                        var iw = box.InnerContentWidth;
                                        var ew = box.VisualWidth;
                                        //float to specific position 
                                        box.SetVisualSize(iw, box.VisualHeight);
                                    }
                                } break;
                        }
                        //---------------------
                    } break;
            }


            switch (box.Float)
            {
                case CssFloat.Left:
                    {
                        var recentLeftFloatBox = lay.LatestLeftFloatBox;
                        var recentRightFloatBox = lay.LatestRightFloatBox;
                        float availableWidth2 = myContainingBlock.GetClientWidth();

                        if (recentRightFloatBox != null)
                        {
                            availableWidth2 -= recentRightFloatBox.LocalX;
                        }

                        float sx = myContainingBlock.GetClientLeft();
                        float sy = myContainingBlock.GetClientTop();

                        if (recentLeftFloatBox != null)
                        {
                            availableWidth2 -= recentLeftFloatBox.LocalVisualRight;
                            sx = recentLeftFloatBox.LocalVisualRight;
                            sy = recentLeftFloatBox.LocalY;
                        }

                        if (box.VisualWidth > availableWidth2)
                        {
                            //start newline
                            sx = myContainingBlock.GetClientLeft();

                            float sy1 = 0;
                            float sy2 = 0;
                            sy1 = sy2 = myContainingBlock.GetClientTop();

                            if (recentLeftFloatBox != null)
                            {
                                sy1 = recentLeftFloatBox.LocalX + recentLeftFloatBox.InnerContentHeight +
                                  recentLeftFloatBox.ActualPaddingBottom +
                                  recentLeftFloatBox.ActualMarginBottom;
                            }
                            if (recentRightFloatBox != null)
                            {
                                sy2 = recentRightFloatBox.LocalX + recentRightFloatBox.InnerContentHeight +
                                   recentRightFloatBox.ActualPaddingBottom +
                                   recentRightFloatBox.ActualMarginBottom;
                            }

                            sy = (sy1 > sy2) ? sy1 : sy2;
                        }

                        box.SetLocation(sx, sy);
                        lay.LatestLeftFloatBox = box;
                        lay.AddFloatBox(box);
                    } break;
                case CssFloat.Right:
                    {

                        var recentLeftFloatBox = lay.LatestLeftFloatBox;
                        var recentRightFloatBox = lay.LatestRightFloatBox;
                        float availableWidth2 = myContainingBlock.GetClientWidth();

                        if (recentLeftFloatBox != null)
                        {
                            availableWidth2 -= recentLeftFloatBox.LocalX;
                        }

                        float sx = myContainingBlock.GetClientRight() - box.VisualWidth;
                        float sy = myContainingBlock.GetClientTop();

                        if (recentRightFloatBox != null)
                        {
                            availableWidth2 -= recentRightFloatBox.LocalX;
                            sx = recentRightFloatBox.LocalX - box.VisualWidth;
                            sy = recentRightFloatBox.LocalY;
                        }

                        if (box.VisualWidth > availableWidth2)
                        {
                            //start newline
                            sx = myContainingBlock.GetClientRight() - box.VisualWidth;

                            float sy1 = 0;
                            float sy2 = 0;
                            sy1 = sy2 = myContainingBlock.GetClientTop();

                            if (recentLeftFloatBox != null)
                            {
                                sy1 = recentLeftFloatBox.LocalY + recentLeftFloatBox.InnerContentHeight +
                                  recentLeftFloatBox.ActualPaddingBottom +
                                  recentLeftFloatBox.ActualMarginBottom;
                            }
                            if (recentRightFloatBox != null)
                            {
                                sy2 = recentRightFloatBox.LocalY + recentRightFloatBox.InnerContentHeight +
                                   recentRightFloatBox.ActualPaddingBottom +
                                   recentRightFloatBox.ActualMarginBottom;
                            }

                            sy = (sy1 > sy2) ? sy1 : sy2;
                        }
                        box.SetLocation(sx, sy);
                        lay.LatestRightFloatBox = box;
                        lay.AddFloatBox(box);
                    } break;
                case CssFloat.None:
                default:
                    {
                        //review here for inherit property

                    } break;
            }

        }



        /// <summary>
        /// Measure image box size by the width\height set on the box and the actual rendered image size.<br/>
        /// If no image exists for the box error icon will be set.
        /// </summary>
        /// <param name="imgRun">the image word to measure</param>
        public static void MeasureImageSize(CssImageRun imgRun, LayoutVisitor lay)
        {
            var width = imgRun.OwnerBox.Width;
            var height = imgRun.OwnerBox.Height;

            bool hasImageTagWidth = width.Number > 0 && width.UnitOrNames == CssUnitOrNames.Pixels;
            bool hasImageTagHeight = height.Number > 0 && height.UnitOrNames == CssUnitOrNames.Pixels;
            bool scaleImageHeight = false;

            if (hasImageTagWidth)
            {
                imgRun.Width = width.Number;
            }
            else if (width.Number > 0 && width.IsPercentage)
            {

                imgRun.Width = width.Number * lay.LatestContainingBlock.VisualWidth;
                scaleImageHeight = true;
            }
            else if (imgRun.HasUserImageContent)
            {
                imgRun.Width = imgRun.ImageRectangle == Rectangle.Empty ? imgRun.OriginalImageWidth : imgRun.ImageRectangle.Width;
            }
            else
            {
                imgRun.Width = hasImageTagHeight ? height.Number / 1.14f : 20;
            }

            var maxWidth = imgRun.OwnerBox.MaxWidth;// new CssLength(imageWord.OwnerBox.MaxWidth);
            if (maxWidth.Number > 0)
            {
                float maxWidthVal = -1;
                switch (maxWidth.UnitOrNames)
                {
                    case CssUnitOrNames.Percent:
                        {
                            maxWidthVal = maxWidth.Number * lay.LatestContainingBlock.VisualWidth;
                        } break;
                    case CssUnitOrNames.Pixels:
                        {
                            maxWidthVal = maxWidth.Number;
                        } break;
                }


                if (maxWidthVal > -1 && imgRun.Width > maxWidthVal)
                {
                    imgRun.Width = maxWidthVal;
                    scaleImageHeight = !hasImageTagHeight;
                }
            }

            if (hasImageTagHeight)
            {
                imgRun.Height = height.Number;
            }
            else if (imgRun.HasUserImageContent)
            {
                imgRun.Height = imgRun.ImageRectangle == Rectangle.Empty ? imgRun.OriginalImageHeight : imgRun.ImageRectangle.Height;
            }
            else
            {
                imgRun.Height = imgRun.Width > 0 ? imgRun.Width * 1.14f : 22.8f;
            }

            if (imgRun.HasUserImageContent)
            {
                // If only the width was set in the html tag, ratio the height.
                if ((hasImageTagWidth && !hasImageTagHeight) || scaleImageHeight)
                {
                    // Divide the given tag width with the actual image width, to get the ratio.
                    float ratio = imgRun.Width / imgRun.OriginalImageWidth;
                    imgRun.Height = imgRun.OriginalImageHeight * ratio;
                }
                // If only the height was set in the html tag, ratio the width.
                else if (hasImageTagHeight && !hasImageTagWidth)
                {
                    // Divide the given tag height with the actual image height, to get the ratio.
                    float ratio = imgRun.Height / imgRun.OriginalImageHeight;
                    imgRun.Width = imgRun.OriginalImageWidth * ratio;
                }
            }
            //imageWord.Height += imageWord.OwnerBox.ActualBorderBottomWidth + imageWord.OwnerBox.ActualBorderTopWidth + imageWord.OwnerBox.ActualPaddingTop + imageWord.OwnerBox.ActualPaddingBottom;
        }

        /// <summary>
        /// Check if the given box contains only inline child boxes.
        /// </summary>
        /// <param name="box">the box to check</param>
        /// <returns>true - only inline child boxes, false - otherwise</returns>
        static bool ContainsInlinesOnly(CssBox box)
        {
            var children = CssBox.UnsafeGetChildren(box);
            var linkedNode = children.GetFirstLinkedNode();
            while (linkedNode != null)
            {
                if (!linkedNode.Value.OutsideDisplayIsInline)
                {
                    return false;
                }

                linkedNode = linkedNode.Next;
            }
            return true;
        }

        /// <summary>
        /// do layout line formatting context
        /// </summary>
        /// <param name="hostBlock"></param>
        /// <param name="lay"></param>
        static void DoLayoutLinesContext(CssBox hostBlock, LayoutVisitor lay)
        {
            //this in line formatting context
            //*** hostBlock must confirm that it has all inline children     
            hostBlock.SetHeightToZero();
            hostBlock.ResetLineBoxes();

            //----------------------------------------------------------------------------------------
            float limitLocalRight = hostBlock.VisualWidth - (hostBlock.ActualPaddingRight + hostBlock.ActualBorderRightWidth);
            float localX = hostBlock.ActualTextIndent + hostBlock.ActualPaddingLeft + hostBlock.ActualBorderLeftWidth;
            float localY = hostBlock.ActualPaddingTop + hostBlock.ActualBorderTopWidth;
            //----------------------------------------------------------------------------------------

            if (lay.HasFloatBoxInContext)
            {
                var recentLeftFloatBox = lay.LatestLeftFloatBox;
                var recentRightFloatBox = lay.LatestRightFloatBox;
                var latestSibling = lay.LatestSiblingBox;

                if (latestSibling != null)
                {
                    //check latest sibling first 
                    if (hostBlock.Float == CssFloat.None)
                    {
                        if (recentLeftFloatBox != null)
                        {
                            if (hostBlock.LocalY < recentLeftFloatBox.LocalVisualBottom)
                            {
                                localX = recentLeftFloatBox.LocalVisualRight;
                            }
                        }
                        else
                        {

                        }
                    }
                }
                else
                {
                    if (hostBlock.Float == CssFloat.None)
                    {
                        if (recentLeftFloatBox != null)
                        {
                            localX = recentLeftFloatBox.LocalVisualRight;
                        }
                        if (recentRightFloatBox != null)
                        {
                            limitLocalRight = recentRightFloatBox.LocalX;
                        }
                    }
                    //check if need newline or not 
                }

            }

            int interlineSpace = 0;

            //First line box 

            var line = new CssLineBox(hostBlock);
            hostBlock.AddLineBox(line);
            //****
            var floatCtx = new FloatFormattingContext();

            FlowBoxContentIntoHostLineFmtContext(lay, hostBlock, hostBlock,
                  limitLocalRight, localX,
                  ref line, ref localX, ref floatCtx);

            //**** 
            // if width is not restricted we need to lower it to the actual width
            if (hostBlock.VisualWidth + lay.ContainerBlockGlobalX >= CssBoxConstConfig.BOX_MAX_RIGHT)
            {
                float newWidth = localX + hostBlock.ActualPaddingRight + hostBlock.ActualBorderRightWidth;// CssBox.MAX_RIGHT - (args.ContainerBlockGlobalX + blockBox.LocalX);
                if (newWidth <= CSS_OFFSET_THRESHOLD)
                {
                    newWidth = CSS_OFFSET_THRESHOLD;
                }
                hostBlock.SetVisualWidth(newWidth);
            }
            //--------------------- 
            float maxLineWidth = 0;
            if (hostBlock.CssDirection == CssDirection.Rtl)
            {
                CssTextAlign textAlign = hostBlock.CssTextAlign;
                foreach (CssLineBox linebox in hostBlock.GetLineBoxIter())
                {
                    ApplyAlignment(linebox, textAlign, lay);
                    ApplyRightToLeft(linebox); //*** 
                    linebox.CloseLine(lay); //*** 
                    linebox.CachedLineTop = localY;
                    localY += linebox.CacheLineHeight + interlineSpace; // + interline space?

                    if (maxLineWidth < linebox.CachedExactContentWidth)
                    {
                        maxLineWidth = linebox.CachedExactContentWidth;
                    }
                }
            }
            else
            {

                CssTextAlign textAlign = hostBlock.CssTextAlign;
                foreach (CssLineBox linebox in hostBlock.GetLineBoxIter())
                {
                    ApplyAlignment(linebox, textAlign, lay);

                    linebox.CloseLine(lay); //***

                    linebox.CachedLineTop = localY;
                    localY += linebox.CacheLineHeight + interlineSpace;

                    if (maxLineWidth < linebox.CachedExactContentWidth)
                    {
                        maxLineWidth = linebox.CachedExactContentWidth;
                    }
                }
            }

            hostBlock.SetVisualHeight(localY + hostBlock.ActualPaddingBottom + hostBlock.ActualBorderBottomWidth);

            //final 
            SetFinalInnerContentSize(hostBlock, maxLineWidth, hostBlock.VisualHeight, lay);
        }
        static void DoLayoutBlocksContext(CssBox box, LayoutVisitor lay)
        {

            //block formatting context.... 
            lay.PushContaingBlock(box);
            var currentLevelLatestSibling = lay.LatestSiblingBox;
            lay.LatestSiblingBox = null;//reset 
            //------------------------------------------  
            var children = CssBox.UnsafeGetChildren(box);
            var cnode = children.GetFirstLinkedNode();
            while (cnode != null)
            {
                var childBox = cnode.Value;

                //----------------------------
                if (childBox.IsBrElement)
                {
                    //br always block
                    CssBox.ChangeDisplayType(childBox, Css.CssDisplay.Block);
                    childBox.SetVisualHeight(FontDefaultConfig.DEFAULT_FONT_SIZE * 0.95f);
                }
                //-----------------------------
                if (childBox.OutsideDisplayIsInline)
                {
                    //inline correction on-the-fly ! 
                    //1. collect consecutive inlinebox
                    //   and move to new anon block box

                    CssBox anoForInline = CreateAnonBlock(box, childBox);
                    anoForInline.ReEvaluateComputedValues(lay.SampleIFonts, box);

                    var tmp = cnode.Next;
                    do
                    {
                        children.Remove(childBox);
                        anoForInline.AppendChild(childBox);

                        if (tmp != null)
                        {
                            childBox = tmp.Value;
                            if (childBox.OutsideDisplayIsInline)
                            {
                                tmp = tmp.Next;
                                if (tmp == null)
                                {
                                    children.Remove(childBox);
                                    anoForInline.AppendChild(childBox);
                                    break;//break from do while
                                }
                            }
                            else
                            {
                                break;//break from do while
                            }
                        }
                        else
                        {
                            break;//break from do while
                        }
                    } while (true);

                    childBox = anoForInline;
                    //------------------------   
                    //2. move this inline box 
                    //to new anonbox 
                    cnode = tmp;
                    //------------------------ 
                    childBox.PerformLayout(lay);

                    if (childBox.CanBeReferenceSibling)
                    {
                        lay.LatestSiblingBox = childBox;
                    }
                }
                else
                {

                    childBox.PerformLayout(lay);

                    switch (childBox.Float)
                    {
                        case CssFloat.Left:
                            {
                                childBox.IsOutOfFlowBox = true;
                                lay.LatestLeftFloatBox = childBox;

                            } break;
                        case CssFloat.Right:
                            {
                                childBox.IsOutOfFlowBox = true;
                                //float box is out-of-flow box
                                //so move it to abs layer                                 
                                lay.LatestRightFloatBox = childBox;

                            } break;
                    }

                    if (childBox.Float == CssFloat.None && childBox.CanBeReferenceSibling)
                    {
                        lay.LatestSiblingBox = childBox;
                    }

                    cnode = cnode.Next;

                }
            }

            //------------------------------------------
            lay.LatestSiblingBox = currentLevelLatestSibling;
            lay.PopContainingBlock();
            //------------------------------------------------ 
            float boxWidth = CalculateActualWidth(box);

            if (lay.ContainerBlockGlobalX + boxWidth > CssBoxConstConfig.BOX_MAX_RIGHT)
            {
            }
            else
            {
                if (box.CssDisplay != Css.CssDisplay.TableCell)
                {
                    box.SetVisualWidth(boxWidth);
                }
            }

            float boxHeight = box.GetHeightAfterMarginBottomCollapse(lay.LatestContainingBlock);
            box.SetVisualHeight(boxHeight);
            //--------------------------------------------------------------------------------
            //final  
            SetFinalInnerContentSize(box, boxWidth, boxHeight, lay);

        }
        static void SetFinalInnerContentSize(CssBox box, float innerContentW, float innerContentH, LayoutVisitor lay)
        {
            box.InnerContentWidth = innerContentW;
            box.InnerContentHeight = innerContentH;

            if (!box.Height.IsEmptyOrAuto)
            {
                var h = CssValueParser.ConvertToPx(box.Height, lay.LatestContainingBlock.VisualWidth, lay.LatestContainingBlock);
                box.SetExpectedSize(box.ExpectedWidth, h);
                box.SetVisualHeight(h);
                box.SetCssBoxHeight(h);
            }
            else
            {
                switch (box.Position)
                {
                    case CssPosition.Fixed:
                    case CssPosition.Absolute:
                        box.SetVisualHeight(box.InnerContentHeight);
                        break;
                }

            }
            if (!box.Width.IsEmptyOrAuto)
            {
                //find max line width  
                var w = CssValueParser.ConvertToPx(box.Width, lay.LatestContainingBlock.VisualWidth, lay.LatestContainingBlock);
                box.SetExpectedSize(w, box.ExpectedHeight);
                box.SetVisualWidth(w);
                box.SetCssBoxWidth(w);
            }
            else
            {
                switch (box.Position)
                {
                    case CssPosition.Fixed:
                    case CssPosition.Absolute:
                        box.SetVisualWidth(box.InnerContentWidth);
                        break;
                }
            }

            switch (box.Overflow)
            {
                case CssOverflow.Scroll:
                case CssOverflow.Auto:
                    {
                        if ((box.InnerContentHeight > box.VisualHeight) ||
                        (box.InnerContentWidth > box.VisualWidth))
                        {
                            lay.RequestScrollView(box);
                        }
                    } break;
            }
        }
        static float CalculateActualWidth(CssBox box)
        {
            float maxRight = 0;
            var boxes = CssBox.UnsafeGetChildren(box);
            var cnode = boxes.GetFirstLinkedNode();
            while (cnode != null)
            {
                var cssbox = cnode.Value;
                float nodeRight = cssbox.LocalX + cssbox.InnerContentWidth +
                     cssbox.ActualPaddingLeft + cssbox.ActualPaddingRight +
                     cssbox.ActualMarginLeft +
                     cssbox.ActualMarginRight;

                maxRight = nodeRight > maxRight ? nodeRight : maxRight;
                cnode = cnode.Next;
            }
            return maxRight + (box.ActualBorderLeftWidth + box.ActualPaddingLeft +
                box.ActualPaddingRight + box.ActualBorderRightWidth);
        }

        static CssBox CreateAnonBlock(CssBox parent, CssBox insertBefore)
        {
            //auto gen by layout engine ***
            var newBox = new CssBox(CssBox.UnsafeGetBoxSpec(parent).GetAnonVersion(), parent.RootGfx);
            CssBox.ChangeDisplayType(newBox, Css.CssDisplay.Block);
            parent.InsertChild(insertBefore, newBox);
            return newBox;
        }

        /// <summary>
        /// Recursively flows the content of the box using the inline model
        /// </summary>
        /// <param name="lay"></param>
        /// <param name="hostBox"></param>
        /// <param name="srcBox"></param>
        /// <param name="limitLocalRight"></param>
        /// <param name="firstRunStartX"></param>
        /// <param name="hostLine"></param>
        /// <param name="cx"></param>
        static void FlowBoxContentIntoHostLineFmtContext(
          LayoutVisitor lay,
          CssBox hostBox, //target host box that contains line formatting context
          CssBox srcBox, //src that has  runs /splitable content) to flow into hostBox line model
          float limitLocalRight,
          float firstRunStartX,
          ref CssLineBox hostLine,
          ref float cx,
          ref FloatFormattingContext floatCtx)
        {

            //recursive *** 
            //-------------------------------------------------------------------- 
            var oX = cx;
            if (srcBox.HasOnlyRuns)
            {
                //condition 3 

                FlowRunsIntoHost(lay, hostBox, srcBox, srcBox, 0,
                     limitLocalRight, firstRunStartX,
                     0, 0,
                     CssBox.UnsafeGetRunList(srcBox),
                     ref hostLine, ref cx
                     );
            }
            else
            {

                int childNumber = 0;
                var ifonts = lay.SampleIFonts;
                foreach (CssBox b in srcBox.GetChildBoxIter())
                {
                    if (b.CssDisplayOutside == CssDisplayOutside.Block)
                    {
                        //if display outside is block
                        //just break  

                    }
                    //-----------------------------------------
#if DEBUG
                    if (b.__aa_dbugId == 4)
                    {

                    }
#endif

                    float leftMostSpace = 0, rightMostSpace = 0;
                    //if b has absolute pos then it is removed from the flow 
                    if (b.NeedComputedValueEvaluation)
                    {
                        b.ReEvaluateComputedValues(ifonts, hostBox);
                    }
                    b.MeasureRunsSize(lay);
#if DEBUG
                    if (b.Position == CssPosition.Absolute)
                    {
                        //should not found here!
                        throw new NotSupportedException();
                    }
#endif

                    cx += leftMostSpace;
                    if (b.CssDisplay == CssDisplay.InlineBlock)
                    {
                        //--------
                        // if inside display is block context *** 
                        //--------- 

                        //outside -> inline
                        //inside -> block

                        //can't split 
                        //create 'block-run'  
                        PerformContentLayout(b, lay);
                        CssBlockRun blockRun = b.JustBlockRun;
                        if (blockRun == null)
                        {
                            blockRun = new CssBlockRun(b);
                            blockRun.SetOwner(srcBox);
                            b.JustBlockRun = blockRun;
                        }


                        if (b.Width.IsEmptyOrAuto)
                        {
                            blockRun.SetSize(CssBox.GetLatestCachedMinWidth(b), b.VisualHeight);
                        }
                        else
                        {
                            blockRun.SetSize(b.VisualWidth, b.VisualHeight);
                        }

                        b.SetLocation(b.LocalX, 0); //because of inline***

                        FlowRunsIntoHost(lay, hostBox, srcBox, b, childNumber,
                            limitLocalRight, firstRunStartX,
                            leftMostSpace, rightMostSpace,
                            new List<CssRun>() { b.JustBlockRun },
                            ref hostLine, ref cx);
                    }

                    else if (b.HasOnlyRuns)
                    {
                        switch (b.Float)
                        {
                            default:
                            case CssFloat.None:
                                {
                                    FlowRunsIntoHost(lay, hostBox, srcBox, b, childNumber,
                                        limitLocalRight, firstRunStartX,
                                        leftMostSpace, rightMostSpace,
                                        CssBox.UnsafeGetRunList(b),
                                        ref hostLine, ref cx);

                                } break;
                            case CssFloat.Left:
                                {
                                    //float is out of flow item 
                                    //1. current line is shortening
                                    //2. add 'b' to special container ***  

                                    var newAnonBlock = new CssFloatContainerBox(
                                        CssBox.UnsafeGetBoxSpec(b),
                                        b.RootGfx, CssDisplay.Block);
                                    newAnonBlock.ReEvaluateComputedValues(ifonts, hostBox);

                                    //add to abs layer
                                    hostBox.AppendToAbsoluteLayer(newAnonBlock);
                                    newAnonBlock.ResetLineBoxes();

                                    float localX1 = 0;
                                    var line = new CssLineBox(newAnonBlock);
                                    newAnonBlock.AddLineBox(line);

                                    var newFloatCtx = new FloatFormattingContext();
                                    FlowBoxContentIntoHostLineFmtContext(lay, newAnonBlock, b,
                                        limitLocalRight, 0,
                                        ref line, ref localX1, ref newFloatCtx);


                                    float localY = 0;
                                    int interlineSpace = 0;
                                    float maxLineWidth = 0;
                                    CssTextAlign textAlign = newAnonBlock.CssTextAlign;
                                    foreach (CssLineBox linebox in newAnonBlock.GetLineBoxIter())
                                    {
                                        ApplyAlignment(linebox, textAlign, lay);
                                        linebox.CloseLine(lay); //*** 
                                        linebox.CachedLineTop = localY;
                                        localY += linebox.CacheLineHeight + interlineSpace;
                                        if (maxLineWidth < linebox.CachedExactContentWidth)
                                        {
                                            maxLineWidth = linebox.CachedExactContentWidth;
                                        }
                                    }

                                    float hostSizeW = hostBox.VisualWidth;
                                    SetFinalInnerContentSize(newAnonBlock, maxLineWidth, localY, lay);
                                    //need to adjust line box   

                                    //TODO: review here!, 
                                    if (hostLine.CanDoMoreLeftOffset(newAnonBlock.InnerContentWidth, limitLocalRight))
                                    {
                                        hostLine.DoLeftOffset(newAnonBlock.InnerContentWidth);
                                        cx = hostLine.GetRightOfLastRun();
                                        newAnonBlock.SetLocation(floatCtx.lineLeftOffset, floatCtx.offsetFloatTop); //TODO: review top location again
                                        floatCtx.lineLeftOffset = newAnonBlock.LocalX + newAnonBlock.InnerContentWidth;
                                    }
                                    else
                                    {
                                        //newline
                                        newAnonBlock.SetLocation(hostBox.GetClientLeft(), hostLine.CalculateLineHeight());
                                        floatCtx.offsetFloatTop = newAnonBlock.LocalY;
                                    }

                                } break;
                            case CssFloat.Right:
                                {
                                    //float is out of flow item      
                                    //1. create new block box and then
                                    //flow content in to this new box
                                    var newAnonBlock = new CssFloatContainerBox(
                                        CssBox.UnsafeGetBoxSpec(b),
                                        b.RootGfx, CssDisplay.Block);

                                    newAnonBlock.ReEvaluateComputedValues(ifonts, hostBox);

                                    //add to abs layer
                                    hostBox.AppendToAbsoluteLayer(newAnonBlock);
                                    newAnonBlock.ResetLineBoxes();
                                    float localX1 = 0;

                                    var line = new CssLineBox(newAnonBlock);
                                    newAnonBlock.AddLineBox(line);

                                    var newFloatCtx = new FloatFormattingContext();
                                    FlowBoxContentIntoHostLineFmtContext(lay, newAnonBlock, b,
                                        limitLocalRight, 0,
                                        ref line, ref localX1, ref newFloatCtx);

                                    float localY = 0;
                                    int interlineSpace = 0;
                                    float maxLineWidth = 0;
                                    CssTextAlign textAlign = newAnonBlock.CssTextAlign;
                                    foreach (CssLineBox linebox in newAnonBlock.GetLineBoxIter())
                                    {
                                        ApplyAlignment(linebox, textAlign, lay);
                                        linebox.CloseLine(lay); //*** 
                                        linebox.CachedLineTop = localY;
                                        localY += linebox.CacheLineHeight + interlineSpace;
                                        if (maxLineWidth < linebox.CachedExactContentWidth)
                                        {
                                            maxLineWidth = linebox.CachedExactContentWidth;
                                        }
                                    }
                                    SetFinalInnerContentSize(newAnonBlock, maxLineWidth, localY, lay);

                                    //todo: review here
                                    float hostSizeW = hostBox.VisualWidth;
                                    var rightOfLastRun = hostLine.GetRightOfLastRun();

                                    if (!floatCtx.floatingOutOfLine)
                                    {
                                        if (rightOfLastRun + maxLineWidth < hostSizeW - floatCtx.lineRightOffset)
                                        {
                                            float newX = hostSizeW - (maxLineWidth + floatCtx.lineRightOffset);
                                            newAnonBlock.SetLocation(newX, floatCtx.offsetFloatTop);
                                            floatCtx.lineRightOffset = newX;
                                            floatCtx.rightFloatBox = newAnonBlock;
                                            floatCtx.floatingOutOfLine = true;
                                        }
                                        else
                                        {
                                            //start newline 
                                            float newX = hostSizeW - maxLineWidth;
                                            newAnonBlock.SetLocation(newX, floatCtx.offsetFloatTop + hostLine.CalculateLineHeight());
                                            floatCtx.lineRightOffset = newX;
                                            floatCtx.rightFloatBox = newAnonBlock;
                                            floatCtx.floatingOutOfLine = true;
                                            floatCtx.offsetFloatTop = newAnonBlock.LocalY;
                                        }
                                    }
                                    else
                                    {
                                        //out-of-line mode
                                        if (floatCtx.rightFloatBox != null)
                                        {
                                            float newX = floatCtx.rightFloatBox.LocalX - maxLineWidth;
                                            if (newX > 0)
                                            {
                                                newAnonBlock.SetLocation(newX, floatCtx.offsetFloatTop);
                                                floatCtx.lineRightOffset = newX;
                                                floatCtx.rightFloatBox = newAnonBlock;
                                                floatCtx.offsetFloatTop = newAnonBlock.LocalY;
                                            }
                                            else
                                            {  //start new line
                                                newX = hostSizeW - maxLineWidth;
                                                newAnonBlock.SetLocation(newX, floatCtx.rightFloatBox.LocalY + floatCtx.rightFloatBox.InnerContentHeight);
                                                floatCtx.lineRightOffset = newX;
                                                floatCtx.rightFloatBox = newAnonBlock;
                                                floatCtx.offsetFloatTop = newAnonBlock.LocalY + newAnonBlock.InnerContentHeight;
                                            }
                                        }
                                        else
                                        {
                                            throw new NotSupportedException();
                                        }
                                    }

                                } break;
                        }
                    }
                    else
                    {


                        //go deeper  
                        //recursive *** 
                        //not new lineFormatting context
                        FlowBoxContentIntoHostLineFmtContext(lay, hostBox, b,
                                   limitLocalRight, firstRunStartX,
                                   ref hostLine, ref cx, ref floatCtx);

                    }

                    cx += rightMostSpace;
                    childNumber++;
                }
            }
            if (srcBox.Position == CssPosition.Relative)
            {
                //offset content relative to it 'flow' position'
                var left = CssValueParser.ConvertToPx(srcBox.Left, hostBox.VisualWidth, srcBox);
                var top = CssValueParser.ConvertToPx(srcBox.Top, hostBox.VisualWidth, srcBox);
                srcBox.SetLocation(srcBox.LocalX + left, srcBox.LocalY + top);
            }

        }
        static void LayoutContentInAbsoluteLayer(LayoutVisitor lay, CssBox srcBox)
        {

            if (srcBox.JustTempContainer) return;

            var ifonts = lay.SampleIFonts;

            //css3 jan2015: absolute position
            //use offset relative to its normal the box's containing box***

            float containerW = lay.LatestContainingBlock.VisualWidth;

            float maxRight = 0;
            float maxBottom = 0;

            foreach (var b in srcBox.GetAbsoluteChildBoxIter())
            {
                if (b.JustTempContainer)
                {
                    continue;
                }

                if (b.NeedComputedValueEvaluation)
                {
                    b.ReEvaluateComputedValues(ifonts, lay.LatestContainingBlock);
                }

                b.MeasureRunsSize(lay);
                PerformContentLayout(b, lay);

                b.SetLocation(
                     CssValueParser.ConvertToPx(b.Left, containerW, b),
                     CssValueParser.ConvertToPx(b.Top, containerW, b));

                var localRight = b.LocalVisualRight;
                var localBottom = b.LocalVisualBottom;

                if (maxRight < localRight)
                {
                    maxRight = localRight;
                }
                if (maxBottom < localBottom)
                {
                    maxBottom = localBottom;
                }
            }

            int i_maxRight = (int)maxRight;
            int i_maxBottom = (int)maxBottom;
            srcBox.InnerContentWidth = i_maxRight;
            srcBox.InnerContentHeight = i_maxBottom;
        }

        static void FlowRunsIntoHost(LayoutVisitor lay,
          CssBox hostBox,
          CssBox splitableBox,
          CssBox b,
          int childNumber, //child number of b
          float limitRight,
          float firstRunStartX,
          float leftMostSpace,
          float rightMostSpace,
          List<CssRun> runs,
          ref CssLineBox hostLine,
          ref float cx)
        {
            //flow runs into hostLine, create new line if need  
            bool wrapNoWrapBox = false;
            CssWhiteSpace bWhiteSpace = b.WhiteSpace;
            bool hostBoxIsB = hostBox == b;

            if (bWhiteSpace == CssWhiteSpace.NoWrap && cx > firstRunStartX)
            {
                var tmpRight = cx;
                for (int i = runs.Count - 1; i >= 0; --i)
                {
                    tmpRight += runs[i].Width;
                }
                //----------------------------------------- 
                if (tmpRight > limitRight)
                {
                    wrapNoWrapBox = true;
                }
            }

            //----------------------------------------------------- 

            int lim = runs.Count - 1;
            for (int i = 0; i <= lim; ++i)
            {
                var run = runs[i];
                //---------------------------------------------------
                //check if need to start new line ? 
                if ((cx + run.Width + rightMostSpace > limitRight &&
                     bWhiteSpace != CssWhiteSpace.NoWrap &&
                     bWhiteSpace != CssWhiteSpace.Pre &&
                     (bWhiteSpace != CssWhiteSpace.PreWrap || !run.IsSpaces))
                     || run.IsLineBreak || wrapNoWrapBox)
                {

                    wrapNoWrapBox = false; //once! 

                    //-------------------------------
                    //create new line ***
                    hostLine = new CssLineBox(hostBox);
                    hostBox.AddLineBox(hostLine);
                    //reset x pos for new line
                    cx = firstRunStartX;


                    // handle if line is wrapped for the first text element where parent has left margin/padding
                    if (childNumber == 0 && //b is first child of splitable box ('b' == splitableBox.GetFirstChild())
                        !run.IsLineBreak &&
                        (i == 0 || splitableBox.ParentBox.IsBlock))//this run is first run of 'b' (run == b.FirstRun)
                    {
                        cx += splitableBox.ActualMarginLeft +
                            splitableBox.ActualBorderLeftWidth +
                            splitableBox.ActualPaddingLeft;
                    }

                    if (run.IsSolidContent || i == 0)
                    {
                        cx += leftMostSpace;
                    }
                }
                //---------------------------------------------------

                if (run.IsSpaces && hostLine.RunCount == 0)
                {
                    //not add 
                    continue;
                }
                //---------------------------------------------------

                hostLine.AddRun(run); //***
                if (lim == 0)
                {
                    //single one
                    if (!hostBoxIsB)
                    {
                        cx += b.ActualPaddingLeft;
                    }
                    run.SetLocation(cx, 0);
                    cx += run.Width + b.ActualPaddingRight;
                }
                else
                {
                    if (i == 0)
                    {
                        //first
                        if (!hostBoxIsB)
                        {
                            cx += b.ActualPaddingLeft;
                        }

                        run.SetLocation(cx, 0);
                        cx = run.Right;
                    }
                    else if (i == lim)
                    {
                        run.SetLocation(cx, 0);
                        cx += run.Width + b.ActualPaddingRight;
                    }
                    else
                    {
                        run.SetLocation(cx, 0);
                        cx = run.Right;
                    }
                }
                //---------------------------------------------------
                //move current_line_x to right of run
                //cx = run.Right;
            }
        }
        /// <summary>
        /// Applies vertical and horizontal alignment to words in lineboxes
        /// </summary>
        /// <param name="g"></param>
        /// <param name="lineBox"></param> 
        static void ApplyAlignment(CssLineBox lineBox, CssTextAlign textAlign, LayoutVisitor lay)
        {
            switch (textAlign)
            {
                case CssTextAlign.Right:
                    ApplyRightAlignment(lineBox);
                    break;
                case CssTextAlign.Center:
                    ApplyCenterAlignment(lineBox);
                    break;
                case CssTextAlign.Justify:
                    ApplyJustifyAlignment(lineBox);
                    break;
                default:
                    break;
            }
            //--------------------------------------------- 
            // Applies vertical alignment to the linebox 
            return;
            //TODO: review here
            lineBox.ApplyBaseline(lineBox.CalculateTotalBoxBaseLine(lay));
            //---------------------------------------------  
        }

        /// <summary>
        /// Applies right to left direction to words
        /// </summary>
        /// <param name="blockBox"></param>
        /// <param name="lineBox"></param>
        static void ApplyRightToLeft(CssLineBox lineBox)
        {
            if (lineBox.RunCount > 0)
            {

                float left = lineBox.GetFirstRun().Left;
                float right = lineBox.GetLastRun().Right;
                foreach (CssRun run in lineBox.GetRunIter())
                {
                    float diff = run.Left - left;
                    float w_right = right - diff;
                    run.Left = w_right - run.Width;
                }
            }
        }
        static void ApplyJustifyAlignment(CssLineBox lineBox)
        {


            if (lineBox.IsLastLine) return;

            float indent = lineBox.IsFirstLine ? lineBox.OwnerBox.ActualTextIndent : 0f;

            float runWidthSum = 0f;
            int runCount = 0;

            float availableWidth = lineBox.OwnerBox.GetClientWidth() - indent;

            // Gather text sum
            foreach (CssRun w in lineBox.GetRunIter())
            {
                runWidthSum += w.Width;
                runCount++;
            }

            if (runCount == 0) return; //Avoid Zero division

            float spaceOfEachRun = (availableWidth - runWidthSum) / runCount; //Spacing that will be used

            float cX = lineBox.OwnerBox.GetClientLeft() + indent;
            CssRun lastRun = lineBox.GetLastRun();
            foreach (CssRun run in lineBox.GetRunIter())
            {
                run.Left = cX;
                cX = run.Right + spaceOfEachRun;
                if (run == lastRun)
                {
                    run.Left = lineBox.OwnerBox.GetClientRight() - run.Width;
                }
            }
        }

        /// <summary>
        /// Applies centered alignment to the text on the linebox
        /// </summary>
        /// <param name="g"></param>
        /// <param name="line"></param>
        static void ApplyCenterAlignment(CssLineBox line)
        {

            if (line.RunCount == 0) return;
            CssRun lastRun = line.GetLastRun();
            float diff = (line.OwnerBox.GetClientWidth() - lastRun.Right) / 2;
            if (diff > CSS_OFFSET_THRESHOLD)
            {
                foreach (CssRun word in line.GetRunIter())
                {
                    word.Left += diff;
                }
                line.CachedLineContentWidth += diff;
            }
        }

        /// <summary>
        /// Applies right alignment to the text on the linebox
        /// </summary>
        /// <param name="g"></param>
        /// <param name="line"></param>
        static void ApplyRightAlignment(CssLineBox line)
        {
            if (line.RunCount == 0)
            {
                return;
            }
            CssRun lastRun = line.GetLastRun();
            float diff = line.OwnerBox.GetClientWidth() - line.GetLastRun().Right;
            if (diff > CSS_OFFSET_THRESHOLD)
            {
                foreach (CssRun word in line.GetRunIter())
                {
                    word.Left += diff;
                }
            }
        }
        static void RearrangeWithFlexContext(CssBox box, LayoutVisitor lay)
        {

            //this is an experiment!,  
            var children = CssBox.UnsafeGetChildren(box);
            var cnode = children.GetFirstLinkedNode();

            List<FlexItem> simpleFlexLine = new List<FlexItem>();
            FlexLine flexLine = new FlexLine(box);
            while (cnode != null)
            {
                flexLine.AddChild(new FlexItem(cnode.Value));
                cnode = cnode.Next;
            }
            flexLine.Arrange();


            if (box.Height.IsEmptyOrAuto)
            {
                //set new height                
                box.SetVisualHeight(flexLine.LineHeightAfterArrange);
                //check if it need scrollbar or not 
            }
            if (box.Width.IsEmptyOrAuto)
            {
                box.SetVisualWidth(flexLine.LineWidthAfterArrange);
            }

            SetFinalInnerContentSize(box, flexLine.LineWidthAfterArrange, flexLine.LineHeightAfterArrange, lay);

        }
    }
}