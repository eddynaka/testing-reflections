using Xunit;

namespace VsClassLibrary.Tests
{
    [VsTestSettings(UIThread = true)]
    public class DteUtilitiesTests
    {
        [VsFact(Version = "2012")]
        void TestMethod11()
        {
            Assert.Equal("11.0", DteUtilities.GetVsVersion());
        }

        [VsFact(Version = "2013")]
        void TestMethod12()
        {
            Assert.Equal("12.0", DteUtilities.GetVsVersion());
        }

        [VsFact(Version = "2015")]
        void TestMethod14()
        {
            Assert.Equal("14.0", DteUtilities.GetVsVersion());
        }

        [VsFact(Version = "2017")]
        void TestMethod15()
        {
            Assert.Equal("15.0", DteUtilities.GetVsVersion());
        }

        [VsFact(Version = "2019")]
        void TestMethod16()
        {
            Assert.Equal("16.0", DteUtilities.GetVsVersion());
        }
    }
}
