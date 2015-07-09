using System.Collections.Generic;
using Messages.CSharp.Pieces;
using NUnit.Framework;

namespace BattleShipTests
{
    [TestFixture]
    public class PointTests
    {
        [Test]
        public void TestPointOrderingYaxis()
        {
            var point1 = new Point(0, 0);
            var point2 = new Point(0, 1);
            var list = new List<Point> {point2, point1};
            list.Sort();
            Assert.AreEqual(point1, list[0]);
            Assert.AreEqual(point2, list[1]);
        }

        [Test]
        public void TestPointOrderingXaxis()
        {
            var point1 = new Point(0, 1);
            var point2 = new Point(1, 1);
            var list = new List<Point> { point1, point2 };
            list.Sort();
            Assert.AreEqual(point1, list[0]);
            Assert.AreEqual(point2, list[1]);
        }

        [Test]
        public void TestPointsAreEqual()
        {
            var point1 = new Point(4, 6);
            var point2 = new Point(4, 6);
            Assert.AreEqual(point1, point2);
        }

    }
}
