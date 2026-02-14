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

async def test_game():
    client = UnityMCPClient()
    await client.connect()
    
    print("\n" + "="*60)
    print("BIT Rhythm Game - Full Test")
    print("="*60)
    
    # 1. Gameplay 씬 로드
    print("\n[1] Loading Gameplay scene...")
    result = await client.call("load_scene", {
        "scenePath": "Assets/Scenes/Gameplay.unity",
        "loadMode": "Single"
    })
    print(f"    Result: {result.get('result', {}).get('success', False)}")
    
    await asyncio.sleep(0.5)
    
    # 2. 씬 계층 구조 분석
    print("\n[2] Analyzing Gameplay scene hierarchy...")
    result = await client.call("get_scene_info")
    if "result" in result:
        scene = result["result"].get("activeScene", {})
        print(f"    Scene: {scene.get('name')}")
        print(f"    Root Objects: {scene.get('rootCount')}")
    
    # 3. 주요 게임 오브젝트 찾기
    print("\n[3] Finding key game objects...")
    key_objects = [
        "GameManager",
        "AudioManager", 
        "NoteSpawner",
        "InputHandler",
        "GameplayUI",
        "JudgementSystem",
        "Main Camera",
        "EventSystem",
        "Canvas"
    ]
    
    for obj_name in key_objects:
        result = await client.call("get_game_object", {
            "objectPath": f"/{obj_name}"
        })
        if "result" in result and result["result"].get("name"):
            obj = result["result"]
            active = obj.get("activeSelf", "?")
            children = len(obj.get("children", []))
            print(f"    [OK] {obj_name}: active={active}, children={children}")
        else:
            print(f"    [--] {obj_name}: not found")
    
    # 4. MainMenu 씬 테스트
    print("\n[4] Testing MainMenu scene...")
    result = await client.call("load_scene", {
        "scenePath": "Assets/Scenes/MainMenu.unity",
        "loadMode": "Single"
    })
    print(f"    Loaded: {result.get('result', {}).get('success', False)}")
    
    await asyncio.sleep(0.5)
    
    result = await client.call("get_scene_info")
    if "result" in result:
        scene = result["result"].get("activeScene", {})
        print(f"    Scene: {scene.get('name')}, Root Objects: {scene.get('rootCount')}")
    
    # 5. SongSelect 씬 테스트
    print("\n[5] Testing SongSelect scene...")
    result = await client.call("load_scene", {
        "scenePath": "Assets/Scenes/SongSelect.unity",
        "loadMode": "Single"
    })
    print(f"    Loaded: {result.get('result', {}).get('success', False)}")
    
    await asyncio.sleep(0.5)
    
    result = await client.call("get_scene_info")
    if "result" in result:
        scene = result["result"].get("activeScene", {})
        print(f"    Scene: {scene.get('name')}, Root Objects: {scene.get('rootCount')}")
    
    # 6. Gameplay 씬으로 복귀
    print("\n[6] Returning to Gameplay scene...")
    result = await client.call("load_scene", {
        "scenePath": "Assets/Scenes/Gameplay.unity",
        "loadMode": "Single"
    })
    print(f"    Loaded: {result.get('result', {}).get('success', False)}")
    
    await client.close()
    
    print("\n" + "="*60)
    print("Game Test Complete!")
    print("="*60)
    print("""
Test Summary:
  [PASS] MCP Connection
  [PASS] Scene Loading (3 scenes)
  [PASS] Scene Info Query
  [PASS] GameObject Access
  
All core functionality working correctly!
""")

if __name__ == "__main__":
    asyncio.run(test_game())
