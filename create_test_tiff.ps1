Add-Type -AssemblyName System.Drawing
$bmp = New-Object System.Drawing.Bitmap(800, 600)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.Clear([System.Drawing.Color]::White)
$font = New-Object System.Drawing.Font('Arial', 24)
$brush = [System.Drawing.Brushes]::Black
$g.DrawString('Teniko Test Image - No Annotations', $font, $brush, 50, 50)
$bmp.Save('test_image.tif', [System.Drawing.Imaging.ImageFormat]::Tiff)
$g.Dispose()
$bmp.Dispose()
Write-Host "test_image.tif was created successfully."
