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

async def analyze_all_screens():
    client = UnityMCPClient()
    await client.connect()
    
    print("\n" + "="*70)
    print("BIT Rhythm Game - Full Screen Analysis & Redesign")
    print("="*70)
    
    # ============================================
    # 1. MAIN MENU 분석
    # ============================================
    print("\n" + "="*70)
    print("[1] MAIN MENU SCREEN ANALYSIS")
    print("="*70)
    
    result = await client.call("load_scene", {
        "scenePath": "Assets/Scenes/MainMenu.unity",
        "loadMode": "Single"
    })
    print(f"    Scene Loaded: {result.get('result', {}).get('success', False)}")
    await asyncio.sleep(0.5)
    
    # 씬 정보
    result = await client.call("get_scene_info")
    if "result" in result:
        scene = result["result"].get("activeScene", {})
        print(f"    Scene: {scene.get('name')}")
        print(f"    Root Objects: {scene.get('rootCount')}")
    
    # 루트 오브젝트 분석
    result = await client.call("get_game_object", {"objectPath": "/"})
    if "result" in result and "children" in result["result"]:
        print("\n    Root Objects:")
        for child in result["result"]["children"]:
            name = child.get("name", "unknown")
            print(f"      - {name}")
    
    # ============================================
    # 2. SONG SELECT 분석
    # ============================================
    print("\n" + "="*70)
    print("[2] SONG SELECT SCREEN ANALYSIS")
    print("="*70)
    
    result = await client.call("load_scene", {
        "scenePath": "Assets/Scenes/SongSelect.unity",
        "loadMode": "Single"
    })
    print(f"    Scene Loaded: {result.get('result', {}).get('success', False)}")
    await asyncio.sleep(0.5)
    
    result = await client.call("get_scene_info")
    if "result" in result:
        scene = result["result"].get("activeScene", {})
        print(f"    Scene: {scene.get('name')}")
        print(f"    Root Objects: {scene.get('rootCount')}")
    
    result = await client.call("get_game_object", {"objectPath": "/"})
    if "result" in result and "children" in result["result"]:
        print("\n    Root Objects:")
        for child in result["result"]["children"]:
            name = child.get("name", "unknown")
            print(f"      - {name}")
    
    # ============================================
    # 3. GAMEPLAY 분석
    # ============================================
    print("\n" + "="*70)
    print("[3] GAMEPLAY SCREEN ANALYSIS")
    print("="*70)
    
    result = await client.call("load_scene", {
        "scenePath": "Assets/Scenes/Gameplay.unity",
        "loadMode": "Single"
    })
    print(f"    Scene Loaded: {result.get('result', {}).get('success', False)}")
    await asyncio.sleep(0.5)
    
    result = await client.call("get_scene_info")
    if "result" in result:
        scene = result["result"].get("activeScene", {})
        print(f"    Scene: {scene.get('name')}")
        print(f"    Root Objects: {scene.get('rootCount')}")
    
    result = await client.call("get_game_object", {"objectPath": "/"})
    if "result" in result and "children" in result["result"]:
        print("\n    Root Objects:")
        for child in result["result"]["children"]:
            name = child.get("name", "unknown")
            print(f"      - {name}")
    
    # ============================================
    # 4. 리소스 분석
    # ============================================
    print("\n" + "="*70)
    print("[4] AVAILABLE IMAGE RESOURCES")
    print("="*70)
    
    print("\n    UI Images (Resources/UI/):")
    print("      - BIT.jpg")
    print("      - GameplayBG.jpg")
    print("      - SongSelectBG.jpg")
    
    print("\n    Skin Images (Resources/Skins/NanoBanana/):")
    print("      - Background.png")
    print("      - TapNote.png")
    print("      - LongNoteBody.png")
    print("      - ScratchNote.png")
    print("      - JudgementLine.png")
    print("      - LaneKeyBg.png")
    print("      - HitEffect.png")
    
    # ============================================
    # 5. 리디자인 제안
    # ============================================
    print("\n" + "="*70)
    print("[5] REDESIGN RECOMMENDATIONS")
    print("="*70)
    
    print("""
    CURRENT STATE:
    ┌─────────────────────────────────────────────────────────────────┐
    │ MAIN MENU                                                       │
    │   - Background: BIT.jpg (just changed)                         │
    │   - Title: "A.I. BEAT" with neon effect                        │
    │   - Buttons: Play, Library, Settings, Exit                     │
    │   - Equalizer bars animation                                    │
    └─────────────────────────────────────────────────────────────────┘
    
    ┌─────────────────────────────────────────────────────────────────┐
    │ SONG SELECT                                                     │
    │   - Background: SongSelectBG.jpg or procedural                 │
    │   - Song list with difficulty selection                        │
    │   - Back button                                                 │
    └─────────────────────────────────────────────────────────────────┘
    
    ┌─────────────────────────────────────────────────────────────────┐
    │ GAMEPLAY                                                        │
    │   - Background: GameplayBG.jpg or procedural                   │
    │   - 4 lanes + 1 scratch lane                                    │
    │   - Notes: Tap, Long, Scratch                                   │
    │   - Score, Combo, Judgement display                            │
    └─────────────────────────────────────────────────────────────────┘
    
    REDESIGN OPTIONS:
    1. Use consistent background theme across all screens
    2. Apply BIT.jpg style to SongSelect and Gameplay
    3. Create custom note skins
    4. Update color palette
    """)
    
    await client.close()
    
    print("\n" + "="*70)
    print("Analysis Complete!")
    print("="*70)

if __name__ == "__main__":
    asyncio.run(analyze_all_screens())
