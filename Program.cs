using ImageMagick;
using TagLib;

namespace MetadataConverter
{

    internal class Program
    {
        private const string TEXT_CONVERTED = "Converted from PNG to JPEG";
        private const string TEXT_RESIZED = "Resized to fit 300x300";
        private const string TEXT_NO_CONVERT = "No conversion necessary";

        private static readonly string[] EXTENSIONS =
            [
            ".mp3",
            ".wav",
            ".ogg",
            ".flac",
            ".m4a",
            ".alac"
            ];

        private static bool NO_CROP;

        static int Main(string[] args)
        {
            MagickNET.Initialize();

            int len = args.Length;

            if (len == 0)
            {
                Recurse(Directory.GetCurrentDirectory());
            }
            else if (len == 1)
            {
                if (args[0] == "--nocrop")
                {
                    NO_CROP = true;
                }

                if (!Directory.Exists(args[0]))
                {
                    Console.WriteLine($"Provided directory doesn't exist. Dir: '{args[0]}'");
                    return 1;
                }

                Recurse(args[0]);
            }
            else if (len == 2)
            {
                if (args[0] == "--nocrop")
                {
                    NO_CROP = true;
                }
                else
                {
                    Console.WriteLine("Incorrect argument.\nExpected format:\n\t'[--nocrop] [directory]'");
                    return 1;
                }

                if (!Directory.Exists(args[1]))
                {
                    Console.WriteLine($"Provided directory doesn't exist. Dir: '{args[1]}'");
                    return 1;
                }

                Recurse(args[1]);
            }

            return 0;
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

            bool converted = false;
            bool resized = false;
            Console.WriteLine("=====================");

            foreach (var tagImage in tfile.Tag.Pictures)
            {
                Console.WriteLine($"image type: {tagImage.MimeType["image/".Length..]}");
                if (tagImage.Type == PictureType.FrontCover)
                {
                    var img = new MagickImage(tagImage.Data.Data);

                    if (tagImage.MimeType == "image/png")
                    {
                        converted = true;
                        img.Format = MagickFormat.Jpeg;
                        tagImage.MimeType = "image/jpeg";
                    }

                    if (img.Width > 300)
                    {
                        resized = true;
                        img.Resize(300, 300);

                        if (!NO_CROP && (img.Width != img.Height))
                        {
                            img.Crop(300, 300);
                            img.ResetPage();
                        }

                    }

                    if (converted || resized)
                    {
                        tagImage.Data = new ByteVector(img.ToByteArray());
                    }
                    img.Dispose();
                    break;
                }
            }

            Console.WriteLine($"Parsed '{filename}' " + (converted ? TEXT_CONVERTED : TEXT_NO_CONVERT) + ". " + (resized ? TEXT_RESIZED : string.Empty));
            Console.WriteLine();

            tfile.Save();
            tfile.Dispose();
        }
    }
}
