#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TMPro;

namespace AIBeat.Editor
{
    public static class KoreanFontCreator
    {
        [MenuItem("Tools/A.I. BEAT/Create Korean TMP Font")]
        public static void CreateKoreanFont()
        {
            // 먼저 AssetDatabase 리프레시 (새로 복사한 폰트 인식)
            AssetDatabase.Refresh();

            string fontPath = "Assets/TextMesh Pro/Fonts/MalgunGothicBold.ttf";
            string outputPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/MalgunGothicBold SDF.asset";

            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            if (sourceFont == null)
            {
                Debug.LogError("[KoreanFontCreator] 폰트 파일을 찾을 수 없습니다: " + fontPath);
                return;
            }

            Debug.Log("[KoreanFontCreator] 폰트 로드 성공: " + sourceFont.name);

            // 기존 에셋 삭제
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outputPath);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(outputPath);
            }

            // 간단한 오버로드로 생성
            var fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont);
            if (fontAsset == null)
            {
                Debug.LogError("[KoreanFontCreator] TMP_FontAsset 생성 실패!");
                return;
            }

            // Dynamic 모드 설정 (런타임에서 한국어 글리프 자동 생성)
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;

            // 에셋 저장
            AssetDatabase.CreateAsset(fontAsset, outputPath);

            // 서브 에셋 저장 (Atlas 텍스처, Material)
            if (fontAsset.atlasTextures != null)
            {
                for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
                {
                    fontAsset.atlasTextures[i].name = "MalgunGothicBold SDF Atlas " + i;
                    AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[i], fontAsset);
                }
            }
            if (fontAsset.material != null)
            {
                fontAsset.material.name = "MalgunGothicBold SDF Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[KoreanFontCreator] 한국어 TMP 폰트 에셋 생성 완료: " + outputPath);
            Selection.activeObject = fontAsset;
        }
    }
}
#endif
