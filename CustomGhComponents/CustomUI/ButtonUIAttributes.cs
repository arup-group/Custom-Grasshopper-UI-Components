using Grasshopper.Kernel.Attributes;
using Grasshopper.GUI.Canvas;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using System;
using System.Drawing;
using System.Collections.Generic;
using Rhino.Display;

namespace CustomUI
{
    /// <summary>
    /// Class to create custom component UI with a button
    /// 
    /// To use this method override CreateAttributes() in component class and set m_attributes = new ButtonUIAttributes(...
    /// </summary>
    public class ButtonUIAttributes : GH_ComponentAttributes
    {
        public ButtonUIAttributes(GH_Component owner, string displayText, Action clickHandle, string spacerText = "") : base(owner) { buttonText = displayText; SpacerTxt = spacerText; action = clickHandle; }

        readonly string buttonText; // text to be displayed
        System.Drawing.RectangleF ButtonBounds; // area for button to be displayed
        readonly Action action;

        RectangleF SpacerBounds; // spacer between standard component and button
        readonly string SpacerTxt; // text to be displayed on spacer

        bool mouseDown;
        float MinWidth
        {
            get
            {
                List<string> spacers = new List<string>();
                spacers.Add(SpacerTxt);
                float sp = MaxTextWidth(spacers, GH_FontServer.Small);
                List<string> buttons = new List<string>();
                buttons.Add(buttonText);
                float bt = MaxTextWidth(buttons, GH_FontServer.Standard);

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

            int s = 2; //spacing to edges and internal between boxes

            int h0 = 0;
            //spacer and title
            if (SpacerTxt != "")
            {
                h0 = 10;
                SpacerBounds = new RectangleF(Bounds.X, Bounds.Bottom + s / 2, Bounds.Width, h0);
            }

            int h1 = 20; // height of button
            // create text box placeholders
            ButtonBounds = new RectangleF(Bounds.X + 2 * s, Bounds.Bottom + h0 + 2 * s, Bounds.Width - 2 - 4 * s, h1);

            //update component bounds
            Bounds = new RectangleF(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height + h0 + h1 + 4 * s);
        }

        protected override void Render(GH_Canvas canvas, System.Drawing.Graphics graphics, GH_CanvasChannel channel)
        {
            base.Render(canvas, graphics, channel);

            if (channel == GH_CanvasChannel.Objects)
            {
                Pen spacer = new Pen(ButtonColours.SpacerColour);
                Pen pen = new Pen(mouseDown ? ButtonColours.ClickedBorderColour : ButtonColours.BorderColour)
                {
                    Width = 0.5f
                };
                Font font = GH_FontServer.Standard;
                // adjust fontsize to high resolution displays
                font = new Font(font.FontFamily, font.Size / GH_GraphicsUtil.UiScale, FontStyle.Regular);

                Font sml = GH_FontServer.Small;
                // adjust fontsize to high resolution displays
                sml = new Font(sml.FontFamily, sml.Size / GH_GraphicsUtil.UiScale, FontStyle.Regular);

                //Draw divider line
                if (SpacerTxt != "")
                {
                    graphics.DrawString(SpacerTxt, sml, ButtonColours.AnnotationTextDark, SpacerBounds, GH_TextRenderingConstants.CenterCenter);
                    graphics.DrawLine(spacer, SpacerBounds.X, SpacerBounds.Y + SpacerBounds.Height / 2, SpacerBounds.X + (SpacerBounds.Width - GH_FontServer.StringWidth(SpacerTxt, sml)) / 2 - 4, SpacerBounds.Y + SpacerBounds.Height / 2);
                    graphics.DrawLine(spacer, SpacerBounds.X + (SpacerBounds.Width - GH_FontServer.StringWidth(SpacerTxt, sml)) / 2 + GH_FontServer.StringWidth(SpacerTxt, sml) + 4, SpacerBounds.Y + SpacerBounds.Height / 2, SpacerBounds.X + SpacerBounds.Width, SpacerBounds.Y + SpacerBounds.Height / 2);
                }

                // Draw button box
                graphics.FillRectangle(mouseDown ? ButtonColours.ClickedButtonColor : ButtonColours.ButtonColor, ButtonBounds);
                graphics.DrawRectangle(pen, ButtonBounds.X, ButtonBounds.Y, ButtonBounds.Width, ButtonBounds.Height);
                graphics.DrawString(buttonText, font, mouseDown ? ButtonColours.AnnotationTextDark : ButtonColours.AnnotationTextBright, ButtonBounds, GH_TextRenderingConstants.CenterCenter);
            }
        }
        public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                System.Drawing.RectangleF rec = ButtonBounds;
                if (rec.Contains(e.CanvasLocation))
                {
                    mouseDown = true;
                    Owner.ExpireSolution(true);
                    return GH_ObjectResponse.Capture;
                }
            }
            return base.RespondToMouseDown(sender, e);
        }

        public override GH_ObjectResponse RespondToMouseUp(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                System.Drawing.RectangleF rec = ButtonBounds;
                if (rec.Contains(e.CanvasLocation))
                {
                    if (mouseDown)
                    {
                        mouseDown = false;
                        action();
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Release;
                    }
                }
            }
            return base.RespondToMouseUp(sender, e);
        }


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
    }


    /// <summary>
    /// Colour class holding the main colours used in colour scheme. 
    /// Make calls to this class to be able to easy update colours.
    /// 
    /// </summary>
    public class ButtonColours
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