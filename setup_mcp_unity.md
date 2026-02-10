# Unity MCP 연결 완전 가이드

## 현재 문제
`com.gamelovers.mcp-unity` 패키지는 HTTP MCP 서버가 아닙니다.

## 해결 방법 3가지

### 방법 1: McpUnity 패키지 설치 (권장)

#### 1. Unity Package Manager에서 설치
```
Window → Package Manager → + → Add package from git URL

https://github.com/justinpbarnett/unity-mcp.git
```

#### 2. 또는 manifest.json 직접 수정
`My project/Packages/manifest.json`에 추가:
```json
{
  "dependencies": {
    "com.gamelovers.mcp-unity": "...",
    + "jp.justinpbarrett.unity-mcp": "https://github.com/justinpbarnett/unity-mcp.git"
  }
}
```

#### 3. Unity 재시작

#### 4. MCP 서버 시작
```
Window → Unity MCP → Start Server
```

---

### 방법 2: 직접 MCP 서버 스크립트 작성

#### 1. Unity 에디터 스크립트 생성
`Assets/Scripts/Editor/McpServer.cs`:

```csharp
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Net;
using System.Text;

public class McpUnityServer : EditorWindow
{
    private HttpListener listener;
    private bool isRunning = false;
    private int port = 8090;

    [MenuItem("Window/MCP Unity Server")]
    public static void ShowWindow()
    {
        GetWindow<McpUnityServer>("MCP Server");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("MCP Server Status", EditorStyles.boldLabel);
        
        port = EditorGUILayout.IntField("Port", port);
        
        if (!isRunning)
        {
            if (GUILayout.Button("Start Server", GUILayout.Height(30)))
            {
                StartServer();
            }
        }
        else
        {
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Stop Server", GUILayout.Height(30)))
            {
                StopServer();
            }
            GUI.backgroundColor = Color.white;
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField($"Status: {(isRunning ? "Running" : "Stopped")}");
    }

    private void StartServer()
    {
        listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{port}/");
        listener.Start();
        isRunning = true;
        Debug.Log($"[MCP] Server started on port {port}");
        
        // 비동기 요청 처리
        listener.BeginGetContext(OnRequest, null);
    }

    private void StopServer()
    {
        listener?.Stop();
        listener = null;
        isRunning = false;
        Debug.Log("[MCP] Server stopped");
    }

    private void OnRequest(IAsyncResult result)
    {
        if (!isRunning) return;
        
        var context = listener.EndGetContext(result);
        var request = context.Request;
        var response = context.Response;

        // MCP JSON-RPC 요청 처리
        string responseString = ProcessMcpRequest(request);
        
        byte[] buffer = Encoding.UTF8.GetBytes(responseString);
        response.ContentLength64 = buffer.Length;
        response.OutputStream.Write(buffer, 0, buffer.Length);
        response.OutputStream.Close();

        // 다음 요청 대기
        listener.BeginGetContext(OnRequest, null);
    }

    private string ProcessMcpRequest(HttpListenerRequest request)
    {
        // 기본 MCP 응답
        return @"{
            ""jsonrpc"": ""2.0"",
            ""id"": 1,
            ""result"": {
                ""protocolVersion"": ""2024-11-05"",
                ""capabilities"": {},
                ""serverInfo"": {
                    ""name"": ""unity-mcp"",
                    ""version"": ""1.0.0""
                }
            }
        }";
    }

    private void OnDestroy()
    {
        StopServer();
    }
}
#endif
```

#### 2. Unity에서 서버 시작
```
Window → MCP Unity Server → Start Server
```

---

### 방법 3: Claude Desktop 설정 (Claude Code 연동)

#### 1. Claude Desktop 설정 파일 수정
`%APPDATA%\Claude\claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "unity": {
      "command": "node",
      "args": ["path/to/unity-mcp-server/index.js"],
      "env": {
        "UNITY_PROJECT_PATH": "H:\\Claude_work\\bit\\My project"
      }
    }
  }
}
```

#### 2. Unity MCP 서버 패키지 설치
npm을 통해 별도 MCP 서버 설치 필요

---

## 테스트 방법

### 1. 포트 확인
```bash
# PowerShell
Test-NetConnection -ComputerName localhost -Port 8090

# 또는 Python
py -c "import socket; s=socket.socket(); print('OPEN' if s.connect_ex(('localhost',8090))==0 else 'CLOSED')"
```

### 2. MCP 요청 테스트
```bash
curl -X POST http://localhost:8090/mcp \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{}}'
```

---

## 현재 상황 정리

### ✅ 확인됨
- Unity 프로젝트: 정상
- `com.gamelovers.mcp-unity`: 설치됨 (하지만 HTTP 서버 아님)
- 게임 구조: 완벽

### ❌ 문제
- MCP 서버: 미실행
- 포트 8090: 닫힘

### 🎯 다음 단계
1. **권장**: 방법 1 (McpUnity 패키지) 설치
2. Unity에서 `Window → Unity MCP → Start Server` 실행
3. 포트 8090 확인
4. 게임 테스트
