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
    # 올바른 경로: /McpUnity
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
            
            # MCP initialize 요청
            init_request = {
                "jsonrpc": "2.0",
                "id": 1,
                "method": "initialize",
                "params": {
                    "protocolVersion": "2024-11-05",
                    "capabilities": {},
                    "clientInfo": {"name": "test-client", "version": "1.0.0"}
                }
            }
            
            print(f"\nSending initialize...")
            await ws.send(json.dumps(init_request))
            
            try:
                response = await asyncio.wait_for(ws.recv(), timeout=10)
                data = json.loads(response)
                print(f"Initialize response: {json.dumps(data, indent=2)}")
                
                # initialized 알림
                initialized = {
                    "jsonrpc": "2.0",
                    "method": "notifications/initialized"
                }
                await ws.send(json.dumps(initialized))
                
                # tools/list 요청
                tools_request = {
                    "jsonrpc": "2.0",
                    "id": 2,
                    "method": "tools/list",
                    "params": {}
                }
                
                print(f"\nRequesting tools list...")
                await ws.send(json.dumps(tools_request))
                
                response = await asyncio.wait_for(ws.recv(), timeout=10)
                data = json.loads(response)
                
                if "result" in data and "tools" in data["result"]:
                    tools = data["result"]["tools"]
                    print(f"\nAvailable tools ({len(tools)}):")
                    for tool in tools:
                        print(f"  - {tool.get('name', 'unknown')}")
                else:
                    print(f"Tools response: {json.dumps(data, indent=2)}")
                
                return True
                
            except asyncio.TimeoutError:
                print("Timeout waiting for response")
                return False
                
    except websockets.exceptions.InvalidStatus as e:
        print(f"WebSocket handshake failed: HTTP {e.response.status_code}")
        return False
    except Exception as e:
        print(f"Error: {type(e).__name__}: {e}")
        import traceback
        traceback.print_exc()
        return False

if __name__ == "__main__":
    result = asyncio.run(test_mcp())
    print(f"\n{'='*50}")
    print(f"FINAL RESULT: {'SUCCESS' if result else 'FAILED'}")
