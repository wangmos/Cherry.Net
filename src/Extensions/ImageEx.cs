using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Cherry.Net.Extensions
{
    public static class ImageEx
    {
        //会产生graphics异常的PixelFormat 
        private static readonly PixelFormat[] IndexedPixelFormats = { PixelFormat.Undefined, PixelFormat.DontCare, PixelFormat.Format16bppArgb1555,
            PixelFormat.Format1bppIndexed, PixelFormat.Format4bppIndexed, PixelFormat.Format8bppIndexed };

        /// <summary>
        /// 字节转图片
        /// </summary>
        /// <param name="bs"></param>
        /// <returns></returns>
        public static Image ToImg(this byte[] bs)
        {
            using (var ms = new MemoryStream(bs))
            {
                return Image.FromStream(ms, true, true);
            }
        }

        /// <summary>
        /// 图片转字节
        /// </summary>
        /// <param name="img"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static byte[] ToBytes(this Image img, ImageFormat format = null)
        {
            var copy = false;
            using (var ms = new MemoryStream())
            {
                for (var i = 0; i < 2; i++)
                {
                    try
                    {
                        img.Save(ms, format ?? ImageFormat.Jpeg);
                        if (copy) img.Dispose();
                        return ms.ToArray();
                    }
                    catch (Exception e)
                    {
                        img = img.CopyImg();
                        copy = true;
                        Debug.WriteLine(e);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 转化图片
        /// </summary>
        /// <param name="srcImg"></param>
        /// <param name="format"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static Bitmap ConvertImg(this Image srcImg, PixelFormat format, int width, int height)
        {
            var bt = new Bitmap(width, height, 0, format, IntPtr.Zero);
            using (var g = Graphics.FromImage(bt))
            {
                g.DrawImage(srcImg, new Rectangle(0, 0, width, height), new Rectangle(0, 0, srcImg.Width, srcImg.Height), GraphicsUnit.Pixel);
            }
            return bt;
        }

        /// <summary>
        /// 复制图片
        /// </summary>
        /// <param name="img"></param>
        /// <returns></returns>
        public static Bitmap CopyImg(this Image img)
        {
            try
            {
                var bmp = new Bitmap(img.Width, img.Height, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.CompositingQuality = CompositingQuality.HighQuality;
                    g.DrawImage(img, 0, 0);
                }
                return bmp;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                return null;
            }
        }

        /// <summary>
        /// 是否是索引像素格式
        /// </summary>
        /// <param name="pf"></param>
        /// <returns></returns>
        public static bool IsIndexedPixelFormat(PixelFormat pf) => IndexedPixelFormats.Contains(pf);

        /// <summary>
        /// 水印
        /// </summary>
        /// <param name="img"></param>
        /// <param name="msg"></param>
        /// <param name="font"></param>
        /// <param name="brush"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static Bitmap AddString(this Image img, string msg, Font font, Brush brush, PointF point)
        {
            var bmp = IsIndexedPixelFormat(img.PixelFormat) ? img.CopyImg() : new Bitmap(img);

            using (var g = Graphics.FromImage(bmp))
            {
                g.DrawString(msg, font, brush, point);
            }
            return bmp;
        }

        /// <summary>
        /// 截屏
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="left"></param>
        /// <param name="up"></param>
        /// <returns></returns>
        public static Bitmap CutScreen(int width = 0, int height = 0, int left = 0, int up = 0)
        {
            var bit = new Bitmap(width <= 0 ? Screen.PrimaryScreen.Bounds.Width : width, height <= 0 ? Screen.PrimaryScreen.Bounds.Height : height);
            using (var g = Graphics.FromImage(bit))
            {
                g.CopyFromScreen(new Point(left, up), new Point(0, 0), bit.Size);
            }
            return bit;
        }
    }
}