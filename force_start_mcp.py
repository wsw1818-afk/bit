#!/usr/bin/env python3
"""
Unity MCP Server 강제 시작 테스트
Unity Editor가 실행 중일 때 MCP 서버를 시작하는 다양한 방법 테스트
"""

import urllib.request
import json
import socket
import time
import subprocess
import sys

def check_port(port=8090):
    """포트 상태 확인"""
    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    result = sock.connect_ex(('localhost', port))
    sock.close()
    return result == 0

def test_mcp_endpoint():
    """MCP 엔드포인트 테스트"""
    endpoints = [
        "http://localhost:8090/",
        "http://localhost:8090/mcp",
        "http://localhost:8090/status",
        "http://127.0.0.1:8090/",
    ]
    
    results = []
    for url in endpoints:
        try:
            req = urllib.request.Request(url, timeout=2)
            response = urllib.request.urlopen(req)
            results.append((url, True, response.status))
        except Exception as e:
            results.append((url, False, str(e)))
    
    return results

def main():
    print("=" * 60)
    print("Unity MCP Server 강제 시작 테스트")
    print("=" * 60)
    print()
    
    # 1. 현재 포트 상태 확인
    print("[1] 포트 8090 상태 확인")
    if check_port(8090):
        print("    ✅ 포트 8090 OPEN - MCP 서버가 이미 실행 중!")
    else:
        print("    ❌ 포트 8090 CLOSED - MCP 서버 미실행")
    print()
    
    # 2. MCP 엔드포인트 테스트
    print("[2] MCP 엔드포인트 테스트")
    results = test_mcp_endpoint()
    for url, success, info in results:
        status = "✅" if success else "❌"
        print(f"    {status} {url}")
        if not success:
            print(f"       에러: {info[:50]}")
    print()
    
    # 3. Unity 프로세스 확인
    print("[3] Unity 프로세스 확인")
    try:
        result = subprocess.run(['tasklist', '/FI', 'IMAGENAME eq Unity.exe'], 
                              capture_output=True, text=True)
        if 'Unity.exe' in result.stdout:
            lines = [l for l in result.stdout.split('\n') if 'Unity.exe' in l]
            print(f"    ✅ Unity 프로세스 {len(lines)}개 실행 중")
            for line in lines[:3]:
                print(f"       {line.strip()}")
        else:
            print("    ❌ Unity 프로세스 미실행")
    except Exception as e:
        print(f"    ⚠️ 확인 실패: {e}")
    print()
    
    # 4. 결론
    print("=" * 60)
    print("결론:")
    if check_port(8090):
        print("✅ MCP 서버가 실행 중입니다!")
    else:
        print("❌ MCP 서버가 실행되지 않았습니다.")
        print()
        print("가능한 원인:")
        print("  1. Unity Editor에서 MCP 서버를 수동으로 시작해야 함")
        print("  2. com.gamelovers.mcp-unity 패키지는 CLI MCP가 아닐 수 있음")
        print("  3. 다른 MCP 패키지 설치 필요 (예: McpUnity)")
        print()
        print("해결 방법:")
        print("  - Unity Editor에서 Window > MCP Unity 메뉴 확인")
        print("  - 또는 Window > Package Manager에서 'McpUnity' 검색/설치")
    print("=" * 60)

if __name__ == "__main__":
    main()
