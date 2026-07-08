param(
    [string]$pdfPath
)

Add-Type -Path "bin\Debug\net8.0\PdfSharp.dll"

$doc = [PdfSharp.Pdf.IO.PdfReader]::Open($pdfPath, [PdfSharp.Pdf.IO.PdfDocumentOpenMode]::ReadOnly)
Write-Host "Page Count: $($doc.PageCount)"
$width = $doc.Pages[0].Width.Point
$height = $doc.Pages[0].Height.Point
Write-Host "Page 1 Size: ${width} x ${height}"

$doc.Dispose()
