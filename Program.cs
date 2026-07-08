using System;
using System.IO;

namespace Teniko
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("TIFF to PDF (Wang Annotation) Converter");
                Console.WriteLine("=======================================\n");

                if (args.Length == 0)
                {
                    Console.WriteLine("使用方法 (Usage): teniko <入力TIFFパス> [出力PDFパス]");
                    Console.WriteLine("変換したいTIFFファイルをこのウィンドウにドラッグ＆ドロップして [Enter] を押してください:");
                    Console.WriteLine("(Please enter or drag-and-drop the input TIFF file path, then press Enter)");
                    
                    var input = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(input))
                    {
                        Console.WriteLine("\nエラー: 入力ファイルが見つかりません。");
                        return;
                    }
                    args = new string[] { input.Trim('\"', ' ') };
                }

                string inputTiff = args[0];
                string outputPdf = args.Length > 1 ? args[1] : Path.ChangeExtension(inputTiff, ".pdf");

                if (!File.Exists(inputTiff))
                {
                    Console.WriteLine($"エラー: 入力ファイルが見つかりません。パス: {inputTiff}");
                    return;
                }

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

                Console.WriteLine("\n変換が正常に完了しました！ (Conversion completed successfully!)");
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n[変換エラー] 処理中に致命的なエラーが発生しました。処理を中断します。");
                Console.WriteLine($"詳細: {ex.Message}");
            }
            finally
            {
                WaitForKey();
            }
        }

        static void WaitForKey()
        {
            try
            {
                Console.WriteLine("\n==================================================");
                Console.WriteLine("処理が完了しました。");
                Console.WriteLine("右上の [X] ボタンでウィンドウを閉じるか、");
                Console.WriteLine("[Enter] キーを押すとアプリケーションを終了します。");
                Console.WriteLine("==================================================");
                
                var input = Console.ReadLine();
                if (input == null)
                {
                    System.Threading.Thread.Sleep(4000);
                }
            }
            catch
            {
                System.Threading.Thread.Sleep(4000);
            }
            finally
            {
                Console.WriteLine("\n終了しています... (Exiting...)");
                System.Threading.Thread.Sleep(2000);
            }
        }
    }
}
