import asyncio
import websockets
import json

async def test_mcp_websocket():
    uri = "ws://localhost:8090"
    
    try:
        async with websockets.connect(uri) as websocket:
            print(f"✅ WebSocket 연결 성공: {uri}")
            
            # MCP initialize 요청
            init_message = {
                "jsonrpc": "2.0",
                "id": 1,
                "method": "initialize",
                "params": {
                    "protocolVersion": "2024-11-05",
                    "capabilities": {},
                    "clientInfo": {"name": "test-client", "version": "1.0.0"}
                }
            }
            
            await websocket.send(json.dumps(init_message))
            print(f"📤 요청 전송: {json.dumps(init_message, indent=2)}")
            
            response = await websocket.recv()
            print(f"📥 응답 수신: {response}")
            
            response_json = json.loads(response)
            if "result" in response_json:
                print("✅ MCP 초기화 성공!")
                print(f"   서버 정보: {response_json['result'].get('serverInfo', {})}")
            else:
                print(f"⚠️  응답에 오류가 있을 수 있습니다: {response_json}")
                
    except websockets.exceptions.ConnectionRefusedError:
        print(f"❌ 연결 거부: {uri}")
        print("   Unity MCP 서버가 실행 중인지 확인하세요.")
    except websockets.exceptions.WebSocketException as e:
        print(f"❌ WebSocket 오류: {e}")
    except Exception as e:
        print(f"❌ 연결 실패: {e}")

if __name__ == "__main__":
    asyncio.run(test_mcp_websocket())
