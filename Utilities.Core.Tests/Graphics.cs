/* See the LICENSE.txt file in the root folder for license details. */

using System;
using System.Drawing;

using NUnit.Framework;

namespace Utilities.Core.UnitTests
{
  [TestFixture]
  public class GraphicsTests
  {
    public GraphicsTests() : base() { }

    private void GetNewImageDimensionsTest(Size boundingRectangle, Size originalRectangle, Size expectedRectangle)
    {
      const String errorMessageFormat = "boundingRectangle = {0}, originalRectangle = {1}, expectedRectangle = {2}, actualRectangle = {3}";

      Size actualRectangle = GraphicsUtils.GetNewSize(originalRectangle, boundingRectangle);
      Assert.IsTrue(actualRectangle == expectedRectangle, String.Format(errorMessageFormat, boundingRectangle, originalRectangle, expectedRectangle, actualRectangle));
    }

    [Test]
    public void GetNewImageDimensionsTests()
    {
      // Bounding rectangle aspect ratio is 4:3
      // Original rectangle aspect ratio varies from < 1.0, = 1.0, to > 1.0.
      // Original rectangle is always smaller than bounding rectangle.

      //                        boundingRectangle    originalRectangle   expectedRectangle
      GetNewImageDimensionsTest(new Size(1000, 750), new Size(500, 200), new Size(1000, 400));
      GetNewImageDimensionsTest(new Size(1000, 750), new Size(400, 300), new Size(1000, 750));
      GetNewImageDimensionsTest(new Size(1000, 750), new Size(500, 500), new Size(750, 750));
      GetNewImageDimensionsTest(new Size(1000, 750), new Size(300, 400), new Size(563, 750));
      GetNewImageDimensionsTest(new Size(1000, 750), new Size(200, 500), new Size(300, 750));

      // Bounding rectangle aspect ratio is 4:3
      // Original rectangle aspect ratio varies from < 1.0, = 1.0, to > 1.0.
      // Original rectangle is always larger than bounding rectangle.

      //                        boundingRectangle    originalRectangle   expectedRectangle
      GetNewImageDimensionsTest(new Size(1000, 750), new Size(5000, 2000), new Size(1000, 400));
      GetNewImageDimensionsTest(new Size(1000, 750), new Size(4000, 3000), new Size(1000, 750));
      GetNewImageDimensionsTest(new Size(1000, 750), new Size(5000, 5000), new Size(750, 750));
      GetNewImageDimensionsTest(new Size(1000, 750), new Size(3000, 4000), new Size(563, 750));
      GetNewImageDimensionsTest(new Size(1000, 750), new Size(2000, 5000), new Size(300, 750));

      // Bounding rectangle aspect ratio is 1:1
      // Original rectangle aspect ratio varies from < 1.0, = 1.0, to > 1.0.
      // Original rectangle is always smaller than bounding rectangle.

      //                        boundingRectangle    originalRectangle   expectedRectangle
      GetNewImageDimensionsTest(new Size(1000, 1000), new Size(500, 200), new Size(1000, 400));
      GetNewImageDimensionsTest(new Size(1000, 1000), new Size(400, 300), new Size(1000, 750));
      GetNewImageDimensionsTest(new Size(1000, 1000), new Size(500, 500), new Size(1000, 1000));
      GetNewImageDimensionsTest(new Size(1000, 1000), new Size(300, 400), new Size(750, 1000));
      GetNewImageDimensionsTest(new Size(1000, 1000), new Size(200, 500), new Size(400, 1000));

      // Bounding rectangle aspect ratio is 1:1
      // Original rectangle aspect ratio varies from < 1.0, = 1.0, to > 1.0.
      // Original rectangle is always larger than bounding rectangle.

      //                        boundingRectangle    originalRectangle   expectedRectangle
      GetNewImageDimensionsTest(new Size(1000, 1000), new Size(5000, 2000), new Size(1000, 400));
      GetNewImageDimensionsTest(new Size(1000, 1000), new Size(4000, 3000), new Size(1000, 750));
      GetNewImageDimensionsTest(new Size(1000, 1000), new Size(5000, 5000), new Size(1000, 1000));
      GetNewImageDimensionsTest(new Size(1000, 1000), new Size(3000, 4000), new Size(750, 1000));
      GetNewImageDimensionsTest(new Size(1000, 1000), new Size(2000, 5000), new Size(400, 1000));

      // Bounding rectangle aspect ratio is 3:4
      // Original rectangle aspect ratio varies from < 1.0, = 1.0, to > 1.0.
      // Original rectangle is always smaller than bounding rectangle.

      //                        boundingRectangle    originalRectangle   expectedRectangle
      GetNewImageDimensionsTest(new Size(750, 1000), new Size(500, 200), new Size(750, 300));
      GetNewImageDimensionsTest(new Size(750, 1000), new Size(400, 300), new Size(750, 562));
      GetNewImageDimensionsTest(new Size(750, 1000), new Size(500, 500), new Size(750, 750));
      GetNewImageDimensionsTest(new Size(750, 1000), new Size(300, 400), new Size(750, 1000));
      GetNewImageDimensionsTest(new Size(750, 1000), new Size(200, 500), new Size(400, 1000));

      // Bounding rectangle aspect ratio is 3:4
      // Original rectangle aspect ratio varies from < 1.0, = 1.0, to > 1.0.
      // Original rectangle is always larger than bounding rectangle.

      //                        boundingRectangle    originalRectangle   expectedRectangle
      GetNewImageDimensionsTest(new Size(750, 1000), new Size(5000, 2000), new Size(750, 300));
      GetNewImageDimensionsTest(new Size(750, 1000), new Size(4000, 3000), new Size(750, 562));
      GetNewImageDimensionsTest(new Size(750, 1000), new Size(5000, 5000), new Size(750, 750));
      GetNewImageDimensionsTest(new Size(750, 1000), new Size(3000, 4000), new Size(750, 1000));
      GetNewImageDimensionsTest(new Size(750, 1000), new Size(2000, 5000), new Size(400, 1000));
    }
  }
}
