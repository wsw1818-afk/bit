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

async def check_main_menu():
    client = UnityMCPClient()
    await client.connect()
    
    print("\n" + "="*60)
    print("Main Menu Background Image Check")
    print("="*60)
    
    # 1. MainMenu 씬 로드
    print("\n[1] Loading MainMenu scene...")
    result = await client.call("load_scene", {
        "scenePath": "Assets/Scenes/MainMenu.unity",
        "loadMode": "Single"
    })
    print(f"    Result: {result.get('result', {}).get('success', False)}")
    
    await asyncio.sleep(1)
    
    # 2. 씬 정보 확인
    print("\n[2] Scene info...")
    result = await client.call("get_scene_info")
    if "result" in result:
        scene = result["result"].get("activeScene", {})
        print(f"    Scene: {scene.get('name')}")
        print(f"    Root Objects: {scene.get('rootCount')}")
    
    # 3. 플레이 모드 시작
    print("\n[3] Starting Play Mode to see MainMenu...")
    try:
        result = await client.call("execute_menu_item", {
            "menuPath": "Tools/A.I. BEAT/Start Play Mode"
        })
        print(f"    Play Mode started!")
    except Exception as e:
        if "4001" in str(e) or "Play mode" in str(e):
            print("    Play Mode started! (Connection closed as expected)")
        else:
            print(f"    Error: {e}")
    
    # 4. 5초 대기
    print("\n[4] Watch Unity Game window for 5 seconds...")
    for i in range(5, 0, -1):
        print(f"    {i}...", end=" ", flush=True)
        await asyncio.sleep(1)
    print()
    
    # 5. 플레이 모드 중지
    print("\n[5] Stopping Play Mode...")
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
    print("Check Complete!")
    print("="*60)
    print("""
If you see the custom background image in Unity Game window,
the MainMenuBG.jpg is working correctly!

If you see the procedural cyberpunk background,
add your image to:
  My project/Assets/Resources/UI/MainMenuBG.jpg
""")

if __name__ == "__main__":
    asyncio.run(check_main_menu())
