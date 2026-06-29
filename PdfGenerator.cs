using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Annotations;
using Teniko.Models;

namespace Teniko
{
    public class PdfGenerator
    {
        public void GeneratePdf(string sourceTiff, string outputPdf, List<TiffPageInfo> pagesData)
        {
            using var document = new PdfDocument();
            var parser = new WangAnnotationParser();

            using var tiffImage = Image.FromFile(sourceTiff);
            var frameDimension = new FrameDimension(tiffImage.FrameDimensionsList[0]);
            int frameCount = tiffImage.GetFrameCount(frameDimension);
            
            for (int i = 0; i < pagesData.Count; i++)
            {
                var pageInfo = pagesData[i];
                var page = document.AddPage();
                
                // Convert TIFF pixel dimensions to PDF points (1/72 inch)
                double pointsPerInch = 72.0;
                double pdfWidth = (pageInfo.Width / (double)pageInfo.DpiX) * pointsPerInch;
                double pdfHeight = (pageInfo.Height / (double)pageInfo.DpiY) * pointsPerInch;
                
                page.Width = XUnit.FromPoint(pdfWidth);
                page.Height = XUnit.FromPoint(pdfHeight);

                using (var gfx = XGraphics.FromPdfPage(page))
                {
                    try 
                    {
                        if (i < frameCount)
                        {
                            tiffImage.SelectActiveFrame(frameDimension, i);
                            using var ms = new MemoryStream();
                            tiffImage.Save(ms, ImageFormat.Png);
                            ms.Position = 0;
                            using var image = XImage.FromStream(ms);
                            gfx.DrawImage(image, 0, 0, pdfWidth, pdfHeight);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Could not draw TIFF image for page {pageInfo.PageNumber}: {ex.Message}");
                    }

                    // Parse Wang Annotations if present
                    if (pageInfo.WangAnnotationData != null)
                    {
                        var annotations = parser.Parse(pageInfo.WangAnnotationData);
                        DrawAnnotations(page, gfx, annotations, pageInfo);
                    }
                }
            }

            document.Save(outputPdf);
        }

        private void DrawAnnotations(PdfPage pdfPage, XGraphics gfx, List<WangAnnotation> annotations, TiffPageInfo pageInfo)
        {
            double scaleX = 72.0 / pageInfo.DpiX;
            double scaleY = 72.0 / pageInfo.DpiY;

            Console.WriteLine($"[Debug] Page {pageInfo.PageNumber}: {annotations.Count} 個のアノテーションをPDF注釈として追加します。");

            foreach (var ann in annotations)
            {
                // Convert coordinates from TIFF pixels to PDF points
                double x = ann.X * scaleX;
                double y = ann.Y * scaleY;
                double width = ann.Width * scaleX;
                double height = ann.Height * scaleY;
                
                // 幅や高さが0の場合は最小サイズを確保
                if (width < 1) width = 10;
                if (height < 1) height = 10;

                double pdfTop = pdfPage.Height.Point - y;
                double pdfBottom = pdfPage.Height.Point - (y + height);

                var rect = new PdfRectangle(new XRect(x, pdfBottom, width, height));
                // Wang annotations store color in BGR format
                var pdfColor = XColor.FromArgb(255, ann.Color[2], ann.Color[1], ann.Color[0]);

                // PdfTextAnnotationをベースに作成し、Subtypeを上書きすることで様々な注釈を実現
                var pdfAnn = new PdfTextAnnotation
                {
                    Rectangle = rect,
                    Title = "Wang Annotation",
                    Color = pdfColor
                };

                switch (ann)
                {
                    case TextAnnotation textAnn:
                        pdfAnn.Elements.SetName("/Subtype", "/FreeText");
                        pdfAnn.Contents = textAnn.Text;
                        // FreeTextの背景色（/C）を削除し、透明にする
                        pdfAnn.Elements.Remove("/C");
                        
                        // アノテーションの枠線を非表示にする (/BS << /W 0 >>)
                        var bsDict = new PdfSharp.Pdf.PdfDictionary();
                        bsDict.Elements.Add("/W", new PdfSharp.Pdf.PdfInteger(0));
                        pdfAnn.Elements["/BS"] = bsDict;

                        // テキストの文字色を/DAに設定する (RGB値は0.0～1.0の範囲)
                        // Text color is BGR
                        double r = textAnn.Color[2] / 255.0;
                        double g = textAnn.Color[1] / 255.0;
                        double b = textAnn.Color[0] / 255.0;
                        // InvariantCultureを使用して小数点がカンマにならないようにする
                        string rStr = r.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
                        string gStr = g.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
                        string bStr = b.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
                        
                        // 文字の大きさを枠の高さに合わせて調整する
                        int lineCount = 1;
                        if (!string.IsNullOrEmpty(textAnn.Text))
                        {
                            lineCount = textAnn.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
                            if (lineCount == 0) lineCount = 1;
                        }
                        // 枠の高さ(height)から行数とマージンを考慮してフォントサイズを算出
                        double calculatedFontSize = (height / lineCount) * 0.8;
                        if (calculatedFontSize < 1.0) calculatedFontSize = textAnn.FontSize; // フェールセーフ

                        string fontSizeStr = calculatedFontSize.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
                        
                        Console.WriteLine($"[Debug] TextAnnotation: '{textAnn.Text}', OriginalFontSize={textAnn.FontSize}, CalculatedFontSize={calculatedFontSize}, X={x}, Y={y}, W={width}, H={height}");

                        pdfAnn.Elements.SetString("/DA", $"{rStr} {gStr} {bStr} rg /Helv {fontSizeStr} Tf");
                        break;

                    case NoteAnnotation noteAnn:
                        pdfAnn.Elements.SetName("/Subtype", "/Text");
                        pdfAnn.Icon = PdfTextAnnotationIcon.Comment;
                        pdfAnn.Contents = noteAnn.Text;
                        break;

                    case LineAnnotation lineAnn:
                        pdfAnn.Elements.SetName("/Subtype", "/Line");
                        double endX = lineAnn.EndX * scaleX;
                        double endY = lineAnn.EndY * scaleY;
                        double pdfEndY = pdfPage.Height.Point - endY;
                        // 線分の開始点と終了点を指定
                        var lineArray = new PdfSharp.Pdf.PdfArray();
                        lineArray.Elements.Add(new PdfSharp.Pdf.PdfReal(x));
                        lineArray.Elements.Add(new PdfSharp.Pdf.PdfReal(pdfTop));
                        lineArray.Elements.Add(new PdfSharp.Pdf.PdfReal(endX));
                        lineArray.Elements.Add(new PdfSharp.Pdf.PdfReal(pdfEndY));
                        pdfAnn.Elements["/L"] = lineArray;
                        pdfAnn.Contents = "[Line]";
                        break;

                    case HighlightAnnotation hlAnn:
                        pdfAnn.Elements.SetName("/Subtype", "/Highlight");
                        pdfAnn.Contents = "[Highlight]";
                        // ハイライト用の四角形座標 (QuadPoints: BL, BR, TR, TL)
                        var quadArray = new PdfSharp.Pdf.PdfArray();
                        quadArray.Elements.Add(new PdfSharp.Pdf.PdfReal(x)); quadArray.Elements.Add(new PdfSharp.Pdf.PdfReal(pdfBottom)); // 左下
                        quadArray.Elements.Add(new PdfSharp.Pdf.PdfReal(x + width)); quadArray.Elements.Add(new PdfSharp.Pdf.PdfReal(pdfBottom)); // 右下
                        quadArray.Elements.Add(new PdfSharp.Pdf.PdfReal(x + width)); quadArray.Elements.Add(new PdfSharp.Pdf.PdfReal(pdfTop)); // 右上
                        quadArray.Elements.Add(new PdfSharp.Pdf.PdfReal(x)); quadArray.Elements.Add(new PdfSharp.Pdf.PdfReal(pdfTop)); // 左上
                        pdfAnn.Elements["/QuadPoints"] = quadArray;
                        break;

                    case RectangleAnnotation rectAnn:
                        pdfAnn.Elements.SetName("/Subtype", "/Square");
                        pdfAnn.Contents = "[Rectangle]";
                        break;
                }

                // PDFページに注釈を追加
                pdfPage.Annotations.Add(pdfAnn);
            }
        }
    }
}
