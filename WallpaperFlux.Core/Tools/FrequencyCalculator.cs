using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using LanceTools;
using WallpaperFlux.Core.Controllers;
using WallpaperFlux.Core.Util;

namespace WallpaperFlux.Core.Tools
{
    public class FrequencyCalculator
    {
        private Dictionary<ImageType, double> RelativeFrequency = new Dictionary<ImageType, double>()
        {
            {ImageType.Static, 1},
            {ImageType.GIF, 1},
            {ImageType.Video, 1},
        };

        private Dictionary<ImageType, double> ExactFrequency = new Dictionary<ImageType, double>()
        {
            {ImageType.Static, 0},
            {ImageType.GIF, 0},
            {ImageType.Video, 0},
        };

        private bool staticPreviouslyExisted = false;
        private bool gifPreviouslyExisted = false;
        private bool videoPreviouslyExisted = false;

        private Dictionary<ImageType, bool> intentionalAbsoluteChange = new Dictionary<ImageType, bool>()
        {
            {ImageType.Static, false},
            {ImageType.GIF, false},
            {ImageType.Video, false}
        };

        //? Note: Don't accidentally invalidate frequencies that were intentionally set to 0 when updating from a previously empty type
        public void VerifyImageTypeExistence(string imageToVerify = "")
        {
            bool staticExists = DataUtil.Theme.RankController.IsAnyImagesOfTypeRanked(ImageType.Static);
            bool gifExists = DataUtil.Theme.RankController.IsAnyImagesOfTypeRanked(ImageType.GIF);
            bool videoExists = DataUtil.Theme.RankController.IsAnyImagesOfTypeRanked(ImageType.Video);

            if (imageToVerify != "")
            {
                if (DataUtil.Theme.Images.ContainsImage(imageToVerify))
                {
                    //? This appears to be the rank of the image before the change
                    if (DataUtil.Theme.Images.GetImage(imageToVerify).Rank > 1) //! Setting this to be true on a rank of 1 will break the process, do not use > 0 or >= 1
                    {
                        // This indicates that there is in fact an image of the given image type but it is either un-ranked or changing its rank
                        if ((!staticExists && WallpaperUtil.IsStatic(imageToVerify)) || 
                            (!gifExists && WallpaperUtil.IsGif(imageToVerify)) || 
                             (!videoExists && WallpaperUtil.IsSupportedVideoType(imageToVerify)))
                        {
                            // no need to verify, just updating the singular image of an image type
                            //! This does not fix the scenario where an image of rank 1 is increased to rank 2, but this scenario is not as problematic as impacting all ranks so
                            //! we will ignore it for now
                            Debug.WriteLine("No need to verify, just updating the singular image of an image type");
                            return;
                        }
                    }
                }
            }

            bool canModifyStatic = staticExists != staticPreviouslyExisted;
            bool canModifyGif = gifExists != gifPreviouslyExisted;
            bool canModifyVideo = videoExists != videoPreviouslyExisted;

            /*x
            bool allowModifyingStatic = false;
            bool allowModifyingGif = false;
            bool allowModifyingVideo = false;

            if (imageToVerify != "")
            {
                if (File.Exists(imageToVerify))
                {
                    allowModifyingStatic = WallpaperUtil.IsStatic(imageToVerify);
                    allowModifyingGif = WallpaperUtil.IsGif(imageToVerify);
                    allowModifyingVideo = WallpaperUtil.IsSupportedVideoType(imageToVerify);
                }
            }
            */


            //xDebug.WriteLine(intentionalAbsoluteChange[ImageType.Static] + " | " + !intentionalAbsoluteChange[ImageType.GIF] + " | " + !intentionalAbsoluteChange[ImageType.Video]);
            //xDebug.WriteLine(canModifyStatic + " | " + canModifyGIF + " | " + canModifyVideo);
            //xDebug.WriteLine(allowModifyingStatic + " | " + allowModifyingGif + " | " + allowModifyingVideo);

            if (!staticExists) intentionalAbsoluteChange[ImageType.Static] = false;
            if (!gifExists) intentionalAbsoluteChange[ImageType.GIF] = false;
            if (!videoExists) intentionalAbsoluteChange[ImageType.Video] = false;

            Debug.WriteLine("Verifying Image Type Existence, this may recalculate some exact frequencies in need of updating");
            // records the previous state for the next verification iteration
            staticPreviouslyExisted = staticExists;
            gifPreviouslyExisted = gifExists;
            videoPreviouslyExisted = videoExists;

            //? A previously empty frequency should be updated to match it's expected value, if it wasn't made empty intentionally
            RecalculateExactFrequency(
                !intentionalAbsoluteChange[ImageType.Static] || canModifyStatic, 
                !intentionalAbsoluteChange[ImageType.GIF] || canModifyGif, 
                !intentionalAbsoluteChange[ImageType.Video] || canModifyVideo);

            ExactFrequency[ImageType.Static] = staticExists ? ExactFrequency[ImageType.Static] : 0;
            ExactFrequency[ImageType.GIF] = gifExists ? ExactFrequency[ImageType.GIF] : 0;
            ExactFrequency[ImageType.Video] = videoExists ? ExactFrequency[ImageType.Video] : 0;

            RelativeFrequency[ImageType.Static] = staticExists ? RelativeFrequency[ImageType.Static] : 1;
            RelativeFrequency[ImageType.GIF] = gifExists ? RelativeFrequency[ImageType.GIF] : 1;
            RelativeFrequency[ImageType.Video] = videoExists ? RelativeFrequency[ImageType.Video] : 1;

            // TODO Rewrite the exact frequency portion of the original calculation instead of re-introducing BalanceExactFrequencies()
            //BalanceExactFrequencies();
            DataUtil.Theme.Settings.ThemeSettings.FrequencyModel.UpdateModelFrequency(); // updates the UI to the potentially adjusted frequency
        }

        public double GetRelativeFrequency(ImageType imageType) => RelativeFrequency[imageType];

        public double GetExactFrequency(ImageType imageType) => ExactFrequency[imageType];

        public void UpdateFrequency(ImageType imageType, FrequencyType frequencyType, double value)
        {
            if (!DataUtil.Theme.RankController.IsAnyImagesOfTypeRanked(imageType))
            {
                string imageTypeString = "";

                switch (imageType)
                {
                    case ImageType.Static:
                        imageTypeString = "[Images]";
                        break;

                    case ImageType.GIF:
                        imageTypeString = "[GIFs]";
                        break;

                    case ImageType.Video:
                        imageTypeString = "[Videos]";
                        break;
                }

                MessageBoxUtil.ShowError("Cannot set the frequency of an empty type. Rank a few " + imageTypeString + " first then try again");
                return;
            }

            // Update an image type's frequency based on the given frequency type

            if (frequencyType == FrequencyType.Relative) // set the relative chance & recalculate exact chances to represent said change
            {
                Debug.WriteLine("Changing a Relative frequency");

                // Relative Frequency Clamp
                double input = Math.Max(0, value);

                if (input == 0)
                {
                    int zeroCount = 0;
                    if (RelativeFrequency[ImageType.Static] == 0) zeroCount++;
                    if (RelativeFrequency[ImageType.GIF] == 0) zeroCount++;
                    if (RelativeFrequency[ImageType.Video] == 0) zeroCount++;

                    if (zeroCount >= 2) // attempted to make all frequencies 0%, cancel this change
                    {
                        MessageBoxUtil.ShowError("Change aborted, cannot have 0% probability across all entries");
                        return;
                    }
                }

                // >>> Frequency Change <<<
                Debug.WriteLine("Setting Relative Frequency of " + imageType + " to: " + input);
                RelativeFrequency[imageType] = input;//! [You did the stuff on the right in the parent method] input / 100; // the actual value is a percentage

                RecalculateExactFrequency(true, true, true);
            }
            else if (frequencyType == FrequencyType.Exact) // set a new exact chance, recalculating the remaining exact chances & also the relative chances to represent this change
            {
                Debug.WriteLine("Changing an Exact frequency");

                // Exact Frequency Clamp
                double input = MathE.Clamp(value, 0, 1);

                // >>> Frequency Change <<<
                Debug.WriteLine("Setting Exact Frequency of " + imageType + " to: " + input);
                ExactFrequency[imageType] = input; //! [You did the stuff commented on the right in the parent method] input / 100; // the actual value is a percentage

                if (input >= 1 || input <= 0)
                {
                    intentionalAbsoluteChange[imageType] = true;
                }

                if (input < 1 && input > 0) // includes 0 but not 1, accepts all input in-between
                {
                    CalculateExactFrequency(imageType);
                    RecalculateRelativeFrequency(imageType, false);
                }
                else if (input >= 1) // exact chance of 1, set everything else to 0
                {
                    RecalculateRelativeFrequency(imageType, true);
                }
                else if (input <= 0) // exact chance of 0, balance the other frequencies and then update the relative frequencies (under absolute percentage)
                {
                    CalculateExactFrequency(imageType);
                    RecalculateRelativeFrequency(imageType, true);
                }
            }

            VerifyImageTypeExistence();

            //? Ideally this shouldn't happen but imposing this sweeping change prevents the values from becoming stuck
            //? For some reason this isn't imm
            if (double.IsNaN(RelativeFrequency[ImageType.Static])) RelativeFrequency[ImageType.Static] = 0;
            if (double.IsNaN(RelativeFrequency[ImageType.GIF])) RelativeFrequency[ImageType.GIF] = 0;
            if (double.IsNaN(RelativeFrequency[ImageType.Video])) RelativeFrequency[ImageType.Video] = 0;
            if (double.IsNaN(ExactFrequency[ImageType.Static])) ExactFrequency[ImageType.Static] = 0;
            if (double.IsNaN(ExactFrequency[ImageType.GIF])) ExactFrequency[ImageType.GIF] = 0;
            if (double.IsNaN(ExactFrequency[ImageType.Video])) ExactFrequency[ImageType.Video] = 0;
        }

        // Recalculate Relative Frequency to account for changes to Exact Frequency
        // (The recalculation for this can vary wildly depending on how its programmed, in this case, the changed exact value will be
        // displays as 100% while the remaining values will display how likely they are to appear relative to that 100% value)
        private void RecalculateRelativeFrequency(ImageType changedImageType, bool absoluteExactFrequency)
        {
            //? The changed image type *should* be changed before this method
            Debug.WriteLine("Recalculating Relative Frequency");

            RelativeFrequency[changedImageType] = 1;

            if (!absoluteExactFrequency) // exact values have chances anywhere between 0 & 100 exclusive
            {
                Debug.WriteLine("Setting an Exact Frequency between 0-100");

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
            else // some changed exact value has a chance of 1.0 or 0.0, this needs its own separate calculation
            {
                if (ExactFrequency[changedImageType] >= 1) // exact chance input of 1.0
                {
                    if (changedImageType != ImageType.Static)
                    {
                        RelativeFrequency[ImageType.Static] = 1;
                        ExactFrequency[ImageType.Static] = 0;
                    }

                    if (changedImageType != ImageType.GIF)
                    {
                        RelativeFrequency[ImageType.GIF] = 1;
                        ExactFrequency[ImageType.GIF] = 0;
                    }

                    if (changedImageType != ImageType.Video)
                    {
                        RelativeFrequency[ImageType.Video] = 1;
                        ExactFrequency[ImageType.Video] = 0;
                    }
                }
                else if (ExactFrequency[changedImageType] <= 0) // exact chance input of 0.0
                {
                    ImageType opposingTypeOne = ImageType.None;
                    ImageType opposingTypeTwo = ImageType.None;

                    switch (changedImageType)
                    {
                        case ImageType.Static:
                            opposingTypeOne = ImageType.GIF;
                            opposingTypeTwo = ImageType.Video;
                            break;

                        case ImageType.GIF:
                            opposingTypeOne = ImageType.Static;
                            opposingTypeTwo = ImageType.Video;
                            break;

                        case ImageType.Video:
                            opposingTypeOne = ImageType.Static;
                            opposingTypeTwo = ImageType.GIF;
                            break;
                    }

                    // Balance the opposing relative frequencies
                    if (Math.Abs(RelativeFrequency[opposingTypeOne] - RelativeFrequency[opposingTypeTwo]) > 0.000000001) //? This checks is the two opposing frequencies are 'equal' using floating point tolerance
                    {
                        if (RelativeFrequency[opposingTypeOne] > RelativeFrequency[opposingTypeTwo])
                        {
                            RelativeFrequency[opposingTypeTwo] /= RelativeFrequency[opposingTypeOne];
                            RelativeFrequency[opposingTypeOne] = 1;
                        }
                        else
                        {
                            RelativeFrequency[opposingTypeOne] /= RelativeFrequency[opposingTypeTwo];
                            RelativeFrequency[opposingTypeTwo] = 1;
                        }
                    }
                    else
                    {
                        RelativeFrequency[opposingTypeOne] = RelativeFrequency[opposingTypeTwo] = 1;
                    }
                }
            }
        }

        // Recalculate Exact Frequency to account for changes to Relative Frequency
        // (This also displays to the user what the exact chance even is based on the Relative Frequency)
        private void RecalculateExactFrequency(bool canModifyStatic, bool canModifyGIF, bool canModifyVideo)
        {
            Debug.WriteLine("Recalculating Exact Frequency");
            if (canModifyStatic == canModifyGIF == canModifyVideo == false)
            {
                Debug.WriteLine("Exact Frequency calculation aborted, all canModify booleans were false");
                return;
            }

            //? The boolean prevents an unused image type from being counted in frequency calculations
            //! Do not include canModify in these calculations, it serves a different purpose!
            double staticRelativeFrequency = DataUtil.Theme.RankController.IsAnyImagesOfTypeRanked(ImageType.Static) ? RelativeFrequency[ImageType.Static] : 0;
            double gifRelativeFrequency = DataUtil.Theme.RankController.IsAnyImagesOfTypeRanked(ImageType.GIF) ? RelativeFrequency[ImageType.GIF] : 0;
            double videoRelativeFrequency = DataUtil.Theme.RankController.IsAnyImagesOfTypeRanked(ImageType.Video) ? RelativeFrequency[ImageType.Video] : 0;

            double chanceTotal = staticRelativeFrequency +
                                 gifRelativeFrequency +
                                 videoRelativeFrequency;

            double staticRelativeChance = RelativeFrequency[ImageType.Static] / chanceTotal;
            double gifRelativeChance = RelativeFrequency[ImageType.GIF] / chanceTotal;
            double videoRelativeChance = RelativeFrequency[ImageType.Video] / chanceTotal;

            Debug.WriteLine("Relative chanceTotal: " + chanceTotal);
            Debug.WriteLine("R.Static / chanceTotal: " + staticRelativeChance);
            Debug.WriteLine("R.GIF / chanceTotal: " + gifRelativeChance);
            Debug.WriteLine("R.Video / chanceTotal: " + videoRelativeChance);

            // If the frequency isn't weighted, no modifications need to be made
            if (!DataUtil.Theme.Settings.ThemeSettings.WeightedFrequency)
            {
                ExactFrequency[ImageType.Static] = canModifyStatic ? staticRelativeChance : ExactFrequency[ImageType.Static];
                ExactFrequency[ImageType.GIF] = canModifyGIF ? gifRelativeChance : ExactFrequency[ImageType.GIF];
                ExactFrequency[ImageType.Video] = canModifyVideo ? videoRelativeChance : ExactFrequency[ImageType.Video];
            }
            else // Weighted Frequency, frequency will be adjusted by the number of images in each image type
            {
                // Gets the average of both the weighted frequency and the original exact frequency, allowing relative frequency to have an impact on the weight
                double staticWeightedChance = DataUtil.Theme.RankController.GetImageOfTypeWeight(ImageType.Static);
                double gifWeightedChance = DataUtil.Theme.RankController.GetImageOfTypeWeight(ImageType.GIF);
                double videoWeightedChance = DataUtil.Theme.RankController.GetImageOfTypeWeight(ImageType.Video);

                // TODO This is redundant, the verification process does this for you now
                MessageBoxUtil.ShowError("There is a bit of redundancy in this weighted formula, update it to match the new changes");
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

        // When an Exact frequency is changed, the remaining Exact frequencies need to also be updated since the total for Exact frequencies can only add to 1.0
        // A variation of this is not needed for Relative Frequency
        private void CalculateExactFrequency(ImageType changedImageType)
        {
            Debug.WriteLine("Calculating Exact Frequency");

            // Get the improper chance total, which due to the new changes may be greater than or less than 1. The end result should be a chance total of 1
            double chanceTotal = ExactFrequency[ImageType.Static] +
                                 ExactFrequency[ImageType.GIF] +
                                 ExactFrequency[ImageType.Video];

            Debug.WriteLine("chanceTotal: " + chanceTotal + "\n" +
                            "From: " + ExactFrequency[ImageType.Static] + " | " + ExactFrequency[ImageType.GIF] + " | " + ExactFrequency[ImageType.Video]);

            // Leave the changed frequency and readjust the remaining two according to the value difference and their own relative values
            double valueDiff = chanceTotal - 1;
            Debug.WriteLine("chanceTotal valueDiff: " + valueDiff);

            double relativeChanceTotal = 0;

            // Gathers the relative chance of the unchanged Exact Frequencies so that they can be redistributed evenly
            if (changedImageType != ImageType.Static) relativeChanceTotal += ExactFrequency[ImageType.Static];
            if (changedImageType != ImageType.GIF) relativeChanceTotal += ExactFrequency[ImageType.GIF];
            if (changedImageType != ImageType.Video) relativeChanceTotal += ExactFrequency[ImageType.Video];
            Debug.WriteLine("relativeChanceTotal: " + relativeChanceTotal);

            // The other two frequencies do not need to be changed if they are both at 0%
            if (relativeChanceTotal == 0) return;

            double adjustedRelativeChanceTotal = relativeChanceTotal - valueDiff;
            Debug.WriteLine("adjustedRelativeChanceTotal: " + adjustedRelativeChanceTotal);

            double staticChance = 1;
            double gifChance = 1;
            double videoChance = 1;

            // calculate a multiplier for the image types that are *not* being changed
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

        // When an exact frequency is changed, all will need to be rebalanced to account for said change
        //? Do note that the relative adjustment to the Exact Frequency on calculation still exist in the original method
        //? This just undoes any situation where the Exact Frequencies do not add up to 100%, which will occur if one frequency is missing
        private void BalanceExactFrequencies()
        {
            double chanceTotal = 0;
            foreach (ImageType imageType in ExactFrequency.Keys)
            {
                chanceTotal += ExactFrequency[imageType];
            }

            if (ExactFrequency[ImageType.Static] != 0) ExactFrequency[ImageType.Static] = ExactFrequency[ImageType.Static] / chanceTotal;

            if (ExactFrequency[ImageType.GIF] != 0) ExactFrequency[ImageType.GIF] = ExactFrequency[ImageType.GIF] / chanceTotal;

            if (ExactFrequency[ImageType.Video] != 0) ExactFrequency[ImageType.Video] = ExactFrequency[ImageType.Video] / chanceTotal;
        }
    }
}
