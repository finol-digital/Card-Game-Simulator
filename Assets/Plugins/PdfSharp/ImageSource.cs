using System;
using System.IO;
using MigraDocCore.DocumentObjectModel.MigraDoc.DocumentObjectModel.Shapes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using UnityEngine;
using static MigraDocCore.DocumentObjectModel.MigraDoc.DocumentObjectModel.Shapes.ImageSource;
using Object = UnityEngine.Object;

// ReSharper disable once CheckNamespace
public class ImageSharpImageSource : ImageSource
{
    protected override IImageSource FromBinaryImpl(string name, Func<byte[]> imageSource, bool isJpeg = true,
        int? quality = 75)
    {
        return new ImageSharpImageSourceImpl(name, () =>
        {
            var texture2D = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture2D.LoadImage(imageSource.Invoke());
            return texture2D;
        }, isJpeg, quality ?? 75);
    }

    protected override IImageSource FromFileImpl(string path, bool isJpeg = true, int? quality = 75)
    {
        return new ImageSharpImageSourceImpl(path, () =>
        {
            var texture2D = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture2D.LoadImage(File.ReadAllBytes(path));
            return texture2D;
        }, isJpeg, quality ?? 75);
    }

    protected override IImageSource FromStreamImpl(string name, Func<Stream> imageStream, bool isJpeg = true,
        int? quality = 75)
    {
        return new ImageSharpImageSourceImpl(name, () =>
        {
            byte[] b;
            using (Stream stream = imageStream.Invoke())
            using (var ms = new MemoryStream())
            {
                int count;
                do
                {
                    byte[] buf = new byte[1024];
                    count = stream.Read(buf, 0, 1024);
                    ms.Write(buf, 0, count);
                } while (stream.CanRead && count > 0);

                b = ms.ToArray();
            }

            var texture2D = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture2D.LoadImage(b);
            return texture2D;
        }, isJpeg, quality ?? 75);
    }
}

public class ImageSharpImageSourceImpl : IImageSource, IDisposable
{
    private readonly Func<Texture2D> _getImage;
    private readonly int _quality;

    private Texture2D Image => _image ? _image : _image = _getImage?.Invoke();
    private Texture2D _image;

    public int Width => Image.width;
    public int Height => Image.height;

    public string Name { get; }

    public bool IsJpeg { get; }

    public ImageSharpImageSourceImpl(string name, Func<Texture2D> getImage, bool isJpeg, int quality)
    {
        Name = name;
        IsJpeg = isJpeg;
        _getImage = getImage;
        _quality = quality;
    }

    public void SaveAsJpeg(MemoryStream ms)
    {
        byte[] bytes = Image.EncodeToJPG(_quality);
        Image<Rgba32> image = SixLabors.ImageSharp.Image.Load(bytes);
        image.SaveAsJpeg(ms, new JpegEncoder {Quality = _quality});
    }

    public void SaveAsBmp(MemoryStream ms)
    {
        byte[] bytes = Image.EncodeToPNG();
        Image<Rgba32> image = SixLabors.ImageSharp.Image.Load(bytes);
        image.SaveAsBmp(ms, new BmpEncoder {BitsPerPixel = BmpBitsPerPixel.Pixel32});
    }

    public void Dispose() => Object.Destroy(Image);
}
