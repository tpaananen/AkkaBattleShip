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
            var point1 = new Point('A', 1, false, false);
            var point2 = new Point('A', 2, false, false);
            var list = new List<Point> {point2, point1};
            list.Sort();
            Assert.AreEqual(point1, list[0]);
            Assert.AreEqual(point2, list[1]);
        }

        [Test]
        public void TestPointOrderingXaxis()
        {
            var point1 = new Point('A', 1, false, false);
            var point2 = new Point('B', 1, false, false);
            var point3 = new Point('C', 1, false, false);
            var list = new List<Point> { point3, point1, point2 };
            list.Sort();
            Assert.AreEqual(point1, list[0]);
            Assert.AreEqual(point2, list[1]);
            Assert.AreEqual(point3, list[2]);
        }

        [Test]
        public void TestPointOrderingXAndYaxis()
        {
            var point1 = new Point('A', 1, false, false);
            var point2 = new Point('B', 1, false, false);
            var point3 = new Point('C', 1, false, false);

            var point4 = new Point('B', 2, false, false);
            var point5 = new Point('F', 2, false, false);

            var list = new List<Point> { point3, point1, point4, point5, point2 };
            list.Sort();
            Assert.AreEqual(point1, list[0]);
            Assert.AreEqual(point2, list[1]);
            Assert.AreEqual(point3, list[2]);
            Assert.AreEqual(point4, list[3]);
            Assert.AreEqual(point5, list[4]);
        }

        [Test]
        public void TestPointsAreEqual()
        {
            var point1 = new Point('E', 6, false, false);
            var point2 = new Point('E', 6, true, false);
            Assert.AreEqual(point1, point2);
        }

        [Test]
        public void TestShipLength()
        {
            var point1 = new Point('E', 6, false, false);
            var point2 = new Point('E', 6, false, false);
            Assert.AreEqual(1, point1.DistanceTo(point2));

            point1 = new Point('A', 1, false, false);
            point2 = new Point('J', 1, false, false);
            Assert.AreEqual(10, point1.DistanceTo(point2));

            point1 = new Point('A', 1, false, false);
            point2 = new Point('A', 10, false, false);
            Assert.AreEqual(10, point1.DistanceTo(point2));
        }

    }
}
