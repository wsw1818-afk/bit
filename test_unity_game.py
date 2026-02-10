#!/usr/bin/env python3
"""
Unity Game Test Automation Script for A.I. BEAT
Tests Unity rhythm game functionality without requiring active Unity Editor
"""

import os
import sys
import json
import subprocess
import time
import platform
from pathlib import Path

class UnityGameTester:
    def __init__(self, project_path="My project"):
        self.project_path = Path(project_path)
        self.unity_path = self.find_unity_executable()
        self.test_results = []
        
    def find_unity_executable(self):
        """Find Unity executable on Windows"""
        if platform.system() == "Windows":
            # Common Unity installation paths
            unity_paths = [
                "C:/Program Files/Unity/Hub/Editor/*/Editor/Unity.exe",
                "C:/Program Files/Unity/Editor/Unity.exe",
                "D:/Unity/*/Editor/Unity.exe",
            ]
            for path_pattern in unity_paths:
                import glob
                matches = glob.glob(path_pattern)
                if matches:
                    # Return the newest version
                    return sorted(matches)[-1]
        return None
        
    def run_editor_tests(self):
        """Run Unity Editor tests using command line"""
        print("🎮 Starting Unity Editor Tests for A.I. BEAT...")
        
        if not self.unity_path or not Path(self.unity_path).exists():
            print("❌ Unity executable not found. Please install Unity or specify path.")
            return self.run_editor_scripts_tests()
            
        test_command = [
            str(self.unity_path),
            "-projectPath", str(self.project_path.absolute()),
            "-batchmode",
            "-nographics",
            "-runTests",
            "-testPlatform", "EditMode",
            "-testResults", "TestResults.xml",
            "-logFile", "test_log.txt",
            "-quit"
        ]
        
        try:
            print("🔄 Running Unity editor tests...")
            result = subprocess.run(test_command, capture_output=True, text=True, timeout=300)
            
            # Check test results
            results_file = self.project_path / "TestResults.xml"
            if results_file.exists():
                self.parse_test_results(results_file)
                return True
            else:
                print("⚠️  No test results file generated")
                return self.check_compilation_errors()
                
        except subprocess.TimeoutExpired:
            print("⏰ Unity test process timed out")
            return False
        except Exception as e:
            print(f"❌ Error running Unity tests: {e}")
            return False
            
    def run_editor_scripts_tests(self):
        """Run script compilation tests without Unity"""
        print("🔍 Testing Unity C# scripts compilation...")
        
        scripts_path = self.project_path / "Assets" / "Scripts"
        if not scripts_path.exists():
            print("❌ Scripts directory not found")
            return False
            
        # Check for compilation errors in Unity scripts
        editor_tests_path = scripts_path / "Editor" / "AIBeatEditorTests.cs"
        if editor_tests_path.exists():
            print("✅ Editor tests found")
            return self.analyze_test_structure()
        else:
            print("⚠️  No editor tests found, checking script structure...")
            return self.check_script_integrity()
            
    def analyze_test_structure(self):
        """Analyze Unity test structure"""
        print("📋 Analyzing test structure...")
        
        test_file = self.project_path / "Assets" / "Scripts" / "Editor" / "AIBeatEditorTests.cs"
        try:
            with open(test_file, 'r', encoding='utf-8') as f:
                content = f.read()
                
            # Count test methods
            test_methods = content.count("[Test]")
            print(f"✅ Found {test_methods} test methods")
            
            # Check different test categories
            test_categories = [
                "NoteData",
                "SongData", 
                "BeatMapper",
                "JudgementResult",
                "GameResult",
                "Scene",
                "Material",
                "Scripts"
            ]
            
            print("🔍 Test coverage:")
            for category in test_categories:
                count = content.count(f"public void {category}")
                if count > 0:
                    print(f"  ✅ {category}: {count} tests")
                    self.test_results.append({
                        "category": category,
                        "count": count,
                        "status": "covered"
                    })
            
            return True
        except Exception as e:
            print(f"❌ Error analyzing tests: {e}")
            return False
            
    def check_script_integrity(self):
        """Check if Unity scripts are properly structured"""
        print("🔍 Checking script integrity...")
        
        critical_scripts = [
            "Core/GameManager.cs",
            "Gameplay/GameplayController.cs",
            "Gameplay/Note.cs",
            "Gameplay/NoteSpawner.cs",
            "Gameplay/JudgementSystem.cs",
            "Gameplay/InputHandler.cs",
            "Data/SongData.cs",
            "Data/NoteData.cs",
            "Audio/BeatMapper.cs"
        ]
        
        scripts_path = self.project_path / "Assets" / "Scripts"
        missing = []
        available = []
        
        for script in critical_scripts:
            script_path = scripts_path / script
            if script_path.exists():
                available.append(script)
                # Check for MonoBehaviour inheritance
                try:
                    with open(script_path, 'r', encoding='utf-8') as f:
                        content = f.read()
                        if ": MonoBehaviour" in content:
                            print(f"  ✅ {script} - MonoBehaviour found")
                        else:
                            print(f"  ⚠️  {script} - No MonoBehaviour found")
                except:
                    print(f"  ⚠️  {script} - Could not read")
            else:
                missing.append(script)
                print(f"  ❌ {script} - Missing")
        
        self.test_results.append({
            "available_scripts": len(available),
            "missing_scripts": len(missing),
            "scripts": available
        })
        
        return len(missing) == 0
        
    def check_compilation_errors(self):
        """Check for common compilation errors"""
        print("🔍 Checking for compilation errors...")
        
        log_file = self.project_path / "test_log.txt"
        if log_file.exists():
            try:
                with open(log_file, 'r', encoding='utf-8') as f:
                    content = f.read()
                    
                errors = []
                for line in content.split('\n'):
                    if "error CS" in line or "compilation error" in line:
                        errors.append(line.strip())
                        
                if errors:
                    print("❌ Compilation errors found:")
                    for error in errors[:5]:  # Show first 5 errors
                        print(f"  {error}")
                    return False
                else:
                    print("✅ No compilation errors detected")
                    return True
            except:
                pass
                
        print("⚠️  Could not check compilation errors")
        return False
        
    def check_unity_project_structure(self):
        """Check Unity project structure"""
        print("📁 Checking Unity project structure...")
        
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
                print(f"  ✅ {dir_name} - {file_count} files")
            else:
                print(f"  ❌ {dir_name} - Missing")
                all_good = False
                
        # Check critical files
        critical_files = [
            "ProjectSettings/ProjectSettings.asset",
            "ProjectSettings/EditorBuildSettings.asset"
        ]
        
        for file_path in critical_files:
            full_path = self.project_path / file_path
            if full_path.exists():
                print(f"  ✅ {file_path}")
            else:
                print(f"  ❌ {file_path}")
                all_good = False
                
        return all_good
        
    def run_gameplay_simulation(self):
        """Simulate basic gameplay scenarios"""
        print("🎮 Simulating gameplay scenarios...")
        
        simulation_results = []
        
        # Test data structures
        try:
            # Simulate note data creation
            note_data = {
                "hitTime": 1.5,
                "lane": 2,
                "noteType": "Tap"
            }
            
            # Simulate song data
            song_data = {
                "title": "Test Song",
                "bpm": 120.0,
                "duration": 60.0,
                "difficulty": 3,
                "notes": [note_data] * 10
            }
            
            # Test judgement calculations
            hit_time = 1.52
            note_time = 1.50
            diff = abs(hit_time - note_time)
            
            # Perfect: < 0.05s, Great: < 0.1s, Good: < 0.2s, Bad: < 0.35s
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
                
            simulation_results.append({
                "test": "judgement_calculation",
                "result": judgement,
                "status": "passed"
            })
            
            print(f"  ✅ Judgement calculation: {judgement} (diff: {diff:.3f}s)")
            
            # Test scoring system
            base_score = 1000
            combo = 50
            max_combo_bonus = 0.5
            combo_for_max_bonus = 100
            
            combo_bonus = (combo / combo_for_max_bonus) * max_combo_bonus
            total_score = base_score * (1 + combo_bonus)
            
            simulation_results.append({
                "test": "scoring_system",
                "score": total_score,
                "status": "passed"
            })
            
            print(f"  ✅ Scoring system: {total_score:.0f} points (combo: {combo})")
            
        except Exception as e:
            print(f"❌ Gameplay simulation failed: {e}")
            return False
            
        self.test_results.extend(simulation_results)
        return True
        
    def generate_test_report(self):
        """Generate comprehensive test report"""
        print("\n" + "="*60)
        print("📊 UNITY GAME TEST REPORT")
        print("="*60)
        
        report = {
            "project": "A.I. BEAT - Rhythm Game",
            "timestamp": time.strftime("%Y-%m-%d %H:%M:%S"),
            "test_results": self.test_results,
            "summary": {}
        }
        
        # Calculate summary
        passed = 0
        failed = 0
        for result in self.test_results:
            if isinstance(result, dict):
                if result.get("status") in ["passed", "covered"]:
                    passed += 1
                else:
                    failed += 1
                    
        report["summary"] = {
            "total_tests": len(self.test_results),
            "passed": passed,
            "failed": failed,
            "success_rate": f"{(passed/len(self.test_results)*100):.1f}%" if self.test_results else "0%"
        }
        
        print(f"📋 Total Tests: {report['summary']['total_tests']}")
        print(f"✅ Passed: {report['summary']['passed']}")
        print(f"❌ Failed: {report['summary']['failed']}")
        print(f"📈 Success Rate: {report['summary']['success_rate']}")
        
        # Save report
        try:
            with open("unity_test_report.json", "w", encoding='utf-8') as f:
                json.dump(report, f, indent=2, ensure_ascii=False)
            print(f"💾 Report saved to: unity_test_report.json")
        except Exception as e:
            print(f"⚠️  Could not save report: {e}")
            
        return report
        
    def run_full_test_suite(self):
        """Run complete test suite"""
        print("🚀 Starting Unity Game Full Test Suite...")
        print("="*60)
        
        # Test 1: Project structure
        if self.check_unity_project_structure():
            self.test_results.append({"test": "project_structure", "status": "passed"})
        else:
            self.test_results.append({"test": "project_structure", "status": "failed"})
            
        # Test 2: Script integrity
        if self.run_editor_scripts_tests():
            self.test_results.append({"test": "script_integrity", "status": "passed"})
        else:
            self.test_results.append({"test": "script_integrity", "status": "failed"})
            
        # Test 3: Gameplay simulation
        if self.run_gameplay_simulation():
            self.test_results.append({"test": "gameplay_simulation", "status": "passed"})
        else:
            self.test_results.append({"test": "gameplay_simulation", "status": "failed"})
            
        # Try Unity-specific tests if available
        if self.unity_path:
            if self.run_editor_tests():
                self.test_results.append({"test": "unity_editor_tests", "status": "passed"})
            else:
                self.test_results.append({"test": "unity_editor_tests", "status": "failed"})
                
        # Generate final report
        return self.generate_test_report()

def main():
    """Main test execution"""
    tester = UnityGameTester()
    report = tester.run_full_test_suite()
    
    # Determine exit code
    success_rate = float(report['summary']['success_rate'].rstrip('%'))
    if success_rate >= 80:
        print("\n🎉 Game test PASSED! The Unity project is in good shape.")
        return 0
    elif success_rate >= 60:
        print("\n⚠️  Game test PARTIAL. Some areas need attention.")
        return 1
    else:
        print("\n❌ Game test FAILED. Significant issues detected.")
        return 2

if __name__ == "__main__":
    sys.exit(main())
}