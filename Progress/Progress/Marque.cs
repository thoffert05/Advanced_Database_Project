///--------------------------------------------------------------------------------------------------------------------------------------------
/// Marque class
/// 
/// This class is used to update a scrolling marque on the progress bar to show the user that the progress bar is still working and not frozen.  
/// It uses a linear gradient brush to create a fading effect on the marque as it scrolls across the progress bar.  The marque will only be shown 
/// when the progress is greater than a certain percentage to avoid showing it when the progress is not enough to show a marque
/// 
/// Author: Anthony Hoffert
///--------------------------------------------------------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgressBar
{
    class Marque
    {
        //how long to wait before showing the marque again after it has reached the end of the progress bar
        public float delayBeforeShowingMarqueAgain = 0.3f;
        //how fast the marque should scroll across the progress bar, this is a percentage of the progress bar per update
        public float MarqueSpeed = 0.005f;
        //how far can the marque scroll across the bar before it reaches the end of the current progress and must reset
        public float MaxGradientPercent = 0.5f;
        //how wide the marque should be as a percentage of the progress bar, this is used to determine how wide the gradient should be and how
        //wide the rectangles should be when drawing the marque
        float width = 0.05f;
        //where the middle of the gradiant is currently at on the progress bar
        float gradientPosition = 0;
        //where the left most edge of the marque is currently at on the progress bar, this is used to determine where to draw the first rectangle of the marque
        float firstPosition;
        //the smaller of the two progress bars
        float smallerProgress;
        //the bigger of the two progress bars
        float biggerProgress;
        //used to compute where the marque should end on its drawing
        float cutoff;
        //used to compute where the second half of the marque should start drawing on the bar
        float SecondPosition;
        //how the first half of the marque should fade its colors
        float[] relativeIntensities1 = new float[] { 0.0f, 0.5f, 1.0f };
        //defines how the colors should be distributed across first half of the marque for the gradient, this is used to create the fading effect on the marque
        float[] relativePositions1 = new float[] { 0.0f, 0.5f, 1.0f };
        //the type of blending the gradient the first half of should use
        LinearGradientBrush StartGradient;
        //how the first half of the marque should fade its colors
        Blend FadeOutblend = new Blend();
        //how the second half of the marque should fade its colors
        float[] relativeIntensities2 = new float[] { 1.0f, 0.5f, 0.0f };
        //defines how the colors should be distributed across second half of the marque for the gradient, this is used to create the fading effect on the marque
        float[] relativePositions2 = new float[] { 0.0f, 0.5f, 1.0f };
        //the type of blending the gradient the second half of should use
        LinearGradientBrush EndGradient;
        //how the second half of the marque should fade its colors
        Blend FadeInblend = new Blend();
        //marque start and end colors
        Color StartColor, EndColor;
        //how wide the marque should be
        float proposedWidth;
        //rectangle for the first half of the marue
        RectangleF firstRect = new RectangleF();
        //used for the color gradient for the first half of the marque
        RectangleF firstColorRect = new RectangleF();
        //rectangle for the second half of the marue
        RectangleF SecondRect = new RectangleF();
        //used for the color gradient for the second half of the marque
        RectangleF SecondColorRect = new RectangleF();
        /// <summary>
        /// this initializes the marque by setting up the blending for the gradients of the marque, this is used to create the fading effect on the marque as it scrolls across the progress bar
        /// </summary>
        public Marque()
        {
            FadeOutblend.Factors = relativeIntensities1;
            FadeOutblend.Positions = relativePositions1;
            FadeInblend.Factors = relativeIntensities2;
            FadeInblend.Positions = relativePositions2;
        }
        /// <summary>
        /// gets the width of the marque, this is used to determine how wide the marque should be when drawing it on the progress bar
        /// </summary>
        /// <returns>the width of the marque</returns>
        public float getWidth()
        {
            return width;
        }
        /// <summary>
        /// gets the posisiton of the center of the marque
        /// </summary>
        /// <returns>the center of the marque</returns>
        public float getPosition()
        {
            return gradientPosition;
        }
        /// <summary>
        /// updates the position of the marque
        /// </summary>
        public void updateMarque()
        {
            gradientPosition += MarqueSpeed;
            if (gradientPosition > MaxGradientPercent)
                gradientPosition = 0f;
        }
        /// <summary>
        /// draws the marque on the progress bar using the provided graphics object, this is used to show the user that the progress bar is still working and not frozen, it uses a linear gradient brush to create a fading effect on the marque as it scrolls across the progress bar.  The marque will only be shown when the progress is greater than a certain percentage to avoid showing it when the progress is not enough to show a marque
        /// </summary>
        /// <param name="progress1Color">color of the first progress bar</param>
        /// <param name="progress2Color">color of the second progress bar</param>
        /// <param name="progress1">the progress the first bar is at</param>
        /// <param name="progress2">the progress the second bar is at</param>
        /// <param name="FullWidth">the entier width of the whole progress bar window</param>
        /// <param name="Height">the height of the progress bar window</param>
        /// <param name="g">graphics for drawing the progress bar onto the picture box</param>
        public void DrawMarque(Color progress1Color, Color progress2Color, float progress1, float progress2, int FullWidth, int Height, ref Graphics g)
        {
            try {
                //set width
                width = 0.05f;
                //determine start color and end color
                if (progress1 < progress2)
                {
                    smallerProgress = progress1;
                    biggerProgress = progress2;
                    StartColor = progress1Color;
                    EndColor = progress2Color;
                    cutoff = progress2;
                }
                else
                {
                    smallerProgress = progress2;
                    biggerProgress = progress1;
                    StartColor = progress2Color;
                    EndColor = progress1Color;
                    cutoff = progress1;
                }
                //if the marque has reached the end of the progress bar then set the position before the beginning by the delay amount
                if (gradientPosition > cutoff)
                {
                    //set the marque position before the beginning of the progress bar by the delay amount
                    gradientPosition = -delayBeforeShowingMarqueAgain;
                    //exit the function as there is no need to draw the marque since it is before the beginning of the progress bar
                    return;
                }
                //if the marque is completely in the smaller progress bar then both colors are the same
                if (gradientPosition + (width / 2) < smallerProgress)
                {
                    EndColor = StartColor;
                }
                //else it is not all inside the smaller progress bar
                else
                {
                    //if the marque is completely in the bigger progress bar then both colors are the same
                    if (gradientPosition - (width / 2) > smallerProgress)
                    {
                        StartColor = EndColor;
                    }
                }


                //position is 50% of the width of the Marque since the first box is half the width
                firstPosition = gradientPosition - width * 0.5f;

                #region draw the first half of the marque
                //compute the start height and width of the first half of the marque
                firstRect.X = firstPosition;
                firstRect.Y = 0;
                firstRect.Height = Height;
                firstRect.Width = (width / 2);

                //compute the start height and width of the gradient for the first half of the marque
                firstColorRect.X = firstPosition;
                firstColorRect.Y = 0;
                firstColorRect.Height = Height;
                firstColorRect.Width = (width / 2);

                //adjust percentage to the pixel width from the percentage width
                firstRect.Width = firstRect.Width * FullWidth;
                //compute the pixel X start location of the first half of the marque
                firstRect.X = firstRect.X * FullWidth;
                //compute the pixel X start location of the first half of the marque
                firstColorRect.X = firstColorRect.X * FullWidth;
                //adjust percentage to the pixel width from the percentage width
                firstColorRect.Width = firstColorRect.Width * FullWidth;
                //create the gradient for the first half of the marque and fill the rectangle for the first half of the marque with the gradient
                StartGradient = new LinearGradientBrush(firstColorRect, StartColor, Color.White, 0, false);
                //set the blend of the first half of the marque
                StartGradient.Blend = FadeOutblend;
                //draw the first half of the marque
                g.FillRectangle(StartGradient, firstRect);
                #endregion
                #region draw the second half of the marque
                //get second start as a X location
                SecondPosition = firstRect.X + firstRect.Width;
                //get the start of the second half of the marque as a percentage location
                SecondRect.X = SecondPosition;
                SecondRect.Y = 0;
                proposedWidth = firstColorRect.Width;
                //if the width of the second half of the marque would take it beyond the biggest progres bar
                if (proposedWidth + SecondRect.X > cutoff)
                {
                    //set the width of the progres bar to end at the end of the biggest progress bar
                    proposedWidth = cutoff - SecondRect.X;
                }
                //set the start height and width of the second half of the marque
                SecondRect.Width = proposedWidth;
                SecondRect.Height = Height;
                //have the second half of the marque start at the end of the first half of the marque
                SecondColorRect.X = SecondPosition;
                SecondColorRect.Y = 0;
                //have the color gradient still be the full width of the second half of the marque
                SecondColorRect.Width = (width / 2) * FullWidth;
                SecondColorRect.Height = Height;
                //create the gradient for the second half of the marque and fill the rectangle for the second half of the marque with the gradient
                EndGradient = new LinearGradientBrush(SecondColorRect, Color.White, EndColor, 0, false);
                //set the blend of the second half of the marque
                EndGradient.Blend = FadeInblend;
                //draw the second half of the marque
                g.FillRectangle(EndGradient, SecondRect);
                #endregion
                //dispose the gradients to free up resources
                StartGradient.Dispose();
                EndGradient.Dispose();
            }
            catch
            { }
        }

    }
}
