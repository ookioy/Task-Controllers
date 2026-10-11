using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Triangles.Models;

namespace Triangles.Controllers
{
    public class TriangleController : Controller
    {
        private const double RelativeTolerance = 0.00001;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid
                || context.ActionArguments.Values.Any(argument => !ContainsOnlyValidTriangles(argument)))
            {
                context.Result = new ContentResult
                {
                    StatusCode = 400,
                    ContentType = "text/html",
                    Content = "<!doctype html><html><body><h1>Invalid triangle</h1>"
                        + "<p>Side lengths must be positive and satisfy the triangle inequality.</p></body></html>"
                };
                return;
            }

            base.OnActionExecuting(context);
        }

        public string Info([Bind(Prefix = "")] Triangle triangle)
        {
            var sides = GetSortedSides(triangle);
            var perimeter = sides.Sum();
            var area = CalculateArea(sides);

            return string.Format(
                "Triangle:{0}({1}, {2}, {3}){0}"
                + "Reduced:{0}({4:F2}, {5:F2}, {6:F2}){0}{0}"
                + "Area = {7:F2}{0}"
                + "Perimeter = {8}",
                Environment.NewLine,
                sides[0],
                sides[1],
                sides[2],
                sides[0] / perimeter,
                sides[1] / perimeter,
                sides[2] / perimeter,
                area,
                perimeter);
        }

        public string Area([Bind(Prefix = "")] Triangle triangle)
        {
            return CalculateArea(GetSortedSides(triangle)).ToString("F4");
        }

        public double Perimeter([Bind(Prefix = "")] Triangle triangle)
        {
            return GetSortedSides(triangle).Sum();
        }

        public bool IsRightAngled([Bind(Prefix = "")] Triangle triangle)
        {
            var sides = GetSortedSides(triangle);
            var largest = sides[2];
            return AreEqual(
                (sides[0] / largest) * (sides[0] / largest)
                    + (sides[1] / largest) * (sides[1] / largest),
                1);
        }

        public bool IsEquilateral([Bind(Prefix = "")] Triangle triangle)
        {
            var sides = GetSortedSides(triangle);
            return AreEqual(sides[0], sides[1]) && AreEqual(sides[0], sides[2]);
        }

        public bool IsIsosceles([Bind(Prefix = "")] Triangle triangle)
        {
            var sides = GetSortedSides(triangle);
            return AreEqual(sides[0], sides[1])
                || AreEqual(sides[1], sides[2])
                || AreEqual(sides[0], sides[2]);
        }

        public bool AreCongruent(
            [Bind(Prefix = "tr1")] Triangle tr1,
            [Bind(Prefix = "tr2")] Triangle tr2)
        {
            var firstSides = GetSortedSides(tr1);
            var secondSides = GetSortedSides(tr2);
            return firstSides.Zip(secondSides, AreEqual).All(equal => equal);
        }

        public bool AreSimilar(
            [Bind(Prefix = "tr1")] Triangle tr1,
            [Bind(Prefix = "tr2")] Triangle tr2)
        {
            var firstSides = GetSortedSides(tr1);
            var secondSides = GetSortedSides(tr2);
            return AreEqual(firstSides[0] / firstSides[2], secondSides[0] / secondSides[2])
                && AreEqual(firstSides[1] / firstSides[2], secondSides[1] / secondSides[2]);
        }

        [ActionName("GreatesByPerimeter")]
        public string InfoGreatestPerimeter([Bind(Prefix = "tr")] Triangle[] triangles)
        {
            return Info(triangles.OrderByDescending(Perimeter).First());
        }

        [HttpGet("/triangle/GreatestByPerimeter")]
        public string GreatestByPerimeter([Bind(Prefix = "tr")] Triangle[] triangles)
        {
            return InfoGreatestPerimeter(triangles);
        }

        public string InfoGreatestArea([Bind(Prefix = "tr")] Triangle[] triangles)
        {
            var triangle = triangles
                .OrderByDescending(item => CalculateArea(GetSortedSides(item)))
                .First();
            return Info(triangle);
        }

        [ActionName("GreatestByArea")]
        public string GreatestByArea([Bind(Prefix = "tr")] Triangle[] triangles)
        {
            return InfoGreatestArea(triangles);
        }

        public string NumbersPairwiseNotSimilar([Bind(Prefix = "tr")] Triangle[] triangles)
        {
            var pairs = from first in Enumerable.Range(0, triangles.Length)
                        from second in Enumerable.Range(first + 1, triangles.Length - first - 1)
                        where !AreSimilar(triangles[first], triangles[second])
                        select $"({first + 1}, {second + 1})";

            return string.Join(Environment.NewLine, pairs);
        }

        [ActionName("PairwiseNonSimilar")]
        public string PairwiseNonSimilar([Bind(Prefix = "tr")] Triangle[] triangles)
        {
            return NumbersPairwiseNotSimilar(triangles);
        }

        private static bool ContainsOnlyValidTriangles(object argument)
        {
            if (argument == null)
            {
                return false;
            }

            if (argument is Triangle triangle)
            {
                return IsValidTriangle(triangle);
            }

            if (argument is Triangle[] triangles)
            {
                return triangles.Length > 0 && triangles.All(IsValidTriangle);
            }

            return true;
        }

        private static bool IsValidTriangle(Triangle triangle)
        {
            if (triangle == null)
            {
                return false;
            }

            var sides = GetSortedSides(triangle);
            return sides.All(side => !double.IsNaN(side) && !double.IsInfinity(side) && side > 0)
                && sides[0] + sides[1] > sides[2]
                && !double.IsInfinity(sides.Sum());
        }

        private static double[] GetSortedSides(Triangle triangle)
        {
            return new[] { triangle.Side1, triangle.Side2, triangle.Side3 }
                .OrderBy(side => side)
                .ToArray();
        }

        private static double CalculateArea(double[] sortedSides)
        {
            var semiperimeter = sortedSides.Sum() / 2;
            return Math.Sqrt(
                semiperimeter
                * (semiperimeter - sortedSides[0])
                * (semiperimeter - sortedSides[1])
                * (semiperimeter - sortedSides[2]));
        }

        private static bool AreEqual(double first, double second)
        {
            return Math.Abs(first - second) < Math.Abs(first) * RelativeTolerance;
        }
    }
}
