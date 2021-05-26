using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Perpetuum
{
    public static class BitmapExtensions
    {
        [CanBeNull]
        public static Bitmap WithGraphics(this Bitmap bitmap,Action<Graphics> action)
        {
            if (bitmap == null)
                return null;

            using (var g = Graphics.FromImage(bitmap))
            {
                action(g);
            }

            return bitmap;
        }

        /// <summary>
        /// Runs an action on every pixel of a bitmap
        /// </summary>
        [CanBeNull]
        public static Bitmap ForEach(this Bitmap bitmap, Action<Bitmap, int, int> action)
        {
            if (bitmap == null)
                return null;

            for (var j = 0; j < bitmap.Height; j++)
            {
                for (var i = 0; i < bitmap.Width; i++)
                {
                    action(bitmap, i, j);
                }
            }

            return bitmap;
        }


        public static Bitmap DilateOrErode(this Bitmap sourceBitmap, int matrixSize, bool isErode)
        {
            var sourceData = sourceBitmap.LockBits(
                new Rectangle(0, 0, sourceBitmap.Width, sourceBitmap.Height),
                ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            byte[] pixelBuffer = new byte[sourceData.Stride * sourceData.Height];
            byte[] resultBuffer = new byte[sourceData.Stride * sourceData.Height];

            Marshal.Copy(sourceData.Scan0, pixelBuffer, 0, pixelBuffer.Length);

            sourceBitmap.UnlockBits(sourceData);

            var filterOffset = (matrixSize - 1) / 2;
            var morphResetValue = (byte)(isErode ? 0 : 1);

            for (var offsetY = filterOffset; offsetY < sourceBitmap.Height - filterOffset; offsetY++)
            {
                for (var offsetX = filterOffset; offsetX < sourceBitmap.Width - filterOffset; offsetX++)
                {
                    var byteOffset = offsetY * sourceData.Stride + offsetX * 4;
                    var value = morphResetValue;

                    for (var filterY = -filterOffset; filterY <= filterOffset; filterY++)
                    {
                        for (var filterX = -filterOffset; filterX <= filterOffset; filterX++)
                        {
                            var calcOffset = byteOffset + (filterX * 4) + (filterY * sourceData.Stride);
                            if (isErode)
                            {
                                if (pixelBuffer[calcOffset] < value)
                                {
                                    value = pixelBuffer[calcOffset];
                                }
                            }
                            else
                            {
                                if (pixelBuffer[calcOffset] > value)
                                {
                                    value = pixelBuffer[calcOffset];
                                }
                            }

                        }
                    }
                    value = pixelBuffer[byteOffset];
                    resultBuffer[byteOffset] = value;
                }
            }

            var resultBitmap = new Bitmap(sourceBitmap.Width, sourceBitmap.Height);

            var resultData = resultBitmap.LockBits(
                new Rectangle(0, 0, resultBitmap.Width, resultBitmap.Height),
                ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            Marshal.Copy(resultBuffer, 0, resultData.Scan0, resultBuffer.Length);

            resultBitmap.UnlockBits(resultData);

            return resultBitmap;
        }
    }
}
