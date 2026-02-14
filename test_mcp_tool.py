import asyncio
import json
import sys

try:
    import websockets
except ImportError:
    import subprocess
    subprocess.check_call([sys.executable, "-m", "pip", "install", "websockets"])
    import websockets

async def test_mcp():
    uri = "ws://localhost:8090/McpUnity"
    print(f"Connecting to: {uri}")
    
    try:
        async with websockets.connect(
            uri,
            close_timeout=5,
            max_size=1024*1024,
            ping_interval=None
        ) as ws:
            print("SUCCESS! WebSocket connected!")
            
            # GetSceneInfo 도구 호출 (현재 씬 정보 가져오기)
            request = {
                "id": "1",
                "method": "get_scene_info",
                "params": {}
            }
            
            print(f"\nCalling get_scene_info...")
            await ws.send(json.dumps(request))
            
            response = await asyncio.wait_for(ws.recv(), timeout=10)
            data = json.loads(response)
            print(f"Response: {json.dumps(data, indent=2)}")
            
            # GetGameObject 도구 호출
            request2 = {
                "id": "2",
                "method": "get_game_object",
                "params": {
                    "objectPath": "/"
                }
            }
            
            print(f"\nCalling get_game_object...")
            await ws.send(json.dumps(request2))
            
            response2 = await asyncio.wait_for(ws.recv(), timeout=10)
            data2 = json.loads(response2)
            print(f"Response: {json.dumps(data2, indent=2)[:500]}...")
            
            return True
                
    except Exception as e:
        print(f"Error: {type(e).__name__}: {e}")
        import traceback
        traceback.print_exc()
        return False

if __name__ == "__main__":
    result = asyncio.run(test_mcp())
    print(f"\n{'='*50}")
    print(f"MCP CONNECTION TEST: {'SUCCESS' if result else 'FAILED'}")
