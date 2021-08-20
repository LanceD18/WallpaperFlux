using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WallpaperFlux.Core.Collections;
using WallpaperFlux.Core.Models;
using Xunit;

namespace WallpaperFlux.Tests
{
    public class ImageListTests
    {
        [Fact]
        public void GetTest()
        {
            ImageCollection list = new ImageCollection();

            string dummyPath = "dummyPath";

            ImageModel image = list.AddImage(dummyPath);
            list.AddImage("dum");

            Assert.Equal(image, list.GetImage(dummyPath));
        }

        [Fact]
        public void GetRangeTest()
        {
            ImageCollection list = new ImageCollection();

            string[] dummyPaths =
            {
                "dum1",
                "dum2",
                "dum3"
            };

            ImageModel[] images = list.AddImageRange(dummyPaths);
            list.AddImage("dummyPath");

            Assert.Equal(images, list.GetImageRange(dummyPaths));
        }

        [Fact]
        public void AddTest()
        {
            ImageCollection list = new ImageCollection();

            // dummy adds
            string dummyPath = "dummyPath1";
            ImageModel dummyModel = new ImageModel("dummyPath2");
            string[] dummyPaths =
            {
                "dum1",
                "dum2",
                "dum3"
            };
            ImageModel[] dummyModels = new[]
            {
                new ImageModel("model1"),
                new ImageModel("model2"),
                new ImageModel("model3")
            };

            list.AddImage(dummyPath);
            list.AddImage(dummyModel);
            list.AddImageRange(dummyPaths);
            list.AddImageRange(dummyModels);

            // expected
            string[] expectedImages =
            {
                "dummyPath1",
                "dummyPath2",
                "dum1",
                "dum2",
                "dum3",
                "model1",
                "model2",
                "model3"
            };

            Assert.Equal(expectedImages,  list.GetAllImagePaths());
        }

        [Fact]
        public void RemoveStringTest()
        {
            ImageCollection list = new ImageCollection();

            string dummyPath = "dummyPath";
            ImageModel image = list.AddImage(dummyPath);
            list.RemoveImage("dummyPath");

            Assert.False(list.ContainsImage(image.Path));
        }

        [Fact]
        public void RemoveStringRangeTest()
        {
            ImageCollection list = new ImageCollection();

            string[] dummyPaths =
            {
                "dum1",
                "dum2",
                "dum3"
            };

            list.AddImageRange(dummyPaths);
            list.RemoveImage("dum2");

            Assert.False(list.ContainsImage("dum2"));
        }
    }
}
