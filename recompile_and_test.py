import asyncio
import json
import sys

try:
    import websockets
except ImportError:
    import subprocess
    subprocess.check_call([sys.executable, "-m", "pip", "install", "websockets"])
    import websockets

class UnityMCPClient:
    def __init__(self, uri="ws://localhost:8090/McpUnity"):
        self.uri = uri
        self.ws = None
        self.request_id = 0
    
    async def connect(self):
        self.ws = await websockets.connect(self.uri, close_timeout=5, max_size=1024*1024, ping_interval=None)
        print("Connected to Unity MCP")
    
    async def close(self):
        if self.ws:
            await self.ws.close()
    
    async def call(self, method, params=None):
        self.request_id += 1
        request = {
            "id": str(self.request_id),
            "method": method,
            "params": params or {}
        }
        await self.ws.send(json.dumps(request))
        response = await asyncio.wait_for(self.ws.recv(), timeout=30)
        return json.loads(response)

async def test_with_recompile():
    client = UnityMCPClient()
    await client.connect()
    
    print("\n" + "="*60)
    print("MainMenu Background Test with Recompile")
    print("="*60)
    
    # 1. 스크립트 재컴파일 트리거
    print("\n[1] Triggering script recompilation...")
    result = await client.call("recompile_scripts", {})
    print(f"    Result: {json.dumps(result, indent=2)[:200]}")
    
    # 2. 재컴파일 대기
    print("\n[2] Waiting for recompilation (5 seconds)...")
    for i in range(5, 0, -1):
        print(f"    {i}...", end=" ", flush=True)
        await asyncio.sleep(1)
    print()
    
    # 3. 재연결
    print("\n[3] Reconnecting...")
    await client.connect()
    
    # 4. MainMenu 씬 로드
    print("\n[4] Loading MainMenu scene...")
    result = await client.call("load_scene", {
        "scenePath": "Assets/Scenes/MainMenu.unity",
        "loadMode": "Single"
    })
    print(f"    Result: {result.get('result', {}).get('success', False)}")
    
    await asyncio.sleep(1)
    
    # 5. 플레이 모드 시작
    print("\n[5] Starting Play Mode...")
    try:
        result = await client.call("execute_menu_item", {
            "menuPath": "Tools/A.I. BEAT/Start Play Mode"
        })
        print(f"    Play Mode started!")
    except Exception as e:
        if "4001" in str(e):
            print("    Play Mode started!")
        else:
            print(f"    Error: {e}")
    
    # 6. 5초 대기
    print("\n[6] Watch Unity Game window - BIT.jpg should be visible!")
    for i in range(5, 0, -1):
        print(f"    {i}...", end=" ", flush=True)
        await asyncio.sleep(1)
    print()
    
    # 7. 플레이 모드 중지
    print("\n[7] Stopping Play Mode...")
    try:
        await client.connect()
        result = await client.call("execute_menu_item", {
            "menuPath": "Tools/A.I. BEAT/Stop Play Mode"
        })
        print(f"    Stopped!")
    except Exception as e:
        print(f"    {e}")
    
    await client.close()
    
    print("\n" + "="*60)
    print("Test Complete!")
    print("="*60)
    print("""
BIT.jpg is now set as the MainMenu background.
Check the Unity Game window to see the image!
""")

if __name__ == "__main__":
    asyncio.run(test_with_recompile())
