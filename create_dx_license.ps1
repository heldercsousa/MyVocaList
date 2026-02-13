$dir = [System.Environment]::GetFolderPath('ApplicationData') + '\DevExpress'
New-Item -ItemType Directory -Force $dir | Out-Null
$file = Join-Path $dir 'DevExpress_License.txt'
[System.IO.File]::WriteAllText($file, 'rFeVzjBfcglufmQHIR6Fk4ckHTKjfmTccij2owmRAYPjtsxdqE')
Write-Host "Created: $file"
