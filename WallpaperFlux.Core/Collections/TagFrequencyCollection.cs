using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using LanceTools.WPF.Adonis.Util;
using WallpaperFlux.Core.Models;
using WallpaperFlux.Core.Models.Tagging;
using WallpaperFlux.Core.Util;
using WallpaperFlux.Core.ViewModels;

namespace WallpaperFlux.Core.Collections
{
    public class TagFrequencyCollection
    {
        private Dictionary<TagModel, double> TagFrequencies = new Dictionary<TagModel, double>();

        public void ModifyFrequency(TagModel tag, double frequency)
        {
            if (frequency == 1)
            {
                RemoveFrequency(tag);
            }
            else
            {
                AddFrequency(tag, frequency);
            }
        }

        private void AddFrequency(TagModel tag, double frequency)
        {
            if (ContainsFrequency(tag))
            {
                TagFrequencies[tag] = frequency;
            }
            else
            {
                TagFrequencies.Add(tag, frequency);
            }
        }

        private void RemoveFrequency(TagModel tag) //? used if a frequency is being moved back to 1, which counts 
        {
            if (ContainsFrequency(tag))
            {
                TagFrequencies.Remove(tag);
            }
        }

        public bool ContainsFrequency(TagModel tag) => TagFrequencies.ContainsKey(tag);

        private (double, double, double) GetWeights()
        {
            // Split tags

            TagModel[] weightedTags = TagFrequencies.Keys.ToArray();
            TagModel[] unweightedTags = TaggingUtil.GetAllTags(false, false, weightedTags);

            // Get unique images found in valid tags

            BaseImageModel[] unweightedImages = TaggingUtil.GetLinkedImagesInTags(unweightedTags, false);

            // Get weight of tags with & without frequencies to get the weight total

            double unweightedSum = ThemeUtil.Theme.RankController.GetWeightOfImages(unweightedImages);

            double weightedSum = 0;
            foreach (TagModel tag in weightedTags)
            {
                BaseImageModel[] weightedImages = tag.GetLinkedImages(false).ToArray();
                weightedSum += ThemeUtil.Theme.RankController.GetWeightOfImages(weightedImages) * TagFrequencies[tag];
            }

            double weightTotal = unweightedSum + weightedSum;

            Debug.WriteLine("Weighted Sum: " + weightedSum);
            Debug.WriteLine("Unweighted Sum: " + unweightedSum);
            Debug.WriteLine("Weight Total: " + weightTotal);
            return (weightedSum, unweightedSum, weightTotal);
        }

        public double GetRelativeFrequency(TagModel tag)
        {
            if (TagFrequencies.ContainsKey(tag))
            {
                return TagFrequencies[tag];
            }

            return 1;
        }

        public double GetExactFrequency(TagModel tagToCheck)
        {
            //x(int, int, int) weights = GetWeights();
            //xint weightTotal = weights.Item3;

            BaseImageModel[] tagToCheckImages = tagToCheck.GetLinkedImages(convertImagesToSets: true).ToArray();
            int tagToCheckSum = ThemeUtil.Theme.RankController.GetWeightOfImages(tagToCheckImages);
            
            double weightTotal;

            if (TagFrequencies.Keys.Count == 0) // no modified frequencies found, all tags have the same weight
            {
                weightTotal = ThemeUtil.RankController.GetWeightOfAllRankedImages();

                return tagToCheckSum / weightTotal;
            }
            else
            {
                (double, double, double) weights = GetWeights();
                weightTotal = weights.Item3;
                
                double weightedTagToCheckSum = GetTagWeight(tagToCheck, tagToCheckSum);

                Debug.WriteLine("Weighted Tag Sum: " + weightedTagToCheckSum);
                Debug.WriteLine("Rate: " + weightedTagToCheckSum / weightTotal);
                return (double)weightedTagToCheckSum / weightTotal;
            }
        }

        public double GetTagWeight(TagModel tagToCheck, int tagToCheckSum = -1)
        {
            if (tagToCheckSum == -1)
            {
                BaseImageModel[] tagToCheckImages = tagToCheck.GetLinkedImages(convertImagesToSets: true, ignoreChildTags: true).ToArray();
                tagToCheckSum = ThemeUtil.Theme.RankController.GetWeightOfImages(tagToCheckImages);
            }

            double relativeMult = 1;
            
            if (TagFrequencies.ContainsKey(tagToCheck))
            {
                relativeMult = TagFrequencies[tagToCheck];
            }

            foreach (TagModel tag in tagToCheck.GetChildTags())
            {
                // TODO don't forget parent tags too
            }

            Debug.WriteLine(tagToCheck.Name + " | Mult: " + relativeMult);

            double weightedTagToCheckSum = tagToCheckSum * relativeMult;

            return weightedTagToCheckSum;
        }

        public double GetExactFrequencyOfWeighted()
        {
            (double, double, double) weights = GetWeights();
            double weightedSum = weights.Item1;
            double weightTotal = weights.Item3;

            return (double)weightedSum / weightTotal;
        }

        public double GetExactFrequencyOfUnweighted()
        {
            (double, double, double) weights = GetWeights();
            double unweightedSum = weights.Item2;
            double weightTotal = weights.Item3;

            return (double)unweightedSum / weightTotal;
        }
    }
}
