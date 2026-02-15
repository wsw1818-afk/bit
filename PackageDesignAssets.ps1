$sourcePath = "h:\Claude_work\bit\My project\Assets\Resources\AIBeat_Design"
$destinationPath = "h:\Claude_work\bit\AIBeat_Design_Assets.zip"

if (Test-Path $sourcePath) {
    Compress-Archive -Path "$sourcePath\*" -DestinationPath $destinationPath -Force
    Write-Host "Assets packaged successfully to $destinationPath"
} else {
    Write-Warning "Source path $sourcePath does not exist. Please play the game in Unity Editor once to generate assets."
}
