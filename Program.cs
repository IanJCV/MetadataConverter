using ImageMagick;

namespace MetadataConverter
{

    internal class Program
    {
        private const string TEXT_CHANGED = "Converted from PNG to JPEG";
        private const string TEXT_NO_CHANGE = "No change necessary";

        private static readonly string[] EXTENSIONS =
            [
            ".mp3",
            ".wav",
            ".ogg",
            ".flac",
            ".m4a",
            ".alac"
            ];

        static void Main(string[] args)
        {
            MagickNET.Initialize();

            if (args.Length == 0)
            {
                Recurse(Directory.GetCurrentDirectory());
            }
            else if (Directory.Exists(args[0]))
            {
                Recurse(args[0]);
            }
        }

        static void Recurse(string directory)
        {
            foreach (var file in Directory.EnumerateFiles(directory))
            {
                if (EXTENSIONS.Contains(Path.GetExtension(file)))
                {
                    ParseFile(file);
                }
            }

            foreach (string dir in Directory.EnumerateDirectories(directory))
            {
                Recurse(dir);
            }
        }

        static void ParseFile(string filename)
        {
            var tfile = TagLib.File.Create(filename);
            string title = tfile.Tag.Title;

            bool changed = false;
            Console.WriteLine("=====================");

            foreach (var image in tfile.Tag.Pictures)
            {
                Console.WriteLine($"image type: {image.MimeType["image/".Length..]}");
                if (image.Type == TagLib.PictureType.FrontCover && image.MimeType == "image/png")
                {
                    changed = true;
                    using var img = new MagickImage(image.Data.Data);

                    img.Format = MagickFormat.Jpeg;
                    if (img.Width > 300)
                    {
                        img.Resize(300, 300);
                    }

                    image.Data = img.ToByteArray();
                    image.MimeType = "image/jpeg";
                    break;
                }
            }

            Console.WriteLine($"Parsed '{filename}' " + (changed ? TEXT_CHANGED : TEXT_NO_CHANGE));
            Console.WriteLine();

            tfile.Save();
            tfile.Dispose();
        }
    }
}
