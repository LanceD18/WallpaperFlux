using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WallpaperFlux.Core.Models;
using WallpaperFlux.Core.Models.Tagging;
using WallpaperFlux.Core.Util;
using WallpaperFlux.Core.ViewModels;

namespace WallpaperFlux.Core.Collections
{
    public class TagFrequencyCollection
    {
        private Dictionary<TagModel, float> tagFrequencies = new Dictionary<TagModel, float>();

        public void ModifyFrequency(TagModel tag, float frequency)
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

        private void AddFrequency(TagModel tag, float frequency)
        {
            if (ContainsFrequency(tag))
            {
                tagFrequencies[tag] = frequency;
            }
            else
            {
                tagFrequencies.Add(tag, frequency);
            }
        }

        private void RemoveFrequency(TagModel tag) //? used if a frequency is being moved back to 1, which counts 
        {
            if (ContainsFrequency(tag))
            {
                tagFrequencies.Remove(tag);
            }
        }

        public bool ContainsFrequency(TagModel tag) => tagFrequencies.ContainsKey(tag);

        private (int, int, int) GetWeights()
        {
            // Split tags

            TagModel[] weightedTags = tagFrequencies.Keys.ToArray();
            TagModel[] unweightedTags = TaggingUtil.GetAllTags(false, false, weightedTags);

            // Get unique images found in valid tags

            BaseImageModel[] weightedImages = TaggingUtil.GetLinkedImagesInTags(weightedTags);
            BaseImageModel[] unweightedImages = TaggingUtil.GetLinkedImagesInTags(unweightedTags);

            // Get weight of tags with & without frequencies to get the weight total

            int weightedSum = ThemeUtil.Theme.RankController.GetRankSumOfImages(weightedImages);
            int unweightedSum = ThemeUtil.Theme.RankController.GetRankSumOfImages(unweightedImages);
            int weightTotal = unweightedSum + weightedSum;

            return (weightedSum, unweightedSum, weightTotal);
        }

        public double GetExactFrequency(TagModel tagToCheck)
        {
            (int, int, int) weights = GetWeights();
            int weightTotal = weights.Item3;

            BaseImageModel[] tagToCheckImages = tagToCheck.GetLinkedImages(convertImagesToSets: true).ToArray();
            int tagToCheckSum = ThemeUtil.Theme.RankController.GetRankSumOfImages(tagToCheckImages);

            return (double)tagToCheckSum / weightTotal;
        }

        public double GetExactFrequencyOfWeighted()
        {
            (int, int, int) weights = GetWeights();
            int weightedSum = weights.Item1;
            int weightTotal = weights.Item3;

            return (double)weightedSum / weightTotal;
        }

        public double GetExactFrequencyOfUnweighted()
        {
            (int, int, int) weights = GetWeights();
            int unweightedSum = weights.Item2;
            int weightTotal = weights.Item3;

            return (double)unweightedSum / weightTotal;
        }
    }
}
