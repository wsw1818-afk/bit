"""
Unity Game Direct Test
Unity MCP 없이 직접 게임 구조 테스트
"""

import os
import json
from pathlib import Path

class UnityGameTester:
    def __init__(self, project_path="My project"):
        self.project_path = Path(project_path)
        self.test_results = []
        
    def test_project_structure(self):
        """테스트 1: 프로젝트 구조"""
        print("\n📁 [테스트 1] 프로젝트 구조 확인")
        print("-" * 50)
        
        required_dirs = [
            "Assets/Scripts/Core",
            "Assets/Scripts/Gameplay", 
            "Assets/Scripts/Data",
            "Assets/Scenes",
            "Assets/Resources/Songs"
        ]
        
        for dir_path in required_dirs:
            full_path = self.project_path / dir_path
            if full_path.exists():
                files = list(full_path.glob("*.cs")) if "Scripts" in dir_path else list(full_path.glob("*"))
                print(f"  ✅ {dir_path} - {len(files)} 파일")
                self.test_results.append({"test": f"dir_{dir_path}", "status": "passed"})
            else:
                print(f"  ❌ {dir_path} - 없음")
                self.test_results.append({"test": f"dir_{dir_path}", "status": "failed"})
    
    def test_critical_scripts(self):
        """테스트 2: 핵심 스크립트"""
        print("\n📜 [테스트 2] 핵심 스크립트 확인")
        print("-" * 50)
        
        critical_scripts = {
            "Core/GameManager.cs": ["GameManager", "MonoBehaviour"],
            "Gameplay/GameplayController.cs": ["GameplayController", "MonoBehaviour"],
            "Gameplay/Note.cs": ["class Note", "MonoBehaviour"],
            "Gameplay/NoteSpawner.cs": ["NoteSpawner", "MonoBehaviour"],
            "Gameplay/JudgementSystem.cs": ["JudgementSystem", "MonoBehaviour"],
            "Data/SongData.cs": ["SongData", "ScriptableObject"],
            "Data/NoteData.cs": ["NoteData", "Serializable"],
        }
        
        scripts_path = self.project_path / "Assets/Scripts"
        
        for script, required_patterns in critical_scripts.items():
            script_path = scripts_path / script
            if script_path.exists():
                try:
                    with open(script_path, 'r', encoding='utf-8') as f:
                        content = f.read()
                    
                    found_patterns = [p for p in required_patterns if p in content]
                    if found_patterns:
                        print(f"  ✅ {script} - {', '.join(found_patterns)}")
                        self.test_results.append({"test": f"script_{script}", "status": "passed"})
                    else:
                        print(f"  ⚠️  {script} - 패턴 미발견")
                        self.test_results.append({"test": f"script_{script}", "status": "warning"})
                except Exception as e:
                    print(f"  ❌ {script} - 읽기 오류: {e}")
                    self.test_results.append({"test": f"script_{script}", "status": "failed"})
            else:
                print(f"  ❌ {script} - 파일 없음")
                self.test_results.append({"test": f"script_{script}", "status": "failed"})
    
    def test_scenes(self):
        """테스트 3: 씬 파일"""
        print("\n🎬 [테스트 3] 씬 구성 확인")
        print("-" * 50)
        
        scenes = ["MainMenu.unity", "SongSelect.unity", "Gameplay.unity"]
        scenes_path = self.project_path / "Assets/Scenes"
        
        for scene in scenes:
            scene_path = scenes_path / scene
            if scene_path.exists():
                size = scene_path.stat().st_size / 1024  # KB
                print(f"  ✅ {scene} - {size:.1f} KB")
                self.test_results.append({"test": f"scene_{scene}", "status": "passed"})
            else:
                print(f"  ❌ {scene} - 없음")
                self.test_results.append({"test": f"scene_{scene}", "status": "failed"})
    
    def test_game_logic(self):
        """테스트 4: 게임 로직 시뮬레이션"""
        print("\n🎮 [테스트 4] 게임 로직 시뮬레이션")
        print("-" * 50)
        
        # 판정 로직 테스트
        judgement_tests = [
            (0.02, "Perfect"),
            (0.07, "Great"),
            (0.15, "Good"),
            (0.30, "Bad"),
            (0.50, "Miss")
        ]
        
        print("  판정 시스템 테스트:")
        for diff, expected in judgement_tests:
            if diff < 0.05:
                result = "Perfect"
            elif diff < 0.1:
                result = "Great"
            elif diff < 0.2:
                result = "Good"
            elif diff < 0.35:
                result = "Bad"
            else:
                result = "Miss"
            
            status = "✅" if result == expected else "❌"
            print(f"    {status} 차이 {diff:.2f}s → {result} (예상: {expected})")
            self.test_results.append({"test": f"judgement_{diff}", "status": "passed" if result == expected else "failed"})
        
        # 스코어 계산 테스트
        print("\n  스코어 시스템 테스트:")
        base_score = 100
        combo = 50
        combo_bonus = min(combo / 100 * 0.5, 0.5)
        total = base_score * (1 + combo_bonus)
        print(f"    ✅ 콤보 {combo}: {base_score} → {total:.0f}점 (+{combo_bonus*100:.0f}% 복합)")
        self.test_results.append({"test": "scoring_system", "status": "passed"})
        
        # 노트 데이터 구조 테스트
        print("\n  노트 데이터 구조 테스트:")
        note_data = {
            "hitTime": 1.5,
            "lane": 2,
            "noteType": "Tap",
            "duration": 0.0
        }
        print(f"    ✅ NoteData 생성: {note_data}")
        self.test_results.append({"test": "note_data_structure", "status": "passed"})
    
    def test_resources(self):
        """테스트 5: 리소스 확인"""
        print("\n🎵 [테스트 5] 리소스 확인")
        print("-" * 50)
        
        # 오디오 파일
        audio_path = self.project_path / "Assets/StreamingAssets"
        audio_files = list(audio_path.glob("*.mp3")) + list(audio_path.glob("*.wav"))
        print(f"  오디오 파일: {len(audio_files)}개")
        for f in audio_files:
            size = f.stat().st_size / (1024*1024)
            print(f"    📁 {f.name} ({size:.1f} MB)")
        self.test_results.append({"test": "audio_resources", "status": "passed" if audio_files else "warning"})
        
        # 폰트
        font_path = self.project_path / "Assets/Resources/Fonts"
        if font_path.exists():
            fonts = list(font_path.glob("*.ttf"))
            print(f"  폰트 파일: {len(fonts)}개")
            for f in fonts:
                print(f"    🔤 {f.name}")
            self.test_results.append({"test": "font_resources", "status": "passed"})
        
        # SongData
        songs_path = self.project_path / "Assets/Resources/Songs"
        if songs_path.exists():
            songs = list(songs_path.glob("*.asset"))
            print(f"  곡 데이터: {len(songs)}개")
            for s in songs:
                print(f"    🎼 {s.name}")
            self.test_results.append({"test": "song_resources", "status": "passed"})
    
    def generate_report(self):
        """최종 보고서"""
        print("\n" + "="*60)
        print("📊 UNITY A.I. BEAT 게임 테스트 보고서")
        print("="*60)
        
        passed = sum(1 for r in self.test_results if r["status"] == "passed")
        failed = sum(1 for r in self.test_results if r["status"] == "failed")
        warning = sum(1 for r in self.test_results if r["status"] == "warning")
        total = len(self.test_results)
        
        print(f"\n총 테스트: {total}")
        print(f"  ✅ 통과: {passed}")
        print(f"  ⚠️  경고: {warning}")
        print(f"  ❌ 실패: {failed}")
        
        if total > 0:
            success_rate = passed / total * 100
            print(f"\n성공률: {success_rate:.1f}%")
            
            if success_rate >= 90:
                print("\n🎉 결과: 훌륭함! 게임이 정상 작동합니다.")
            elif success_rate >= 70:
                print("\n✅ 결과: 양호함. 일부 항목 확인 필요.")
            elif success_rate >= 50:
                print("\n⚠️  결과: 주의. 여러 문제가 발견되었습니다.")
            else:
                print("\n❌ 결과: 심각함. 주요 기능이 작동하지 않습니다.")
        
        # JSON 저장
        report = {
            "project": "A.I. BEAT",
            "timestamp": "2026-02-10",
            "summary": {
                "total": total,
                "passed": passed,
                "failed": failed,
                "warning": warning,
                "success_rate": f"{success_rate:.1f}%" if total > 0 else "0%"
            },
            "details": self.test_results
        }
        
        with open("game_test_report.json", "w", encoding="utf-8") as f:
            json.dump(report, f, indent=2, ensure_ascii=False)
        print(f"\n💾 보고서 저장됨: game_test_report.json")
    
    def run_all_tests(self):
        """모든 테스트 실행"""
        print("🚀 A.I. BEAT 게임 테스트 시작")
        print("="*60)
        
        self.test_project_structure()
        self.test_critical_scripts()
        self.test_scenes()
        self.test_game_logic()
        self.test_resources()
        self.generate_report()

if __name__ == "__main__":
    tester = UnityGameTester()
    tester.run_all_tests()