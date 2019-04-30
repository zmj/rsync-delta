using System;
using Xunit;

namespace Rsync.Delta.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            string hello = new Class1().Hello();
            Assert.Equal("hello", hello);
        }
    }
}
