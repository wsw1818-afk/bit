# Unity with MCP 패키지 설치 및 실행
$unityPath = "C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Unity.exe"
$projectPath = "H:\Claude_work\bit\My project"

Write-Host "========================================" 
Write-Host "  Unity MCP 설치 및 실행"
Write-Host "========================================"
Write-Host ""
Write-Host "Unity를 시작하면 MCP 패키지가 자동으로 설치됩니다..."
Write-Host ""

# Unity 실행
Start-Process -FilePath $unityPath -ArgumentList "-projectPath", "`"$projectPath`""

Write-Host "✅ Unity가 시작되었습니다!"
Write-Host ""
Write-Host "📋 다음 단계:"
Write-Host "   1. Unity가 완전히 로드될 때까지 기다리세요 (1-2분)"
Write-Host "   2. Package Manager에서 'Unity MCP' 설치 확인"
Write-Host "   3. 메뉴에서 'Window → Unity MCP' 클릭"
Write-Host "   4. 'Start Server' 버튼 클릭"
Write-Host ""
Write-Host "🔍 설치 확인 방법:"
Write-Host "   - 메뉴에 'Unity MCP'가 나타나면 성공!"
Write-Host "   - 포트 8090이 열리면 연결 완료!"
Write-Host ""
Read-Host "아무 키나 눌러 종료..."