import time
import subprocess
import sys

print("20초 대기 중... Unity 에디터가 완전히 로드되도록 기다립니다.")
for i in range(20, 0, -1):
    print(f"남은 시간: {i}초", end='\r')
    time.sleep(1)
print("\n테스트 시작!")

# test_mcp_all.py 실행
result = subprocess.run([sys.executable, 'test_mcp_all.py'], capture_output=True, text=True)
print(result.stdout)
if result.stderr:
    print("오류:", result.stderr)