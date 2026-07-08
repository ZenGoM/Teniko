using System;
using System.IO;
using System.Text;
using BitMiracle.LibTiff.Classic;

namespace Teniko
{
    public class MockWangGenerator
    {
        public static void GenerateMockTiff(string sourceTiff, string destTiff)
        {
            byte[] wangData = CreateWangData();

            var wangTag = new TiffFieldInfo(
                (TiffTag)32932, -1, -1, TiffType.BYTE, FieldBit.Custom, true, true, "WangAnnotationData"
            );
            TiffFieldInfo[] info = { wangTag };

            Tiff.SetTagExtender((tif) =>
            {
                tif.MergeFieldInfo(info, info.Length);
            });

            using (Tiff src = Tiff.Open(sourceTiff, "r"))
            using (Tiff dst = Tiff.Open(destTiff, "w"))
            {
                int width = src.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
                int height = src.GetField(TiffTag.IMAGELENGTH)[0].ToInt();

                dst.SetField(TiffTag.IMAGEWIDTH, width);
                dst.SetField(TiffTag.IMAGELENGTH, height);
                dst.SetField(TiffTag.BITSPERSAMPLE, 8);
                dst.SetField(TiffTag.SAMPLESPERPIXEL, 1);
                dst.SetField(TiffTag.COMPRESSION, Compression.DEFLATE);
                dst.SetField(TiffTag.PHOTOMETRIC, Photometric.MINISBLACK);
                dst.SetField(TiffTag.XRESOLUTION, 300.0f);
                dst.SetField(TiffTag.YRESOLUTION, 300.0f);
                dst.SetField(TiffTag.RESOLUTIONUNIT, ResUnit.INCH);

                byte[] buf = new byte[width];
                for (int i = 0; i < width; i++) buf[i] = 255;
                for (int i = 0; i < height; i++) dst.WriteScanline(buf, i);

                dst.SetField((TiffTag)32932, wangData.Length, wangData);
                dst.WriteDirectory();
            }
        }

        public static void GenerateLargeTiff(string sourceTiff, string destTiff, int pageCount = 50)
        {
            byte[] wangData = CreateWangData();

            var wangTag = new TiffFieldInfo(
                (TiffTag)32932, -1, -1, TiffType.BYTE, FieldBit.Custom, true, true, "WangAnnotationData"
            );
            TiffFieldInfo[] info = { wangTag };

            Tiff.SetTagExtender((tif) =>
            {
                tif.MergeFieldInfo(info, info.Length);
            });

            using (Tiff src = Tiff.Open(sourceTiff, "r"))
            using (Tiff dst = Tiff.Open(destTiff, "w"))
            {
                int width = src.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
                int height = src.GetField(TiffTag.IMAGELENGTH)[0].ToInt();

                for (int page = 0; page < pageCount; page++)
                {
                    dst.SetField(TiffTag.IMAGEWIDTH, width);
                    dst.SetField(TiffTag.IMAGELENGTH, height);
                    dst.SetField(TiffTag.BITSPERSAMPLE, 8);
                    dst.SetField(TiffTag.SAMPLESPERPIXEL, 1);
                    dst.SetField(TiffTag.COMPRESSION, Compression.DEFLATE);
                    dst.SetField(TiffTag.PHOTOMETRIC, Photometric.MINISBLACK);
                    dst.SetField(TiffTag.XRESOLUTION, 300.0f);
                    dst.SetField(TiffTag.YRESOLUTION, 300.0f);
                    dst.SetField(TiffTag.RESOLUTIONUNIT, ResUnit.INCH);

                    if (page < 2)
                    {
                        dst.SetField((TiffTag)32932, wangData.Length, wangData);
                    }

                    byte[] buf = new byte[width];
                    for (int i = 0; i < width; i++) buf[i] = (byte)(255 - (page % 255));
                    for (int i = 0; i < height; i++) dst.WriteScanline(buf, i);

                    dst.WriteDirectory();
                }
            }
        }

        public static void GenerateFormatTests()
        {
            int width = 100;
            int height = 100;

            using (Tiff dst = Tiff.Open("test_1bit.tif", "w"))
            {
                dst.SetField(TiffTag.IMAGEWIDTH, width);
                dst.SetField(TiffTag.IMAGELENGTH, height);
                dst.SetField(TiffTag.BITSPERSAMPLE, 1);
                dst.SetField(TiffTag.SAMPLESPERPIXEL, 1);
                dst.SetField(TiffTag.COMPRESSION, Compression.NONE);
                dst.SetField(TiffTag.PHOTOMETRIC, Photometric.MINISBLACK);
                byte[] buf = new byte[(width + 7) / 8];
                for (int i = 0; i < height; i++) dst.WriteScanline(buf, i);
                dst.WriteDirectory();
            }

            using (Tiff dst = Tiff.Open("test_8bit.tif", "w"))
            {
                dst.SetField(TiffTag.IMAGEWIDTH, width);
                dst.SetField(TiffTag.IMAGELENGTH, height);
                dst.SetField(TiffTag.BITSPERSAMPLE, 8);
                dst.SetField(TiffTag.SAMPLESPERPIXEL, 1);
                dst.SetField(TiffTag.COMPRESSION, Compression.NONE);
                dst.SetField(TiffTag.PHOTOMETRIC, Photometric.MINISBLACK);
                byte[] buf = new byte[width];
                for (int i = 0; i < height; i++) dst.WriteScanline(buf, i);
                dst.WriteDirectory();
            }

            using (Tiff dst = Tiff.Open("test_24bit.tif", "w"))
            {
                dst.SetField(TiffTag.IMAGEWIDTH, width);
                dst.SetField(TiffTag.IMAGELENGTH, height);
                dst.SetField(TiffTag.BITSPERSAMPLE, 8);
                dst.SetField(TiffTag.SAMPLESPERPIXEL, 3);
                dst.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);
                dst.SetField(TiffTag.COMPRESSION, Compression.NONE);
                dst.SetField(TiffTag.PHOTOMETRIC, Photometric.RGB);
                byte[] buf = new byte[width * 3];
                for (int i = 0; i < height; i++) dst.WriteScanline(buf, i);
                dst.WriteDirectory();
            }
        }

        private static byte[] CreateWangData()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                // Header (8 bytes)
                bw.Write((int)0); // reserved
                bw.Write((int)1); // 1 = Intel 32-bit

                // --- Annot-01: Straight Line (WangMarkType.StraightLine = 3)
                WriteMarkAttributes(bw, 3, 10, 10, 110, 110, new byte[]{255,0,0}, new byte[]{0,0,0}, 0, 2);
                WriteOiAnoDatPoints(bw, new int[]{0, 0, 100, 100});

                // --- Annot-02: Text (WangMarkType.TypedText = 7)
                WriteMarkAttributes(bw, 7, 100, 100, 300, 150, new byte[]{0,255,0}, new byte[]{0,0,0}, 0, 1);
                WriteOiAnText(bw, "Test Text");

                // --- Annot-03: Highlight (WangMarkType.StraightLine = 3, Highlighting = 1)
                WriteMarkAttributes(bw, 3, 200, 200, 300, 220, new byte[]{0,255,255}, new byte[]{0,0,0}, 1, 15);
                WriteOiAnoDatPoints(bw, new int[]{0, 10, 100, 10});

                // --- Annot-04: Attach-a-Note (WangMarkType.AttachANote = 10)
                WriteMarkAttributes(bw, 10, 400, 100, 500, 200, new byte[]{200,200,200}, new byte[]{0,0,0}, 0, 1);
                WriteOiAnText(bw, "Sticky Note");

                // --- Annot-05: Rectangle (WangMarkType.HollowRectangle = 5)
                WriteMarkAttributes(bw, 5, 50, 250, 150, 350, new byte[]{255,0,255}, new byte[]{0,0,0}, 0, 3);

                // --- Annot-06: Stamp (WangMarkType.TextStamp = 9)
                WriteMarkAttributes(bw, 9, 300, 300, 400, 350, new byte[]{0,0,255}, new byte[]{0,0,0}, 0, 1);
                WriteOiAnText(bw, "APPROVED");

                // --- Annot-07: Scaling (WangMarkType.TypedText = 7, creationScale = 480)
                WriteMarkAttributes(bw, 7, 100, 400, 300, 450, new byte[]{128,128,0}, new byte[]{0,0,0}, 0, 1);
                WriteOiAnText(bw, "Scaled Text", 480);

                // --- Annot-08: Japanese Encoding (WangMarkType.TypedText = 7, Shift-JIS characters)
                WriteMarkAttributes(bw, 7, 400, 400, 600, 450, new byte[]{0,128,128}, new byte[]{0,0,0}, 0, 1);
                WriteOiAnText(bw, "テスト注釈です");

                return ms.ToArray();
            }
        }

        private static void WriteMarkAttributes(BinaryWriter bw, uint type, int left, int top, int right, int bottom, byte[] color1, byte[] color2, int bHighlighting, uint lineSize)
        {
            bw.Write((int)5); // Type 5
            bw.Write((int)164); // Size 164
            bw.Write((uint)type); // type
            bw.Write((int)left); bw.Write((int)top); bw.Write((int)right); bw.Write((int)bottom);
            bw.Write(color1[0]); bw.Write(color1[1]); bw.Write(color1[2]); bw.Write((byte)0);
            bw.Write(color2[0]); bw.Write(color2[1]); bw.Write(color2[2]); bw.Write((byte)0);
            bw.Write((int)bHighlighting);
            bw.Write((int)0); // transparent
            bw.Write((uint)lineSize);
            bw.Write((uint)0); // r1
            bw.Write((uint)0); // r2
            // logfont 56 bytes
            bw.Write((int)32); // height
            bw.Write((int)0); // width
            bw.Write((int)0); // escapement
            bw.Write((int)0); // orientation
            bw.Write((int)400); // weight
            bw.Write((byte)0); // italic
            bw.Write((byte)0); // underline
            bw.Write((byte)0); // strikeout
            bw.Write((byte)128); // charset (ShiftJIS)
            bw.Write((byte)0); // outprecision
            bw.Write((byte)0); // clipprecision
            bw.Write((byte)0); // quality
            bw.Write((byte)0); // pitch
            byte[] faceName = new byte[28];
            Encoding.ASCII.GetBytes("Arial").CopyTo(faceName, 0);
            bw.Write(faceName);
            
            bw.Write((uint)0); // bReserved3
            bw.Write((long)0); // Time (8 bytes)
            bw.Write((int)1); // bVisible
            bw.Write((uint)0x0FF83F); // dwReserved4
            for (int i = 0; i < 10; i++) bw.Write((int)0); // lReserved (40 bytes)
        }

        private static void WriteOiAnText(BinaryWriter bw, string text, uint creationScale = 240)
        {
            byte[] textBytes = Encoding.GetEncoding(932).GetBytes(text);
            int textLen = textBytes.Length;
            int structSize = 16 + textLen + 1; // 16 + text + null
            if (structSize % 4 != 0) structSize += 4 - (structSize % 4); // align

            bw.Write((int)6); // WangDataType.NamedBlock
            bw.Write((int)(12 + structSize)); // Size = header(12) + structSize

            byte[] blockName = new byte[8];
            Encoding.ASCII.GetBytes("OiAnText").CopyTo(blockName, 0);
            bw.Write(blockName);
            bw.Write((int)structSize);

            bw.Write((int)0); // orientation
            bw.Write((uint)1000); // reserved1
            bw.Write((uint)creationScale); // creationScale
            bw.Write((uint)(textLen + 1)); // length
            bw.Write(textBytes);
            bw.Write((byte)0); // null terminator
            
            int written = 16 + textLen + 1;
            while (written < structSize) { bw.Write((byte)0); written++; }
        }

        private static void WriteOiAnoDatPoints(BinaryWriter bw, int[] points)
        {
            int maxPoints = points.Length / 2;
            int structSize = 8 + maxPoints * 2 * 4; // 8 bytes header + coords
            if (structSize % 4 != 0) structSize += 4 - (structSize % 4);

            bw.Write((int)6); // NamedBlock
            bw.Write((int)(12 + structSize));
            
            byte[] blockName = new byte[8];
            Encoding.ASCII.GetBytes("OiAnoDat").CopyTo(blockName, 0);
            bw.Write(blockName);
            bw.Write((int)structSize);

            bw.Write((int)maxPoints);
            bw.Write((int)maxPoints);
            foreach(var p in points) { bw.Write((int)p); }

            int written = 8 + maxPoints * 8;
            while (written < structSize) { bw.Write((byte)0); written++; }
        }
    }
}
