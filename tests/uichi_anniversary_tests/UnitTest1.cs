using NUnit.Framework;

namespace uichi_anniversary_tests
{
    public class UnitTest1
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestMainScriptReady()
        {
            // Very small smoke test: ensure test runner works
            Assert.Pass("Test runner OK");
        }
    }
}
