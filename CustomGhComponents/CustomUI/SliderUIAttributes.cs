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
    /// Class to create custom component UI with a single dropdown menu
    /// 
    /// Look at gsaDropDownSingle.cs for an example of how to call this method.
    /// 
    /// To use this method override CreateAttributes() in component class and set m_attributes = new DropDownComponentUI(...
    /// </summary>
    public class SliderUIAttributes : GH_ComponentAttributes
    {
        public SliderUIAttributes(GH_Component owner, Action<double> sliderValue, Action<double, double> setMaxMinVals, double initValue, double minVal, double maxVal, int digits, string spacerText = "") : base(owner)
        {
            SpacerTxt = spacerText;
            ReturnSliderValue = sliderValue;
            ChangeMaxMin = setMaxMinVals;
            MinValue = minVal;
            MaxValue = maxVal;
            CurrentValue = initValue;
            noDigits = digits;
            first = true;
        }

        RectangleF SliderBound;// area where the slider is displayed
        RectangleF SliderValTextBound;// bound where text is displayed
        RectangleF GrabBound;// bound around the value grab
        double MinValue;
        double MaxValue;
        double CurrentValue;
        int noDigits;
        bool first;
        float scrollStartX; // location of scroll element at drag start
        float dragMouseStartX; // location of mouse at drag start
        float deltaX; // moved Y-location of scroll element
        bool dragX;
        bool mouseOver;
        readonly Action<double> ReturnSliderValue;
        readonly Action<double, double> ChangeMaxMin;
        RectangleF SpacerBounds;
        readonly string SpacerTxt;

        float MinWidth
        {
            get
            {
                List<string> spacers = new List<string>();
                spacers.Add(SpacerTxt);
                float sp = MaxTextWidth(spacers, GH_FontServer.Small);
                List<string> buttons = new List<string>();
                float num = Math.Max(sp, 90);
                return num;
            }
            set { MinWidth = value; }
        }
        protected override void Layout()
        {
            base.Layout();

            // first change the width to suit; using max to determine component visualisation style
            FixLayout();

            int s = 2; // spacing to edges and internal between boxes

            // create bound for spacer and title
            int h0 = 0; // height of spacer bound
            if (SpacerTxt != "")
            {
                h0 = 10;
                SpacerBounds = new RectangleF(Bounds.X, Bounds.Bottom + s / 2, Bounds.Width, h0);
            }

            int hslider = 15; // height of bound for slider
            int bhslider = 10; // height and width of grab

            // create overall bound for slider
            SliderBound = new RectangleF(Bounds.X + 2 * s, Bounds.Bottom + h0 + 2 * s, Bounds.Width - 2 - 4 * s, hslider);

            // slider grab 
            GrabBound = new RectangleF(SliderBound.X + deltaX + scrollStartX, SliderBound.Y + SliderBound.Height / 2 - bhslider / 2, bhslider, bhslider);

            // round current value for snapping
            CurrentValue = Math.Round(CurrentValue, noDigits);
            double dragPercentage;

            // when component is initiated or value range is changed
            // calculate position of grab
            if (first)
            {
                dragPercentage = (CurrentValue - MinValue) / (MaxValue - MinValue);
                scrollStartX = (float)(dragPercentage * (SliderBound.Width - GrabBound.Width));
                first = false;
            }

            // horizontal position (.X)
            if (deltaX + scrollStartX >= 0) // handle if user drags left of starting point
            {
                // dragging right-wards:
                if (SliderBound.Width - GrabBound.Width >= deltaX + scrollStartX) // handles if user drags below bottom point
                {
                    // update scroll bar position for normal scroll event within bounds
                    dragPercentage = (deltaX + scrollStartX) / (SliderBound.Width - GrabBound.Width);
                    CurrentValue = Math.Round(MinValue + dragPercentage * (MaxValue - MinValue), noDigits);
                    dragPercentage = (CurrentValue - MinValue) / (MaxValue - MinValue);
                }
                else
                {
                    // scroll reached end
                    dragPercentage = 1;
                    scrollStartX = SliderBound.Width - GrabBound.Width;
                    deltaX = 0;
                    CurrentValue = MaxValue;
                }
            }
            else
            {
                // scroll reached start
                dragPercentage = 0;
                scrollStartX = 0;
                deltaX = 0;
                CurrentValue = MinValue;
            }

            // set grab item position with snap
            GrabBound.X = SliderBound.X + (float)(dragPercentage * (SliderBound.Width - GrabBound.Width));

            // return new current value to component
            ReturnSliderValue(CurrentValue);

            // text box for value display

            if (CurrentValue < (MaxValue - MinValue) / 2) // we are in the left half of the range
                SliderValTextBound = new RectangleF(GrabBound.X + GrabBound.Width, SliderBound.Y, SliderBound.X + SliderBound.Width - GrabBound.X + GrabBound.Width, SliderBound.Height);
            else // we are in the right half of the range
                SliderValTextBound = new RectangleF(SliderBound.X, SliderBound.Y, GrabBound.X - SliderBound.X, SliderBound.Height);

            //update component bounds
            Bounds = new RectangleF(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height + h0 + hslider + 4 * s);

        }

        protected override void Render(GH_Canvas canvas, System.Drawing.Graphics graphics, GH_CanvasChannel channel)
        {
            base.Render(canvas, graphics, channel);

            if (channel == GH_CanvasChannel.Objects)
            {
                //Draw divider line
                if (SpacerTxt != "")
                {
                    Pen spacer = new Pen(SliderColours.SpacerColour);
                    Font sml = GH_FontServer.Small;
                    // adjust fontsize to high resolution displays
                    sml = new Font(sml.FontFamily, sml.Size / GH_GraphicsUtil.UiScale, FontStyle.Regular);

                    graphics.DrawString(SpacerTxt, sml, SliderColours.AnnotationTextDark, SpacerBounds, GH_TextRenderingConstants.CenterCenter);
                    graphics.DrawLine(spacer, SpacerBounds.X, SpacerBounds.Y + SpacerBounds.Height / 2, SpacerBounds.X + (SpacerBounds.Width - GH_FontServer.StringWidth(SpacerTxt, sml)) / 2 - 4, SpacerBounds.Y + SpacerBounds.Height / 2);
                    graphics.DrawLine(spacer, SpacerBounds.X + (SpacerBounds.Width - GH_FontServer.StringWidth(SpacerTxt, sml)) / 2 + GH_FontServer.StringWidth(SpacerTxt, sml) + 4, SpacerBounds.Y + SpacerBounds.Height / 2, SpacerBounds.X + SpacerBounds.Width, SpacerBounds.Y + SpacerBounds.Height / 2);
                }

                // draw drag line and intervals
                Pen line = new Pen(SliderColours.RailColour);
                graphics.DrawLine(line, new PointF(SliderBound.X + GrabBound.Width / 2, SliderBound.Y + SliderBound.Height / 2), new PointF(SliderBound.X + SliderBound.Width - GrabBound.Width / 2, SliderBound.Y + SliderBound.Height / 2));
                //graphics.DrawLine(line, new PointF(BorderBound.X + GrabBound.Width / 2, BorderBound.Y + BorderBound.Height / 3), new PointF(BorderBound.X + GrabBound.Width / 2, BorderBound.Y + BorderBound.Height * 2 / 3));
                //graphics.DrawLine(line, new PointF(BorderBound.X + BorderBound.Width - GrabBound.Width / 2, BorderBound.Y + BorderBound.Height / 3), new PointF(BorderBound.X + BorderBound.Width - GrabBound.Width / 2, BorderBound.Y + BorderBound.Height * 2 / 3));

                // draw grab item
                Pen pen = new Pen(SliderColours.DragElementEdge);
                pen.Width = 2f;
                RectangleF button = new RectangleF(GrabBound.X, GrabBound.Y, GrabBound.Width, GrabBound.Height);
                button.Inflate(-2, -2);
                Brush fill = new SolidBrush(SliderColours.DragElementFill);
                graphics.FillEllipse(fill, button);
                graphics.DrawEllipse(pen, button);

                // Draw display value text
                Font font = new Font(GH_FontServer.FamilyStandard, 7);
                // adjust fontsize to high resolution displays
                font = new Font(font.FontFamily, font.Size / GH_GraphicsUtil.UiScale, FontStyle.Regular);
                string val = string.Format(new System.Globalization.NumberFormatInfo() { NumberDecimalDigits = noDigits }, "{0:F}", new decimal(CurrentValue));

                graphics.DrawString(val, font, SliderColours.AnnotationTextDark, SliderValTextBound, ((CurrentValue - MinValue) / (MaxValue - MinValue) < 0.5) ? GH_TextRenderingConstants.NearCenter : GH_TextRenderingConstants.FarCenter);
            }
        }
        public override GH_ObjectResponse RespondToMouseUp(GH_Canvas sender, GH_CanvasMouseEvent e)
        {

            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                GH_Component comp = Owner as GH_Component;
                if (dragX)
                {
                    // if drag was true then we release it here:
                    scrollStartX += deltaX;
                    deltaX = 0;
                    dragX = false;
                    comp.ExpireSolution(true);
                    return GH_ObjectResponse.Release;
                }

            }
            return base.RespondToMouseUp(sender, e);
        }

        public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                System.Drawing.RectangleF rec = GrabBound;
                GH_Component comp = Owner as GH_Component;
                if (rec.Contains(e.CanvasLocation))
                {
                    dragMouseStartX = e.CanvasLocation.X;
                    dragX = true;
                    comp.ExpireSolution(true);
                    return GH_ObjectResponse.Capture;
                }
            }
            return base.RespondToMouseDown(sender, e);
        }

        public override GH_ObjectResponse RespondToMouseMove(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (dragX)
            {
                GH_Component comp = Owner as GH_Component;

                deltaX = e.CanvasLocation.X - dragMouseStartX;

                Grasshopper.Instances.CursorServer.AttachCursor(sender, "GH_NumericSlider");

                comp.ExpireSolution(true);
                return GH_ObjectResponse.Ignore;
            }

            RectangleF rec = GrabBound;
            if (rec.Contains(e.CanvasLocation))
            {
                mouseOver = true;
                Grasshopper.Instances.CursorServer.AttachCursor(sender, "GH_NumericSlider");
                return GH_ObjectResponse.Capture;
            }

            if (mouseOver)
            {
                mouseOver = false;
                Grasshopper.Instances.CursorServer.ResetCursor(sender);
                return GH_ObjectResponse.Release;
            }

            return base.RespondToMouseMove(sender, e);
        }
        public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            RectangleF rec = GrabBound;
            if (rec.Contains(e.CanvasLocation))
            {
                Grasshopper.Kernel.Special.GH_NumberSlider hiddenSlider = new Grasshopper.Kernel.Special.GH_NumberSlider();
                hiddenSlider.Slider.Maximum = (decimal)MaxValue;
                hiddenSlider.Slider.Minimum = (decimal)MinValue;
                hiddenSlider.Slider.DecimalPlaces = noDigits;
                hiddenSlider.Slider.Type = noDigits == 0 ? Grasshopper.GUI.Base.GH_SliderAccuracy.Integer : Grasshopper.GUI.Base.GH_SliderAccuracy.Float;
                hiddenSlider.Name = Owner.Name + " Slider";
                hiddenSlider.Slider.Value = (decimal)CurrentValue;
                Grasshopper.GUI.GH_NumberSliderPopup gH_MenuSliderForm = new Grasshopper.GUI.GH_NumberSliderPopup();
                GH_WindowsFormUtil.CenterFormOnCursor(gH_MenuSliderForm, true);
                gH_MenuSliderForm.Setup(hiddenSlider);
                //hiddenSlider.PopupEditor();
                var res = gH_MenuSliderForm.ShowDialog();
                if (res == DialogResult.OK)
                {
                    first = true;
                    MaxValue = (double)hiddenSlider.Slider.Maximum;
                    MinValue = (double)hiddenSlider.Slider.Minimum;
                    CurrentValue = (double)hiddenSlider.Slider.Value;
                    noDigits = hiddenSlider.Slider.Type == Grasshopper.GUI.Base.GH_SliderAccuracy.Integer ? 0 : hiddenSlider.Slider.DecimalPlaces;
                    ChangeMaxMin(MaxValue, MinValue);
                    Owner.OnDisplayExpired(false);
                    Owner.ExpireSolution(true);
                    return GH_ObjectResponse.Handled;
                }
            }
            return GH_ObjectResponse.Ignore;
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
    public class SliderColours
    {
        //Set colours for Component UI
        static readonly Color Primary = Color.FromArgb(255, 229, 27, 36);
        static readonly Color Primary_light = Color.FromArgb(255, 255, 93, 78);
        static readonly Color Primary_dark = Color.FromArgb(255, 170, 0, 0);
        public static Brush ButtonColor
        {
            get { return new SolidBrush(Primary); }
        }
        public static Brush DragElementEdge
        {
            get { return new SolidBrush(Primary); }
        }
        public static Color DragElementFill
        {
            get { return Color.FromArgb(255, 244, 244, 244); }
        }
        public static Color RailColour
        {
            get { return Color.FromArgb(255, 164, 164, 164); }
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
