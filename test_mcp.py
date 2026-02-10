import urllib.request
import json

try:
    req = urllib.request.Request(
        'http://localhost:8090/mcp',
        data=json.dumps({
            'jsonrpc': '2.0',
            'id': 1,
            'method': 'initialize',
            'params': {
                'protocolVersion': '2024-11-05',
                'capabilities': {},
                'clientInfo': {'name': 'cline', 'version': '1.0.0'}
            }
        }).encode(),
        headers={'Content-Type': 'application/json'}
    )
    response = urllib.request.urlopen(req, timeout=5)
    print(response.read().decode())
except Exception as e:
    print(f"연결 실패: {e}")