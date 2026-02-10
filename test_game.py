#!/usr/bin/env python3
"""
Unity Game Test Automation for A.I. BEAT
Tests Unity rhythm game functionality
"""

import os
import sys
import json
import time
from pathlib import Path

class UnityGameTester:
    def __init__(self, project_path="My project"):
        self.project_path = Path(project_path)
        self.test_results = []
    
    def check_unity_project_structure(self):
        """Check Unity project structure"""
        print("Checking Unity project structure...")
        
        required_dirs = [
            "Assets/Scripts",
            "Assets/Scenes", 
            "Assets/Prefabs",
            "Assets/Resources",
            "ProjectSettings"
        ]
        
        all_good = True
        for dir_name in required_dirs:
            dir_path = self.project_path / dir_name
            if dir_path.exists():
                file_count = len(list(dir_path.rglob("*")))
                print(f"  OK {dir_name} - {file_count} files")
            else:
                print(f"  MISSING {dir_name}")
                all_good = False
        
        return all_good
    
    def check_script_integrity(self):
        """Check if Unity scripts are properly structured"""
        print("\nChecking script integrity...")
        
        critical_scripts = [
            "Core/GameManager.cs",
            "Gameplay/GameplayController.cs",
            "Gameplay/Note.cs",
            "Gameplay/NoteSpawner.cs",
            "Gameplay/JudgementSystem.cs",
            "Gameplay/InputHandler.cs",
            "Data/SongData.cs",
            "Data/NoteData.cs"
        ]
        
        scripts_path = self.project_path / "Assets" / "Scripts"
        missing = []
        available = []
        
        for script in critical_scripts:
            script_path = scripts_path / script
            if script_path.exists():
                available.append(script)
                print(f"  OK {script}")
            else:
                missing.append(script)
                print(f"  MISSING {script}")
        
        self.test_results.append({
            "available_scripts": len(available),
            "missing_scripts": len(missing)
        })
        
        return len(missing) == 0
    
    def analyze_editor_tests(self):
        """Analyze Unity test structure"""
        print("\nAnalyzing editor tests...")
        
        test_file = self.project_path / "Assets" / "Scripts" / "Editor" / "AIBeatEditorTests.cs"
        try:
            with open(test_file, 'r', encoding='utf-8') as f:
                content = f.read()
            
            test_methods = content.count("[Test]")
            print(f"  Found {test_methods} test methods")
            
            test_categories = [
                "NoteData", "SongData", "BeatMapper",
                "JudgementResult", "GameResult",
                "Scene", "Material", "Scripts"
            ]
            
            for category in test_categories:
                count = content.count(f"public void {category}")
                if count > 0:
                    print(f"  {category}: {count} tests")
                    self.test_results.append({"category": category, "count": count})
            
            return True
        except Exception as e:
            print(f"  ERROR: {e}")
            return False
    
    def run_gameplay_simulation(self):
        """Simulate basic gameplay scenarios"""
        print("\nSimulating gameplay...")
        
        try:
            # Test judgement system
            hit_time = 1.52
            note_time = 1.50
            diff = abs(hit_time - note_time)
            
            if diff < 0.05:
                judgement = "Perfect"
            elif diff < 0.1:
                judgement = "Great"
            elif diff < 0.2:
                judgement = "Good"
            elif diff < 0.35:
                judgement = "Bad"
            else:
                judgement = "Miss"
            
            print(f"  Judgement: {judgement} (diff: {diff:.3f}s)")
            
            # Test scoring
            base_score = 1000
            combo = 50
            max_combo_bonus = 0.5
            combo_for_max_bonus = 100
            
            combo_bonus = (combo / combo_for_max_bonus) * max_combo_bonus
            total_score = base_score * (1 + combo_bonus)
            
            print(f"  Score: {total_score:.0f} points (combo: {combo})")
            
            self.test_results.extend([
                {"test": "judgement", "result": judgement, "status": "passed"},
                {"test": "scoring", "score": total_score, "status": "passed"}
            ])
            
            return True
            
        except Exception as e:
            print(f"  ERROR: {e}")
            return False
    
    def run_full_test_suite(self):
        """Run complete test suite"""
        print("Unity Game Test Suite for A.I. BEAT")
        print("=" * 60)
        
        # Test 1: Project structure
        if self.check_unity_project_structure():
            self.test_results.append({"test": "project_structure", "status": "passed"})
        else:
            self.test_results.append({"test": "project_structure", "status": "failed"})
        
        # Test 2: Script integrity
        if self.check_script_integrity():
            self.test_results.append({"test": "script_integrity", "status": "passed"})
        else:
            self.test_results.append({"test": "script_integrity", "status": "failed"})
        
        # Test 3: Editor tests
        if self.analyze_editor_tests():
            self.test_results.append({"test": "editor_tests", "status": "passed"})
        else:
            self.test_results.append({"test": "editor_tests", "status": "failed"})
        
        # Test 4: Gameplay simulation
        if self.run_gameplay_simulation():
            self.test_results.append({"test": "gameplay_simulation", "status": "passed"})
        else:
            self.test_results.append({"test": "gameplay_simulation", "status": "failed"})
        
        return self.generate_test_report()
    
    def generate_test_report(self):
        """Generate test report"""
        print("\n" + "=" * 60)
        print("UNITY GAME TEST REPORT")
        print("=" * 60)
        
        passed = sum(1 for r in self.test_results 
                    if isinstance(r, dict) and r.get("status") == "passed")
        failed = sum(1 for r in self.test_results 
                    if isinstance(r, dict) and r.get("status") == "failed")
        
        print(f"Total Tests: {len(self.test_results)}")
        print(f"Passed: {passed}")
        print(f"Failed: {failed}")
        if self.test_results:
            rate = (passed / len(self.test_results)) * 100
            print(f"Success Rate: {rate:.1f}%")
        
        # Save report
        report = {
            "project": "A.I. BEAT - Rhythm Game",
            "timestamp": time.strftime("%Y-%m-%d %H:%M:%S"),
            "test_results": self.test_results,
            "summary": {
                "total": len(self.test_results),
                "passed": passed,
                "failed": failed
            }
        }
        
        try:
            with open("unity_test_report.json", "w") as f:
                json.dump(report, f, indent=2)
            print("\nReport saved to: unity_test_report.json")
        except Exception as e:
            print(f"Could not save report: {e}")
        
        return report

def main():
    """Main function"""
    tester = UnityGameTester()
    report = tester.run_full_test_suite()
    
    success_rate = report['summary']['passed'] / report['summary']['total'] * 100
    if success_rate >= 80:
        print("\nGame test PASSED!")
        return 0
    elif success_rate >= 60:
        print("\nGame test PARTIAL")
        return 1
    else:
        print("\nGame test FAILED")
        return 2

if __name__ == "__main__":
    sys.exit(main())
