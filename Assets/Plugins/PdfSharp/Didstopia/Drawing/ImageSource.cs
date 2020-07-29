using System;
using System.Collections.Generic;
using System.IO;
using System.Text;


namespace MigraDocCore.DocumentObjectModel.MigraDoc.DocumentObjectModel.Shapes
{
    public abstract class ImageSource
    {
        public static ImageSource ImageSourceImpl { get; set; }

        public interface IImageSource
        {
            int Width { get; }
            int Height { get; }
            string Name { get; }
            bool IsJpeg { get; }
            void SaveAsJpeg(MemoryStream ms);
            void SaveAsBmp(MemoryStream ms);
        }

        protected abstract IImageSource FromFileImpl(string path, bool isJpeg = true, int? quality = 75);
        protected abstract IImageSource FromBinaryImpl(string name, Func<byte[]> imageSource, bool isJpeg = true, int? quality = 75);
        protected abstract IImageSource FromStreamImpl(string name, Func<Stream> imageStream, bool isJpeg = true, int? quality = 75);

        public static IImageSource FromFile(string path, bool isJpeg = true, int? quality = 75)
        {
            try { return ImageSourceImpl.FromFileImpl(path, isJpeg, quality); }
            catch (Exception) { throw new Exception("Invalid or missing ImageSource implementation (a custom implementation is required)"); }
        }

        public static IImageSource FromBinary(string name, Func<byte[]> imageSource, bool isJpeg = true, int? quality = 75)
        {
            try { return ImageSourceImpl.FromBinaryImpl(name, imageSource, isJpeg, quality); }
            catch (Exception) { throw new Exception("Invalid or missing ImageSource implementation (a custom implementation is required)"); }
        }

        public static IImageSource FromStream(string name, Func<Stream> imageStream, bool isJpeg = true, int? quality = 75)
        {
            try { return ImageSourceImpl.FromStreamImpl(name, imageStream, isJpeg, quality); }
            catch (Exception) { throw new Exception("Invalid or missing ImageSource implementation (a custom implementation is required)"); }
        }
    }
}
