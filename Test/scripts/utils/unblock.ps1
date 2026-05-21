Get-ChildItem -Path 'D:\Scuola\5IA\INFO\ClaudeCode\Test\frontend\bin\Debug\net10.0' -Filter '*.dll' | ForEach-Object {
    $dest = $_.FullName
    Copy-Item -Path $dest -Destination "$dest.tmp" -Force
    Move-Item -Path "$dest.tmp" -Destination $dest -Force
}
Write-Host "Done"