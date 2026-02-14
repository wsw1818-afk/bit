import asyncio
import json
import sys

try:
    import websockets
except ImportError:
    print("websockets installing...")
    import subprocess
    subprocess.check_call([sys.executable, "-m", "pip", "install", "websockets"])
    import websockets

async def test_mcp():
    # 여러 엔드포인트 시도
    endpoints = [
        "ws://localhost:8090/",
        "ws://localhost:8090/mcp",
        "ws://localhost:8090/ws",
        "ws://localhost:8090/socket",
        "ws://localhost:8090/websocket",
    ]
    
    for uri in endpoints:
        print(f"\nTrying: {uri}")
        try:
            async with websockets.connect(uri, close_timeout=5) as ws:
                print(f"  SUCCESS! WebSocket connected!")
                
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
                
                print(f"  Sending: {json.dumps(init_request)}")
                await ws.send(json.dumps(init_request))
                
                response = await asyncio.wait_for(ws.recv(), timeout=10)
                print(f"  Received: {response}")
                
                return True
                
        except websockets.exceptions.InvalidStatus as e:
            print(f"  Failed: HTTP {e.response.status_code}")
        except ConnectionRefusedError:
            print(f"  Failed: Connection refused")
        except asyncio.TimeoutError:
            print(f"  Failed: Timeout")
        except Exception as e:
            print(f"  Failed: {type(e).__name__}: {e}")
    
    return False

if __name__ == "__main__":
    result = asyncio.run(test_mcp())
    print(f"\n{'='*40}")
    print(f"Result: {'SUCCESS' if result else 'FAILED'}")
