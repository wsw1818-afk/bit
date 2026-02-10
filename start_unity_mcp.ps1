$unityPath = "C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Unity.exe"
$projectPath = "H:\Claude_work\bit\My project"
$logPath = "H:\Claude_work\bit\unity_mcp_log.txt"

Write-Host "Unity MCP Server 시작 중..."
Write-Host "Unity 경로: $unityPath"
Write-Host "프로젝트 경로: $projectPath"

# Unity 실행 (GUI 모드)
Start-Process -FilePath $unityPath -ArgumentList "-projectPath", "`"$projectPath`"" -WindowStyle Normal

Write-Host "Unity가 시작되었습니다. MCP 서버가 활성화될 때까지 기다려주세요..."
Write-Host ""
Write-Host "=== MCP 서버 상태 확인 ==="
Write-Host "포트 8090이 열리면 자동으로 연결됩니다."

# 10초 대기 후 포트 확인
Start-Sleep -Seconds 10

$portTest = Test-NetConnection -ComputerName localhost -Port 8090 -WarningAction SilentlyContinue
if ($portTest.TcpTestSucceeded) {
    Write-Host "✅ MCP 서버가 포트 8090에서 실행 중입니다!"
} else {
    Write-Host "⚠️ MCP 서버가 아직 시작되지 않았습니다."
    Write-Host "Unity 에디터가 완전히 로드될 때까지 기다려주세요."
}