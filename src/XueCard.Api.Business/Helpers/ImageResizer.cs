using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace XueCard.Api.Business.Helpers
{
    public static class ImageResizer
    {
        public static Stream ResizeImage(Stream originalImageStream, int width, int height)
        {
            using var originalImage = Image.FromStream(originalImageStream);

            var resizedBitmap = new Bitmap(width, height);
            using (var graphics = Graphics.FromImage(resizedBitmap))
            {
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;

                graphics.DrawImage(originalImage, 0, 0, width, height);
            }

            var resizedStream = new MemoryStream();
            resizedBitmap.Save(resizedStream, ImageFormat.Png);
            resizedStream.Position = 0;
            return resizedStream;
        }
    }
}