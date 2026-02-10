# Unity Play Mode 실행 스크립트
$unityPath = "C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Unity.exe"
$projectPath = "H:\Claude_work\bit\My project"

Write-Host "========================================" 
Write-Host "  Unity A.I. BEAT 게임 실행기"
Write-Host "========================================"
Write-Host ""

# Unity가 실행 중인지 확인
$unityProcess = Get-Process | Where-Object { $_.ProcessName -eq "Unity" }

if ($unityProcess) {
    Write-Host "✅ Unity가 이미 실행 중입니다."
    Write-Host ""
    Write-Host "Unity 에디터에서 다음 단계를 따라주세요:"
    Write-Host ""
    Write-Host "  1. Unity 에디터 창을 활성화하세요"
    Write-Host "  2. 상단 메뉴에서 'AIBeat' 클릭"
    Write-Host "  3. 'Enter Play Mode' 선택"
    Write-Host "     또는"
    Write-Host "  4. Ctrl+P 누르기 (Play Mode 단축키)"
    Write-Host ""
    Write-Host "또는"
    Write-Host ""
    Write-Host "  1. 'Scenes' 폴더에서 'Gameplay.unity' 더블클릭"
    Write-Host "  2. 상단의 ▶️ (Play) 버튼 클릭"
    Write-Host ""
} else {
    Write-Host "🚀 Unity를 시작합니다..."
    Start-Process -FilePath $unityPath -ArgumentList "-projectPath", "`"$projectPath`""
    Write-Host ""
    Write-Host "Unity가 로드될 때까지 기다리세요 (약 30초)"
    Write-Host "그 후 위의 단계를 따라 Play Mode를 실행하세요."
}

Write-Host ""
Write-Host "스크린샷 찍기:"
Write-Host "  - F12 키 누르기"
Write-Host "  - 또는 Window > A.I. BEAT > Screen Capture"
Write-Host ""
Write-Host "스크린샷 저장 위치: My project/Screenshots/"
Write-Host ""
Read-Host "아무 키나 눌러 종료..."