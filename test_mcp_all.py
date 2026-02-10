import urllib.request
import json
import socket

def test_endpoint(url, method='GET', data=None):
    try:
        if method == 'GET':
            response = urllib.request.urlopen(url, timeout=5)
        else:
            req = urllib.request.Request(url, data=data.encode() if data else None, headers={'Content-Type': 'application/json'})
            response = urllib.request.urlopen(req, timeout=5)
        return response.read().decode()
    except Exception as e:
        return f"Error: {e}"

base_url = "http://localhost:8090"

# Test various endpoints
endpoints = [
    "/",
    "/status",
    "/health",
    "/api/status",
    "/mcp",
    "/tools",
    "/resources"
]

print("=== MCP Unity Server Test ===\n")

for endpoint in endpoints:
    url = base_url + endpoint
    print(f"Testing {endpoint}:")
    result = test_endpoint(url)
    print(f"  Result: {result[:200]}...\n" if len(result) > 200 else f"  Result: {result}\n")

# Check if port is open
print("=== Port Check ===")
sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
result = sock.connect_ex(('localhost', 8090))
if result == 0:
    print("Port 8090 is OPEN")
else:
    print(f"Port 8090 is CLOSED (error code: {result})")
sock.close()