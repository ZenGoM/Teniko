using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Teniko.Models;
using WangTagReadingLibrary;

namespace Teniko
{
    public class WangAnnotationParser
    {
        // 932 is the code page for Shift-JIS (Windows-31J)
        internal static readonly Encoding JapaneseEncoding;

        static WangAnnotationParser()
        {
            // Register encoding provider for .NET Core / .NET 5+ to support Shift-JIS
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            JapaneseEncoding = Encoding.GetEncoding(932);
        }

        /// <summary>
        /// Parses the byte array of TIFF Tag 32932 into a list of WangAnnotations.
        /// </summary>
        /// <param name="data">Binary data from Tag 32932</param>
        /// <returns>List of parsed annotations</returns>
        public List<WangAnnotation> Parse(byte[] data)
        {
            var annotations = new List<WangAnnotation>();
            if (data == null || data.Length == 0) return annotations;
            
            try 
            {
                var handler = new WangHandler(annotations);
                WangAnnotationsReader.Read(handler, data);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Debug] Error parsing Wang annotation: {ex.Message}");
            }
            return annotations;
        }

        private class WangHandler : IWangAnnotationHandler
        {
            private readonly List<WangAnnotation> _annotations;

            public WangHandler(List<WangAnnotation> annotations)
            {
                _annotations = annotations;
            }

            public void AddLineAnnot(int srcXPixels, int srcYPixels, int dstXPixels, int dstYPixels, byte[] colorComponents, int borderWidthPixels)
            {
                _annotations.Add(new LineAnnotation 
                {
                    X = Math.Min(srcXPixels, dstXPixels),
                    Y = Math.Min(srcYPixels, dstYPixels),
                    Width = Math.Abs(dstXPixels - srcXPixels),
                    Height = Math.Abs(dstYPixels - srcYPixels),
                    Color = new int[] { colorComponents[0], colorComponents[1], colorComponents[2] },
                    EndX = dstXPixels,
                    EndY = dstYPixels,
                    Thickness = borderWidthPixels
                });
            }

            public void AddFreeHandHighligtherAnnot(int[] pointsCoordinates, byte[] colorComponents, int borderWidthPixels)
            {
                if (pointsCoordinates == null || pointsCoordinates.Length < 2) return;
                int minX = int.MaxValue, minY = int.MaxValue;
                int maxX = int.MinValue, maxY = int.MinValue;
                for (int i = 0; i < pointsCoordinates.Length; i += 2)
                {
                    if (pointsCoordinates[i] < minX) minX = pointsCoordinates[i];
                    if (pointsCoordinates[i] > maxX) maxX = pointsCoordinates[i];
                    if (pointsCoordinates[i+1] < minY) minY = pointsCoordinates[i+1];
                    if (pointsCoordinates[i+1] > maxY) maxY = pointsCoordinates[i+1];
                }

                _annotations.Add(new HighlightAnnotation
                {
                    X = minX,
                    Y = minY,
                    Width = maxX - minX,
                    Height = maxY - minY,
                    Color = new int[] { colorComponents[0], colorComponents[1], colorComponents[2] }
                });
            }

            public void AddTextAnnot(int leftPixels, int topPixels, int widthPixels, int heightPixels, string text, bool italic, bool underline, string fontName, int fontSize, int rotation, byte[] colorComponents)
            {
                _annotations.Add(new TextAnnotation
                {
                    X = leftPixels,
                    Y = topPixels,
                    Width = widthPixels,
                    Height = heightPixels,
                    Text = text,
                    Color = new int[] { colorComponents[0], colorComponents[1], colorComponents[2] }, 
                    FontSize = fontSize
                });
            }

            public void AddLinkAnnot(int leftPixels, int topPixels, int widthPixels, int heightPixels, string text, string link) { }

            public void AddStickyNoteAnnot(int leftPixels, int topPixels, int widthPixels, int heightPixels, string text, byte[] foreColor, byte[] fillColor)
            {
                _annotations.Add(new NoteAnnotation
                {
                    X = leftPixels,
                    Y = topPixels,
                    Width = widthPixels,
                    Height = heightPixels,
                    Text = text,
                    Color = new int[] { foreColor[0], foreColor[1], foreColor[2] },
                    BackgroundColor = new int[] { fillColor[0], fillColor[1], fillColor[2] }
                });
            }

            public void AddRectangleAnnot(int leftPixels, int topPixels, int widthPixels, int heightPixels, byte[] borderColor, byte[] backColor, bool fill, int borderWidthPixels)
            {
                _annotations.Add(new RectangleAnnotation
                {
                    X = leftPixels,
                    Y = topPixels,
                    Width = widthPixels,
                    Height = heightPixels,
                    Color = new int[] { borderColor[0], borderColor[1], borderColor[2] },
                    IsFilled = fill,
                    Thickness = borderWidthPixels
                });
            }

            public void AddPolygonAnnot(int[] pointsCoordinates, byte[] borderColor, byte[] backColor, bool fill, int borderWidthPixels) { }

            public void AddFreeHandAnnot(int[] pointsCoordinates, byte[] colorComponents, int borderWidthPixels) { }

            public void AddEmbeddedImageAnnot(int leftPixels, int topPixels, int widthPixels, int heightPixels, IntPtr dib) { }

            public void AddRubberStampAnnot(int leftPixels, int topPixels, int widthPixels, int heightPixels, string text, int rotation, byte[] colorComponents)
            {
                _annotations.Add(new TextAnnotation
                {
                    X = leftPixels,
                    Y = topPixels,
                    Width = widthPixels,
                    Height = heightPixels,
                    Text = text,
                    Color = new int[] { colorComponents[0], colorComponents[1], colorComponents[2] },
                    FontSize = 14
                });
            }
        }
    }
}
