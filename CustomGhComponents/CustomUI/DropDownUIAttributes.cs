using Grasshopper.Kernel.Attributes;
using Grasshopper.GUI.Canvas;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using System.Windows.Forms;
using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;

namespace CustomUI
{
    /// <summary>
    /// Class to create custom component UI with multiple dropdowns
    /// 
    /// Note that it is the component's responsibility to dynamically update lists, this class is only displaying what it gets.
    /// 
    /// To use this method override CreateAttributes() in component class and set m_attributes = new DropDownUIAttributes(...
    /// </summary>
    public class DropDownUIAttributes : GH_ComponentAttributes
    {
        public DropDownUIAttributes(GH_Component owner, Action<int, int> clickHandle, List<List<string>> dropdownContents, List<string> selections, List<string> spacerTexts = null, List<string> initialdescriptions = null) : base(owner)
        {
            dropdownlists = dropdownContents;
            spacerTxts = spacerTexts;
            action = clickHandle;
            initialTxts = initialdescriptions ?? null; // if no description is inputted then null initialTxt
            if (selections == null)
            {
                List<string> tempDisplaytxt = new List<string>();
                for (int i = 0; i < dropdownlists.Count; i++)
                    tempDisplaytxt.Add((initialdescriptions == null) ? dropdownlists[i][0] : initialdescriptions[i]);
                displayTexts = tempDisplaytxt;
            }
            else
                displayTexts = selections;
        }

        readonly List<string> spacerTxts; // list of descriptive texts above each dropdown
        List<RectangleF> SpacerBounds;

        List<RectangleF> BorderBound;// area where the selected item is displayed
        List<RectangleF> TextBound;// lefternmost part of the selected/displayed item
        List<RectangleF> ButtonBound;// right side bit where we place the button to unfold the dropdown list

        readonly List<string> displayTexts; // the selected item text
        readonly List<string> initialTxts; // initial text to be able to display a hint

        readonly List<List<string>> dropdownlists; // content lists of items for dropdown

        List<List<RectangleF>> dropdownBounds;// list of bounds for each item in dropdown list
        List<RectangleF> dropdownBound;// surrounding bound for the entire dropdown list

        readonly Action<int, int> action; //function sending back the selection to component (i = dropdowncontentlist, j = selected item in that list)

        List<bool> unfolded; // list of bools for unfolded or closed dropdown

        RectangleF scrollBar;// surrounding bound for vertical scroll element
        float scrollStartY; // location of scroll element at drag start
        float dragMouseStartY; // location of mouse at drag start
        float deltaY; // moved Y-location of scroll element
        int maxNoRows = 10;
        bool drag;

        float MinWidth
        {
            get
            {
                float sp = MaxTextWidth(spacerTxts, GH_FontServer.Small);
                float bt = 0;
                for (int i = 0; i < dropdownlists.Count; i++)
                {
                    float tbt = MaxTextWidth(dropdownlists[i], new Font(GH_FontServer.FamilyStandard, 7));
                    if (tbt > bt)
                        bt = tbt;
                }
                float num = Math.Max(Math.Max(sp, bt), 90);
                return num;
            }
            set { MinWidth = value; }
        }
        protected override void Layout()
        {
            base.Layout();

            // first change the width to suit; using max to determine component visualisation style
            FixLayout();

            if (SpacerBounds == null)
                SpacerBounds = new List<RectangleF>();
            if (BorderBound == null)
                BorderBound = new List<RectangleF>();
            if (TextBound == null)
                TextBound = new List<RectangleF>();
            if (ButtonBound == null)
                ButtonBound = new List<RectangleF>();
            if (dropdownBound == null)
                dropdownBound = new List<RectangleF>();
            if (dropdownBounds == null)
                dropdownBounds = new List<List<RectangleF>>();
            if (unfolded == null)
                unfolded = new List<bool>();

            int s = 2; //spacing to edges and internal between boxes

            int h0 = 0;

            bool removeScroll = true;

            for (int i = 0; i < dropdownlists.Count; i++)
            {
                //spacer and title
                if (spacerTxts[i] != "")
                {
                    h0 = 10;
                    RectangleF tempSpacer = new RectangleF(Bounds.X, Bounds.Bottom + s / 2, Bounds.Width, h0);
                    if (SpacerBounds.Count == i || SpacerBounds[i] == null)
                        SpacerBounds.Add(tempSpacer);
                    else
                        SpacerBounds[i] = tempSpacer;
                }

                int h1 = 15; // height border
                int bw = h1; // button width

                // create text box border
                RectangleF tempBorder = new RectangleF(Bounds.X + 2 * s, Bounds.Bottom + h0 + 2 * s, Bounds.Width - 2 - 4 * s, h1);
                if (BorderBound.Count == i || BorderBound[i] == null)
                    BorderBound.Add(tempBorder);
                else
                    BorderBound[i] = tempBorder;

                // text box inside border
                RectangleF tempText = new RectangleF(BorderBound[i].X, BorderBound[i].Y, BorderBound[i].Width - bw, BorderBound[i].Height);
                if (TextBound.Count == i || TextBound[i] == null)
                    TextBound.Add(tempText);
                else
                    TextBound[i] = tempText;

                // button area inside border
                RectangleF tempButton = new RectangleF(BorderBound[i].X + BorderBound[i].Width - bw, BorderBound[i].Y, bw, BorderBound[i].Height);
                if (ButtonBound.Count == i || ButtonBound[i] == null)
                    ButtonBound.Add(tempButton);
                else
                    ButtonBound[i] = tempButton;

                //update component bounds
                Bounds = new RectangleF(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height + h0 + h1 + 4 * s);

                // create list of bounds for dropdown if dropdown is unfolded
                if (unfolded.Count == i)
                    unfolded.Add(new bool()); //ensure we have a bool for every list


                if (unfolded[i]) // if unfolded checked create dropdown list
                {
                    removeScroll = false;

                    if (dropdownBounds[i] == null)
                        dropdownBounds[i] = new List<RectangleF>(); // if first time clicked create new list
                    else
                        dropdownBounds[i].Clear(); // if previously created make sure to clear existing if content has changed
                    for (int j = 0; j < dropdownlists[i].Count; j++)
                    {
                        dropdownBounds[i].Add(new RectangleF(BorderBound[i].X, BorderBound[i].Y + (j + 1) * h1 + s, BorderBound[i].Width, BorderBound[i].Height));
                    }
                    dropdownBound[i] = new RectangleF(BorderBound[i].X, BorderBound[i].Y + h1 + s, BorderBound[i].Width, Math.Min(dropdownlists[i].Count, maxNoRows) * BorderBound[i].Height);

                    //update component size if dropdown is unfolded to be able to capture mouseclicks
                    Bounds = new RectangleF(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height + dropdownBound[i].Height + s);

                    // additional move for the content (moves more than the scroll bar)
                    float contentScroll = 0;

                    // vertical scroll bar if number of items in dropdown list is bigger than max rows allowed
                    if (dropdownlists[i].Count > maxNoRows)
                    {
                        if (scrollBar == null)
                            scrollBar = new RectangleF();

                        // setup size of scroll bar
                        scrollBar.X = dropdownBound[i].X + dropdownBound[i].Width - 8; // locate from right-side of dropdown area
                        // compute height based on number of items in list, but with a minimum size of 2 rows
                        scrollBar.Height = (float)Math.Max(2 * h1, dropdownBound[i].Height * ((double)maxNoRows / ((double)dropdownlists[i].Count)));
                        scrollBar.Width = 8; // width of mouse-grab area (actual scroll bar drawn later)

                        // vertical position (.Y)
                        if (deltaY + scrollStartY >= 0) // handle if user drags above starting point
                        {
                            // dragging downwards:
                            if (dropdownBound[i].Height - scrollBar.Height >= deltaY + scrollStartY) // handles if user drags below bottom point
                            {
                                // update scroll bar position for normal scroll event within bounds
                                scrollBar.Y = dropdownBound[i].Y + deltaY + scrollStartY;
                            }
                            else
                            {
                                // scroll reached bottom
                                scrollStartY = dropdownBound[i].Height - scrollBar.Height;
                                deltaY = 0;
                            }
                        }
                        else
                        {
                            // scroll reached top
                            scrollStartY = 0;
                            deltaY = 0;
                        }

                        // calculate moved position of content
                        float scrollBarMovedPercentage = (dropdownBound[i].Y - scrollBar.Y) / (dropdownBound[i].Height - scrollBar.Height);
                        float scrollContentHeight = dropdownlists[i].Count * h1 - dropdownBound[i].Height;
                        contentScroll = scrollBarMovedPercentage * scrollContentHeight;
                    }

                    // create list of text boxes (we will only draw the visible ones later)
                    dropdownBounds[i] = new List<RectangleF>();
                    for (int j = 0; j < dropdownlists[i].Count; j++)
                    {
                        dropdownBounds[i].Add(new RectangleF(BorderBound[i].X, BorderBound[i].Y + (j + 1) * h1 + s + contentScroll, BorderBound[i].Width, h1));
                    }

                }
                else
                {
                    if (dropdownBounds != null)
                    {
                        if (dropdownBounds.Count == i)
                            dropdownBounds.Add(new List<RectangleF>());
                        if (dropdownBounds[i] != null)
                            dropdownBounds[i].Clear();
                        if (dropdownBound.Count == i)
                            dropdownBound.Add(new RectangleF());
                        else
                            dropdownBound[i] = new RectangleF();
                    }
                }
            }

            if (removeScroll)
            {
                scrollBar = new RectangleF();
                scrollStartY = 0;
            }
        }


        protected override void Render(GH_Canvas canvas, System.Drawing.Graphics graphics, GH_CanvasChannel channel)
        {
            base.Render(canvas, graphics, channel);

            if (channel == GH_CanvasChannel.Objects)
            {
                Pen spacer = new Pen(DropDownColours.SpacerColour);
                Pen pen = new Pen(DropDownColours.BorderColour)
                {
                    Width = 0.5f
                };

                Font sml = GH_FontServer.Small;
                // adjust fontsize to high resolution displays
                sml = new Font(sml.FontFamily, sml.Size / GH_GraphicsUtil.UiScale, FontStyle.Regular);

                for (int i = 0; i < dropdownlists.Count; i++)
                {
                    //Draw divider line
                    if (spacerTxts[i] != "")
                    {
                        graphics.DrawString(spacerTxts[i], sml, DropDownColours.AnnotationTextDark, SpacerBounds[i], GH_TextRenderingConstants.CenterCenter);
                        graphics.DrawLine(spacer, SpacerBounds[i].X, SpacerBounds[i].Y + SpacerBounds[i].Height / 2, SpacerBounds[i].X + (SpacerBounds[i].Width - GH_FontServer.StringWidth(spacerTxts[i], sml)) / 2 - 4, SpacerBounds[i].Y + SpacerBounds[i].Height / 2);
                        graphics.DrawLine(spacer, SpacerBounds[i].X + (SpacerBounds[i].Width - GH_FontServer.StringWidth(spacerTxts[i], sml)) / 2 + GH_FontServer.StringWidth(spacerTxts[i], sml) + 4, SpacerBounds[i].Y + SpacerBounds[i].Height / 2, SpacerBounds[i].X + SpacerBounds[i].Width, SpacerBounds[i].Y + SpacerBounds[i].Height / 2);
                    }

                    // Draw selected item
                    // set font and colour depending on inital or selected text
                    Font font = new Font(GH_FontServer.FamilyStandard, 7);
                    // adjust fontsize to high resolution displays
                    font = new Font(font.FontFamily, font.Size / GH_GraphicsUtil.UiScale, FontStyle.Regular);
                    Brush fontColour = DropDownColours.AnnotationTextDark;
                    if (initialTxts != null)
                    {
                        if (displayTexts[i] == initialTxts[i])
                        {
                            pen = new Pen(DropDownColours.BorderColour);
                            font = sml;
                            fontColour = Brushes.Gray;
                        }
                    }

                    // background
                    Brush background = new SolidBrush(Color.LightGray);
                    // background
                    graphics.FillRectangle(background, BorderBound[i]);
                    // border
                    graphics.DrawRectangle(pen, BorderBound[i].X, BorderBound[i].Y, BorderBound[i].Width, BorderBound[i].Height);
                    // text
                    graphics.DrawString(displayTexts[i], font, fontColour, TextBound[i], GH_TextRenderingConstants.NearCenter);
                    // draw dropdown arrow
                    DrawDropDownButton(graphics, new PointF(ButtonBound[i].X + ButtonBound[i].Width / 2, ButtonBound[i].Y + ButtonBound[i].Height / 2), Color.DarkGray, 15);

                    // draw dropdown list
                    font = new Font(GH_FontServer.FamilyStandard, 7);
                    // adjust fontsize to high resolution displays
                    font = new Font(font.FontFamily, font.Size / GH_GraphicsUtil.UiScale, FontStyle.Regular);
                    fontColour = DropDownColours.AnnotationTextDark;
                    if (unfolded[i])
                    {
                        Pen penborder = new Pen(Brushes.Gray);
                        Brush dropdownbackground = new SolidBrush(Color.LightGray);
                        penborder.Width = 0.3f;
                        for (int j = 0; j < dropdownBounds[i].Count; j++)
                        {
                            RectangleF listItem = dropdownBounds[i][j];
                            if (listItem.Y < dropdownBound[i].Y)
                            {
                                if (listItem.Y + listItem.Height < dropdownBound[i].Y)
                                {
                                    dropdownBounds[i][j] = new RectangleF();
                                    continue;
                                }
                                else
                                {
                                    listItem.Height = listItem.Height - (dropdownBound[i].Y - listItem.Y);
                                    listItem.Y = dropdownBound[i].Y;
                                    dropdownBounds[i][j] = listItem;
                                }
                            }
                            else if (listItem.Y + listItem.Height > dropdownBound[i].Y + dropdownBound[i].Height)
                            {
                                if (listItem.Y > dropdownBound[i].Y + dropdownBound[i].Height)
                                {
                                    dropdownBounds[i][j] = new RectangleF();
                                    continue;
                                }
                                else
                                {
                                    listItem.Height = dropdownBound[i].Y + dropdownBound[i].Height - listItem.Y;
                                    dropdownBounds[i][j] = listItem;
                                }
                            }

                            // background
                            graphics.FillRectangle(dropdownbackground, dropdownBounds[i][j]);
                            // border
                            graphics.DrawRectangle(penborder, dropdownBounds[i][j].X, dropdownBounds[i][j].Y, dropdownBounds[i][j].Width, dropdownBounds[i][j].Height);
                            // text
                            if (dropdownBounds[i][j].Height > 2)
                                graphics.DrawString(dropdownlists[i][j], font, fontColour, dropdownBounds[i][j], GH_TextRenderingConstants.NearCenter);
                        }
                        // border
                        graphics.DrawRectangle(pen, dropdownBound[i].X, dropdownBound[i].Y, dropdownBound[i].Width, dropdownBound[i].Height);

                        // draw vertical scroll bar
                        Brush scrollbar = new SolidBrush(Color.FromArgb(drag ? 160 : 120, Color.Black));
                        Pen scrollPen = new Pen(scrollbar);
                        scrollPen.Width = scrollBar.Width - 2;
                        scrollPen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                        scrollPen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                        graphics.DrawLine(scrollPen, scrollBar.X + 4, scrollBar.Y + 4, scrollBar.X + 4, scrollBar.Y + scrollBar.Height - 4);
                    }
                }
            }
        }
        public override GH_ObjectResponse RespondToMouseUp(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                GH_Component comp = Owner as GH_Component;
                if (drag)
                {
                    // if drag was true then we release it here:
                    scrollStartY += deltaY;
                    deltaY = 0;
                    drag = false;
                    comp.ExpireSolution(true);
                    return GH_ObjectResponse.Release;
                }

                for (int i = 0; i < dropdownlists.Count; i++)
                {
                    System.Drawing.RectangleF rec = BorderBound[i];
                    if (rec.Contains(e.CanvasLocation))
                    {
                        unfolded[i] = !unfolded[i];
                        // close any other dropdowns that may be unfolded
                        for (int j = 0; j < unfolded.Count; j++)
                        {
                            if (j == i)
                                continue;
                            unfolded[j] = false;
                        }
                        comp.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }

                    if (unfolded[i])
                    {
                        System.Drawing.RectangleF rec2 = dropdownBound[i];
                        if (rec2.Contains(e.CanvasLocation))
                        {
                            for (int j = 0; j < dropdownBounds[i].Count; j++)
                            {
                                System.Drawing.RectangleF rec3 = dropdownBounds[i][j];
                                if (rec3.Contains(e.CanvasLocation))
                                {
                                    if (displayTexts[i] != dropdownlists[i][j])
                                    {
                                        // record an undo event so that user can ctrl + z
                                        comp.RecordUndoEvent("Selected " + dropdownlists[i][j]);

                                        // change the displayed text on canvas
                                        displayTexts[i] = dropdownlists[i][j];

                                        // if initial texts exists then change all dropdowns below this one to the initial description
                                        if (initialTxts != null)
                                        {
                                            for (int k = i + 1; k < dropdownlists.Count; k++)
                                                displayTexts[k] = initialTxts[k];
                                        }

                                        // send the selected item back to component (i = dropdownlist index, j = selected item in that list)
                                        action(i, j);

                                        // close the dropdown
                                        unfolded[i] = !unfolded[i];

                                        // recalculate component
                                        comp.ExpireSolution(true);
                                    }
                                    else
                                    {
                                        unfolded[i] = !unfolded[i];
                                        comp.ExpireSolution(true);
                                    }
                                    return GH_ObjectResponse.Handled;
                                }
                            }
                        }
                        else
                        {
                            unfolded[i] = !unfolded[i];
                            comp.ExpireSolution(true);
                            return GH_ObjectResponse.Handled;
                        }
                    }
                }
            }
            return base.RespondToMouseUp(sender, e);
        }
        public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            for (int i = 0; i < dropdownlists.Count; i++)
            {
                if (unfolded[i])
                {
                    if (e.Button == System.Windows.Forms.MouseButtons.Left)
                    {
                        System.Drawing.RectangleF rec = scrollBar;
                        GH_Component comp = Owner as GH_Component;
                        if (rec.Contains(e.CanvasLocation))
                        {
                            dragMouseStartY = e.CanvasLocation.Y;
                            drag = true;
                            comp.ExpireSolution(true);
                            return GH_ObjectResponse.Capture;
                        }
                    }
                }
            }
            return base.RespondToMouseDown(sender, e);
        }
        public override GH_ObjectResponse RespondToMouseMove(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (drag)
            {
                GH_Component comp = Owner as GH_Component;

                deltaY = e.CanvasLocation.Y - dragMouseStartY;

                comp.ExpireSolution(true);
                return GH_ObjectResponse.Ignore;
            }

            for (int i = 0; i < ButtonBound.Count; i++)
            {
                if (ButtonBound[i].Contains(e.CanvasLocation))
                {
                    mouseOver = true;
                    sender.Cursor = System.Windows.Forms.Cursors.Hand;
                    return GH_ObjectResponse.Capture;
                }
            }
            if (mouseOver)
            {
                mouseOver = false;
                Grasshopper.Instances.CursorServer.ResetCursor(sender);
                return GH_ObjectResponse.Release;
            }

            return base.RespondToMouseMove(sender, e);
        }
        bool mouseOver;
        protected void FixLayout()
        {
            float width = this.Bounds.Width; // initial component width before UI overrides
            float num = Math.Max(width, MinWidth); // number for new width
            float num2 = 0f; // value for increased width (if any)

            // first check if original component must be widened
            if (num > width)
            {
                num2 = num - width; // change in width
                // update component bounds to new width
                this.Bounds = new RectangleF(
                    this.Bounds.X - num2 / 2f,
                    this.Bounds.Y,
                    num,
                    this.Bounds.Height);
            }

            // secondly update position of input and output parameter text
            // first find the maximum text width of parameters

            foreach (IGH_Param item in base.Owner.Params.Output)
            {
                PointF pivot = item.Attributes.Pivot; // original anchor location of output
                RectangleF bounds = item.Attributes.Bounds; // text box itself
                item.Attributes.Pivot = new PointF(
                    pivot.X + num2 / 2f, // move anchor to the right
                    pivot.Y);
                item.Attributes.Bounds = new RectangleF(
                    bounds.Location.X + num2 / 2f,  // move text box to the right
                    bounds.Location.Y,
                    bounds.Width,
                    bounds.Height);
            }
            // for input params first find the widest input text box as these are right-aligned
            float inputwidth = 0f;
            foreach (IGH_Param item in base.Owner.Params.Input)
            {
                if (inputwidth < item.Attributes.Bounds.Width)
                    inputwidth = item.Attributes.Bounds.Width;
            }
            foreach (IGH_Param item2 in base.Owner.Params.Input)
            {
                PointF pivot2 = item2.Attributes.Pivot; // original anchor location of input
                RectangleF bounds2 = item2.Attributes.Bounds;
                item2.Attributes.Pivot = new PointF(
                    pivot2.X - num2 / 2f + inputwidth, // move to the left, move back by max input width
                    pivot2.Y);
                item2.Attributes.Bounds = new RectangleF(
                     bounds2.Location.X - num2 / 2f,
                     bounds2.Location.Y,
                     bounds2.Width,
                     bounds2.Height);
            }
        }
        public static float MaxTextWidth(List<string> spacerTxts, Font font)
        {
            float sp = new float(); //width of spacer text

            // adjust fontsize to high resolution displays
            font = new Font(font.FontFamily, font.Size / GH_GraphicsUtil.UiScale, FontStyle.Regular);

            for (int i = 0; i < spacerTxts.Count; i++)
            {
                if (GH_FontServer.StringWidth(spacerTxts[i], font) + 8 > sp)
                    sp = GH_FontServer.StringWidth(spacerTxts[i], font) + 8;
            }
            return sp;
        }
        public static void DrawDropDownButton(Graphics graphics, PointF center, Color colour, int rectanglesize)
        {
            Pen pen = new Pen(new SolidBrush(colour))
            {
                Width = rectanglesize / 8
            };

            graphics.DrawLines(
                pen, new PointF[]
                {
                new PointF(center.X - rectanglesize / 4, center.Y - rectanglesize / 8),
                new PointF(center.X, center.Y + rectanglesize / 6),
                new PointF(center.X + rectanglesize / 4, center.Y - rectanglesize / 8)
                });

        }
    }


    /// <summary>
    /// Colour class holding the main colours used in colour scheme. 
    /// Make calls to this class to be able to easy update colours.
    /// 
    /// </summary>
    public class DropDownColours
    {
        //Set colours for Component UI
        static readonly Color Primary = Color.FromArgb(255, 229, 27, 36);
        static readonly Color Primary_light = Color.FromArgb(255, 255, 93, 78);
        static readonly Color Primary_dark = Color.FromArgb(255, 170, 0, 0);
        public static Brush ButtonColor
        {
            get { return new SolidBrush(Primary); }
        }
        public static Brush ClickedButtonColor
        {
            get { return new SolidBrush(Primary_light); }
        }
        public static Color BorderColour
        {
            get { return Primary_dark; }
        }
        public static Color ClickedBorderColour
        {
            get { return Primary; }
        }
        public static Color SpacerColour
        {
            get { return Color.DarkGray; }
        }
        public static Brush AnnotationTextDark
        {
            get { return Brushes.Black; }
        }
        public static Brush AnnotationTextBright
        {
            get { return Brushes.White; }
        }
    }
}