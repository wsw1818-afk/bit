# Unity MCP 메뉴 확인 가이드

## 현재 상태
- ✅ Unity 실행 중 (3개 프로세스)
- ❌ MCP 서버: 미실행 (포트 8090 닫힘)

## Unity 에디터에서 확인할 메뉴

### 1. 상단 메뉴 확인
Unity 에디터 상단 메뉴에서 다음을 찾으세요:

```
Window
  ├── Unity MCP        <-- 이 메뉴가 있나요?
  ├── MCP
  └── AI BEAT
        └── MCP Server

Tools
  ├── Unity MCP        <-- 이 메뉴가 있나요?
  └── MCP

AIBeat
  └── MCP Server       <-- 이 메뉴가 있나요?
```

### 2. Package Manager 확인
```
Window → Package Manager
```
왼쪽 패키지 목록에서 확인:
- `Unity MCP`
- `McpUnity`
- `jp.justinpbarrett.unity-mcp`

### 3. Project Settings 확인
```
Edit → Project Settings
```
왼쪽 목록에서 확인:
- `MCP`
- `Unity MCP`

### 4. 콘솔 로그 확인
```
Window → General → Console
```
로그에서 다음 키워드 검색:
- `MCP`
- `mcp-unity`
- `Unity MCP`

## MCP 서버 시작 방법

메뉴를 찾으면:
1. 해당 메뉴 클릭
2. `Start Server` 또는 `Enable` 버튼 클릭
3. 포트 8090이 열리는지 확인

## 패키지가 없다면

Package Manager에서 직접 설치:
1. Window → Package Manager
2. `+` 버튼 클릭 → `Add package from git URL`
3. 입력: `https://github.com/justinpbarnett/unity-mcp.git`
4. Install 클릭
5. Unity 재시작

## 설치 후 확인

설치가 완료되면 다음 명령으로 테스트:
```bash
# PowerShell
Test-NetConnection localhost -Port 8090

# 또는 브라우저
http://localhost:8090/
```

## 도움이 필요하시면

Unity 에디터에서 보이는 메뉴를 캡처해서 보여주세요:
1. 상단 메뉴 (Window, Tools, AIBeat)
2. Package Manager 패키지 목록
3. Console의 MCP 관련 로그
