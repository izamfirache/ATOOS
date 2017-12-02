using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NUnitRunnerProjectDemo
{
    [TestFixture]
    public class Test_RemoveMe
    {
        [TestCase()]
        public virtual void BasicUnitTest1()
        {
            Assert.AreEqual(10, 20);
        }

        [TestCase()]
        public virtual void BasicUnitTest2()
        {
            Assert.AreEqual("str", "str");
        }

        [TestCase()]
        public virtual void BasicUnitTest3()
        {
            Assert.AreEqual(20, 20);
        }
    }
}
