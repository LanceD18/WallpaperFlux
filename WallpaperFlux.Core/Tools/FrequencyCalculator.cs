using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using HandyControl.Controls;
using LanceTools;
using WallpaperFlux.Core.Util;

namespace WallpaperFlux.Core.Tools
{
    public class FrequencyCalculator
    {
        private Dictionary<ImageType, double> RelativeFrequency = new Dictionary<ImageType, double>()
        {
            {ImageType.Static, 0.9},
            {ImageType.GIF, 1},
            {ImageType.Video, 1},
        };

        private Dictionary<ImageType, double> ExactFrequency = new Dictionary<ImageType, double>()
        {
            {ImageType.Static, 0.33},
            {ImageType.GIF, 0.33},
            {ImageType.Video, 0.33},
        };

        public double GetRelativeFrequency(ImageType imageType) => RelativeFrequency[imageType];

        public double GetExactFrequency(ImageType imageType) => ExactFrequency[imageType];

        //TODO Don't ref ThemeModel here the second time around
        public void UpdateFrequency(object sender, ImageType imageType, FrequencyType frequencyType)
        {
            /* TODO
            double input = 0;
            TextBox sourceTextBox = sender as TextBox;

            // Process the Input
            try
            {
                string inputText = sourceTextBox.Text;
                if (inputText.Contains('%')) inputText = inputText.Substring(0, inputText.IndexOf('%')); // removes % from input if it was left

                if (frequencyType == FrequencyType.Relative)
                {
                    input = Math.Max(0, double.Parse(inputText));
                }
                else if (frequencyType == FrequencyType.Exact)
                {
                    input = MathE.Clamp(double.Parse(inputText), 0, 100);
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
                // incorrect value entered, end update and reset text (reset externally)
                return;
            }

            // Update a Frequency
            if (frequencyType == FrequencyType.Relative) // set the relative chance & recalculate exact chances to represent said change
            {
                Debug.WriteLine("Relative");

                if (input == 0)
                {
                    int zeroCount = 0;
                    if (RelativeFrequency[ImageType.Static] == 0) zeroCount++;
                    if (RelativeFrequency[ImageType.GIF] == 0) zeroCount++;
                    if (RelativeFrequency[ImageType.Video] == 0) zeroCount++;

                    if (zeroCount >= 2) // attempted to make all frequencies 0%, cancel this change
                    {
                        Debug.WriteLine("Cannot have 0% probability across all entries. Change cancelled");
                        return;
                    }
                }

                RelativeFrequency[imageType] = input / 100; // the actual value is a percentage

                RecalculateExactFrequency();
            }
            else if (frequencyType == FrequencyType.Exact) // set a new exact chance, recalculating the remaining exact chances & also the relative chances to represent this change
            {
                Debug.WriteLine("Exact");
                ExactFrequency[imageType] = input / 100; // the actual value is a percentage

                if (input < 100 && input > 0)
                {
                    CalculateExactFrequency(imageType);
                    RecalculateRelativeFrequency(imageType, false);
                }
                else if (input >= 100) // exact chance of 1, set everything else to 0
                {
                    if (imageType != ImageType.Static) ExactFrequency[ImageType.Static] = 0;
                    if (imageType != ImageType.GIF) ExactFrequency[ImageType.GIF] = 0;
                    if (imageType != ImageType.Video) ExactFrequency[ImageType.Video] = 0;
                    RecalculateRelativeFrequency(imageType, true);
                }
                else if (input <= 0) // exact chance of 0, set everything else to 0.5
                {
                    if (imageType != ImageType.Static) ExactFrequency[ImageType.Static] = 0.5;
                    if (imageType != ImageType.GIF) ExactFrequency[ImageType.GIF] = 0.5;
                    if (imageType != ImageType.Video) ExactFrequency[ImageType.Video] = 0.5;
                    RecalculateRelativeFrequency(imageType, true);
                }

            }
            */
        }

        // Recalculate Relative Frequency to account for changes to Exact Frequency
        // (The recalculation for this can vary wildly depending on how its programmed, in this case, the changed exact value will be
        // displays as 100% while the remaining values will display how likely they are to appear relative to that 100% value)
        private void RecalculateRelativeFrequency(ImageType changedImageType, bool absolutePercentage)
        {
            RelativeFrequency[changedImageType] = 1;

            if (!absolutePercentage) // exact values have chances anywhere between 0 & 100 exclusive
            {
                if (changedImageType != ImageType.Static)
                    RelativeFrequency[ImageType.Static] =
                        ExactFrequency[ImageType.Static] / ExactFrequency[changedImageType];

                if (changedImageType != ImageType.GIF)
                    RelativeFrequency[ImageType.GIF] =
                        ExactFrequency[ImageType.GIF] / ExactFrequency[changedImageType];

                if (changedImageType != ImageType.Video)
                    RelativeFrequency[ImageType.Video] =
                        ExactFrequency[ImageType.Video] / ExactFrequency[changedImageType];
            }
            else // some exact value has a chance of 0 or 100, this needs its own separate calculation
            {
                RelativeFrequency[ImageType.Static] = 1 * ExactFrequency[ImageType.Static];
                RelativeFrequency[ImageType.GIF] = 1 * ExactFrequency[ImageType.GIF];
                RelativeFrequency[ImageType.Video] = 1 * ExactFrequency[ImageType.Video];
            }
        }

        // Recalculate Exact Frequency to account for changes to Relative Frequency
        // (This also displays to the user what the exact chance even is)
        private void RecalculateExactFrequency()
        {
            double chanceTotal = RelativeFrequency[ImageType.Static] +
                                 RelativeFrequency[ImageType.GIF] +
                                 RelativeFrequency[ImageType.Video];


            double staticRelativeChance = RelativeFrequency[ImageType.Static] / chanceTotal;
            double gifRelativeChance = RelativeFrequency[ImageType.GIF] / chanceTotal;
            double videoRelativeChance = RelativeFrequency[ImageType.Video] / chanceTotal;

            Debug.WriteLine("chanceTotal: " + chanceTotal);
            Debug.WriteLine("Static: " + staticRelativeChance);
            Debug.WriteLine("GIF: " + gifRelativeChance);
            Debug.WriteLine("Video: " + videoRelativeChance);

            if (!DataUtil.Theme.Settings.ThemeSettings.WeightedFrequency)
            {
                ExactFrequency[ImageType.Static] = staticRelativeChance;
                ExactFrequency[ImageType.GIF] = gifRelativeChance;
                ExactFrequency[ImageType.Video] = videoRelativeChance;
            }
            else
            {
                // Gets the average of both the weighted frequency and the original exact frequency, allowing relative frequency to have an impact on the weight
                double staticWeightedChance = DataUtil.Theme.RankController.GetImageOfTypeWeight(ImageType.Static);
                double gifWeightedChance = DataUtil.Theme.RankController.GetImageOfTypeWeight(ImageType.GIF);
                double videoWeightedChance = DataUtil.Theme.RankController.GetImageOfTypeWeight(ImageType.Video);

                if (staticWeightedChance == 1) // prevents a division by 0 error below
                {
                    ExactFrequency[ImageType.Static] = 1;
                    ExactFrequency[ImageType.GIF] = ExactFrequency[ImageType.Video] = 0;
                    return;
                }
                if (gifWeightedChance == 1) // prevents a division by 0 error below
                {
                    ExactFrequency[ImageType.GIF] = 1;
                    ExactFrequency[ImageType.Static] = ExactFrequency[ImageType.Video] = 0;
                    return;
                }
                if (videoWeightedChance == 1) // prevents a division by 0 error below
                {
                    ExactFrequency[ImageType.Video] = 1;
                    ExactFrequency[ImageType.Static] = ExactFrequency[ImageType.GIF] = 0;
                    return;
                }

                /*x
                ExactFrequency[ImageType.Static] = (staticWeightedChance + staticRelativeChance) / 2;
                ExactFrequency[ImageType.GIF] = (gifWeightedChance + gifRelativeChance) / 2;
                ExactFrequency[ImageType.Video] = (videoWeightedChance + videoRelativeChance) / 2;
                */

                double staticWeightedRelativeChance = staticRelativeChance / (1 - staticWeightedChance);
                double gifWeightedRelativeChance = gifRelativeChance / (1 - gifWeightedChance);
                double videoWeightedRelativeChance = videoRelativeChance / (1 - videoWeightedChance);
                double weightedChanceTotal = staticWeightedRelativeChance + gifWeightedRelativeChance + videoWeightedRelativeChance;

                ExactFrequency[ImageType.Static] = staticWeightedRelativeChance / weightedChanceTotal;
                ExactFrequency[ImageType.GIF] = gifWeightedRelativeChance / weightedChanceTotal;
                ExactFrequency[ImageType.Video] = videoWeightedRelativeChance / weightedChanceTotal;
            }
        }

        private void CalculateExactFrequency(ImageType changedImageType)
        {
            // Readjust Exact Frequency to account for the new changes
            double chanceTotal = ExactFrequency[ImageType.Static] +
                                 ExactFrequency[ImageType.GIF] +
                                 ExactFrequency[ImageType.Video];
            Debug.WriteLine("chanceTotal: " + chanceTotal);

            // Leave the changed frequency and readjust the remaining two according to the value difference and their own relative values
            double valueDiff = chanceTotal - 1;
            Debug.WriteLine("valueDiff: " + valueDiff);

            double relativeChanceTotal = 0;

            if (changedImageType != ImageType.Static) relativeChanceTotal += ExactFrequency[ImageType.Static];
            if (changedImageType != ImageType.GIF) relativeChanceTotal += ExactFrequency[ImageType.GIF];
            if (changedImageType != ImageType.Video) relativeChanceTotal += ExactFrequency[ImageType.Video];
            Debug.WriteLine("relativeChanceTotal: " + relativeChanceTotal);

            double adjustedRelativeChanceTotal = relativeChanceTotal - valueDiff;
            Debug.WriteLine("adjustedRelativeChanceTotal: " + adjustedRelativeChanceTotal);

            double staticChance = 1;
            double gifChance = 1;
            double videoChance = 1;

            // calculate a multiplier for the image types that are *not* in use
            switch (changedImageType)
            {
                case ImageType.Static:
                    gifChance = ExactFrequency[ImageType.GIF] / relativeChanceTotal;
                    videoChance = ExactFrequency[ImageType.Video] / relativeChanceTotal;
                    break;

                case ImageType.GIF:
                    staticChance = ExactFrequency[ImageType.Static] / relativeChanceTotal;
                    videoChance = ExactFrequency[ImageType.Video] / relativeChanceTotal;
                    break;

                case ImageType.Video:
                    staticChance = ExactFrequency[ImageType.Static] / relativeChanceTotal;
                    gifChance = ExactFrequency[ImageType.GIF] / relativeChanceTotal;
                    break;
            }

            // readjust percentages
            if (changedImageType != ImageType.Static) ExactFrequency[ImageType.Static] = staticChance * adjustedRelativeChanceTotal;
            if (changedImageType != ImageType.GIF) ExactFrequency[ImageType.GIF] = gifChance * adjustedRelativeChanceTotal;
            if (changedImageType != ImageType.Video) ExactFrequency[ImageType.Video] = videoChance * adjustedRelativeChanceTotal;
        }
    }
}
