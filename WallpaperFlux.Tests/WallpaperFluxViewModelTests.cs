using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace WallpaperFlux.Tests
{
    public class WallpaperFluxViewModelTests
    {
        [Fact]
        public void DummyFactTest()
        {
            Assert.Equal(1, 1);
        }

        [Theory]
        [InlineData(2, 4)]
        public void DummyTheoryTest(int x, int y)
        {
            Assert.Equal(x + y, x + y);
        }

    }
}
