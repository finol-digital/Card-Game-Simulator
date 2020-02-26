using System;
using System.IO;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;

using MigraDocCore.DocumentObjectModel.MigraDoc.DocumentObjectModel.Shapes;

using static MigraDocCore.DocumentObjectModel.MigraDoc.DocumentObjectModel.Shapes.ImageSource;
using ISImage = SixLabors.ImageSharp.Image;

public class ImageSharpImageSource : ImageSource
    {
        protected override IImageSource FromBinaryImpl(string name, Func<byte[]> imageSource, bool isJpeg = true, int? quality = 75)
            => new ImageSharpImageSourceImpl(name, () => ISImage.Load(imageSource.Invoke()), isJpeg, quality ?? 75);

        protected override IImageSource FromFileImpl(string path, bool isJpeg = true, int? quality = 75)
            => new ImageSharpImageSourceImpl(path, () => (Image<Rgba32>)ISImage.Load(path), isJpeg, quality ?? 75);

        protected override IImageSource FromStreamImpl(string name, Func<Stream> imageStream, bool isJpeg = true, int? quality = 75)
            => new ImageSharpImageSourceImpl(name, () =>
            {
                using (var stream = imageStream.Invoke())
                {
                    return (Image<Rgba32>)ISImage.Load(stream);
                }
            }, isJpeg, quality ?? 75);
    }

    public class ImageSharpImageSourceImpl : IImageSource, IDisposable
    {
        private Func<Image<Rgba32>> _getImage;
        private readonly int _quality;

        private Image<Rgba32> _image;
        public Image<Rgba32> Image
        {
            get
            {
                if (_image == null && _getImage != null)
                {
                    _image = _getImage.Invoke();
                }
                return _image;
            }
        }

        public int Width => Image.Width;
        public int Height => Image.Height;

        public string Name { get; }

        public bool IsJpeg { get; }

        public ImageSharpImageSourceImpl(string name, Func<Image<Rgba32>> getImage, bool isJpeg, int quality)
        {
            Name = name;
            IsJpeg = isJpeg;
            _getImage = getImage;
            _quality = quality;
        }

        public void SaveAsJpeg(MemoryStream ms) => Image.SaveAsJpeg(ms, new JpegEncoder { Quality = _quality });

        public void SaveAsBmp(MemoryStream ms) => Image.SaveAsBmp(ms, new BmpEncoder { BitsPerPixel = BmpBitsPerPixel.Pixel32 });

        public void Dispose() => Image.Dispose();
    }

