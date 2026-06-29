using System;
using System.Collections.Generic;
using BitMiracle.LibTiff.Classic;

namespace Teniko
{
    public class TiffPageInfo
    {
        public int PageNumber { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public float DpiX { get; set; }
        public float DpiY { get; set; }
        public int Orientation { get; set; }
        public byte[]? WangAnnotationData { get; set; }
        public string? TempImagePath { get; set; }
    }

    public class TiffReader : IDisposable
    {
        // Tag ID for Wang Annotation Data
        private const TiffTag TIFFTAG_WANG_ANNOTATION = (TiffTag)32932;

        public TiffReader()
        {
            // Register custom tags to LibTiff.NET so it doesn't throw on unknown tags
            Tiff.SetTagExtender(TagExtender);
            Tiff.SetErrorHandler(new SilentErrorHandler());
        }

        class SilentErrorHandler : BitMiracle.LibTiff.Classic.TiffErrorHandler
        {
            public override void WarningHandler(Tiff tif, string module, string fmt, params object[] args) { }
            public override void WarningHandlerExt(Tiff tif, object clientData, string module, string fmt, params object[] args) { }
            public override void ErrorHandler(Tiff tif, string module, string fmt, params object[] args) { }
            public override void ErrorHandlerExt(Tiff tif, object clientData, string module, string fmt, params object[] args) { }
        }

        private static void TagExtender(Tiff tif)
        {
            var wangTag = new TiffFieldInfo(
                TIFFTAG_WANG_ANNOTATION,
                -1, -1,
                TiffType.BYTE,
                FieldBit.Custom,
                true,
                true,
                "WangAnnotationData"
            );

            var unknownTag32934 = new TiffFieldInfo(
                (TiffTag)32934,
                -1, -1,
                TiffType.ANY,
                FieldBit.Custom,
                true,
                true,
                "UnknownTag32934"
            );

            TiffFieldInfo[] info = { wangTag, unknownTag32934 };
            tif.MergeFieldInfo(info, info.Length);
        }

        public List<TiffPageInfo> ExtractPages(string tiffPath)
        {
            var pages = new List<TiffPageInfo>();

            using (Tiff tif = Tiff.Open(tiffPath, "r"))
            {
                if (tif == null)
                    throw new InvalidOperationException($"TIFFファイル '{tiffPath}' を開けませんでした。");

                int pageNumber = 0;
                do
                {
                    pageNumber++;
                    var pageInfo = new TiffPageInfo { PageNumber = pageNumber };

                    // Get basic properties
                    pageInfo.Width = tif.GetField(TiffTag.IMAGEWIDTH)?[0].ToInt() ?? 0;
                    pageInfo.Height = tif.GetField(TiffTag.IMAGELENGTH)?[0].ToInt() ?? 0;

                    // Get DPI (defaults to 72 if missing)
                    pageInfo.DpiX = tif.GetField(TiffTag.XRESOLUTION)?[0].ToFloat() ?? 72f;
                    pageInfo.DpiY = tif.GetField(TiffTag.YRESOLUTION)?[0].ToFloat() ?? 72f;

                    // Get Orientation (defaults to 1 = TopLeft)
                    pageInfo.Orientation = tif.GetField(TiffTag.ORIENTATION)?[0].ToInt() ?? 1;

                    // Try reading Wang Annotation Tag (32932)
                    FieldValue[]? wangTagData = tif.GetField(TIFFTAG_WANG_ANNOTATION);
                    if (wangTagData != null && wangTagData.Length >= 2)
                    {
                        // The tag data is returned as [length, byte[]]
                        int dataLength = wangTagData[0].ToInt();
                        byte[] rawBytes = wangTagData[1].GetBytes();
                        
                        if (rawBytes != null && dataLength > 0)
                        {
                            Console.WriteLine($"[Debug] Page {pageNumber}: WangAnnotationDataを検出しました ({dataLength} バイト)。");
                            pageInfo.WangAnnotationData = rawBytes;
                        }
                        else
                        {
                            Console.WriteLine($"[Debug] Page {pageNumber}: WangAnnotationDataタグは存在しますが、データが空です。");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[Debug] Page {pageNumber}: WangAnnotationData (タグ32932) は見つかりませんでした。");
                    }

                    // For PDF generation, we need to extract this page as an image (PNG or JPG is easiest for PdfSharp)
                    // LibTiff.NET doesn't easily save to PNG/JPG, but we can extract raw RGBA and create a bitmap.
                    // However, we will assume the caller or another step handles actual image embedding.
                    // To keep things simple in this parser, we just pass the info. The PdfSharp generator
                    // will use XImage.FromFile(tiff) if possible, but XImage might not support multi-page TIFF perfectly
                    // natively without specific handling. We'll handle image extraction in PdfGenerator or here.

                    pages.Add(pageInfo);

                } while (tif.ReadDirectory());
            }

            return pages;
        }

        public void Dispose()
        {
            // Cleanup any temporary files if created
        }
    }
}
