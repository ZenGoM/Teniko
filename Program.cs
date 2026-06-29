using System;
using System.IO;

namespace Teniko
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("TIFF to PDF (Wang Annotation) Converter");
            Console.WriteLine("=======================================\n");

            if (args.Length < 2)
            {
                Console.WriteLine("使用方法: teniko <入力TIFFパス> <出力PDFパス>");
                return;
            }

            string inputTiff = args[0];
            string outputPdf = args[1];

            if (!File.Exists(inputTiff))
            {
                Console.WriteLine($"エラー: 入力ファイルが見つかりません。パス: {inputTiff}");
                return;
            }

            try
            {
                Console.WriteLine($"入力ファイル: {inputTiff}");
                Console.WriteLine($"出力ファイル: {outputPdf}");
                Console.WriteLine("処理を開始します...");

                // 1. Read TIFF and extract pages / Wang Annotations
                using var tiffReader = new TiffReader();
                var pages = tiffReader.ExtractPages(inputTiff);
                Console.WriteLine($"{pages.Count} ページのTIFFデータを読み込みました。");

                // 2. Generate PDF and draw annotations
                var pdfGenerator = new PdfGenerator();
                pdfGenerator.GeneratePdf(inputTiff, outputPdf, pages);

                Console.WriteLine("\n変換が正常に完了しました！");
            }
            catch (Exception ex)
            {
                // 仕様: エラー時はファイル全体の変換エラーとして直ちに処理を中断
                Console.WriteLine("\n[変換エラー] 処理中に致命的なエラーが発生しました。処理を中断します。");
                Console.WriteLine($"詳細: {ex.Message}");
                // throw; // スタックトレースを出力する場合はコメント解除
            }
        }
    }
}
