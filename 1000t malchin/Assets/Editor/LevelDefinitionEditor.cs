using UnityEditor;
using UnityEngine;
using Malchin.Combat;

namespace Malchin.EditorTools
{
    /// <summary>
    /// Custom inspector for LevelDefinition: grid settings, reward, and an
    /// editable spawn timeline (time / column / enemy) with add, remove, and
    /// sort, plus a readable preview. Keeps levels easy to author by hand.
    /// </summary>
    [CustomEditor(typeof(LevelDefinition))]
    public class LevelDefinitionEditor : Editor
    {
        SerializedProperty _levelName, _gridWidth, _gridHeight, _cellSize, _baseMaxHP;
        SerializedProperty _rewardSheep, _rewardCattle, _rewardHorse, _spawns;

        void OnEnable()
        {
            _levelName    = serializedObject.FindProperty("levelName");
            _gridWidth    = serializedObject.FindProperty("gridWidth");
            _gridHeight   = serializedObject.FindProperty("gridHeight");
            _cellSize     = serializedObject.FindProperty("cellSize");
            _baseMaxHP    = serializedObject.FindProperty("baseMaxHP");
            _rewardSheep  = serializedObject.FindProperty("rewardSheep");
            _rewardCattle = serializedObject.FindProperty("rewardCattle");
            _rewardHorse  = serializedObject.FindProperty("rewardHorse");
            _spawns       = serializedObject.FindProperty("spawns");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_levelName);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Battlefield grid", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_gridWidth, new GUIContent("Columns"));
            EditorGUILayout.PropertyField(_gridHeight, new GUIContent("Rows"));
            EditorGUILayout.PropertyField(_cellSize);
            EditorGUILayout.PropertyField(_baseMaxHP, new GUIContent("Base HP"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Reward on win", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_rewardSheep, new GUIContent("Sheep"));
            EditorGUILayout.PropertyField(_rewardCattle, new GUIContent("Cattle"));
            EditorGUILayout.PropertyField(_rewardHorse, new GUIContent("Special Horse"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Spawn timeline", EditorStyles.boldLabel);
            DrawSpawnHeader();

            int maxColumn = Mathf.Max(0, _gridWidth.intValue - 1);
            int removeIndex = -1;
            for (int i = 0; i < _spawns.arraySize; i++)
            {
                var element = _spawns.GetArrayElementAtIndex(i);
                var time   = element.FindPropertyRelative("time");
                var column = element.FindPropertyRelative("column");
                var enemy  = element.FindPropertyRelative("enemy");

                EditorGUILayout.BeginHorizontal();
                time.floatValue = Mathf.Max(0f, EditorGUILayout.FloatField(time.floatValue, GUILayout.Width(60)));
                column.intValue = Mathf.Clamp(EditorGUILayout.IntField(column.intValue, GUILayout.Width(50)), 0, maxColumn);
                EditorGUILayout.PropertyField(enemy, GUIContent.none);
                if (GUILayout.Button("✕", GUILayout.Width(24))) removeIndex = i;
                EditorGUILayout.EndHorizontal();
            }
            if (removeIndex >= 0) _spawns.DeleteArrayElementAtIndex(removeIndex);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add spawn")) AddSpawn();
            if (GUILayout.Button("Sort by time")) SortByTime();
            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();

            DrawPreview();
        }

        void DrawSpawnHeader()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Time", GUILayout.Width(60));
            EditorGUILayout.LabelField("Col", GUILayout.Width(50));
            EditorGUILayout.LabelField("Enemy");
            GUILayout.Space(28);
            EditorGUILayout.EndHorizontal();
        }

        void AddSpawn()
        {
            int i = _spawns.arraySize;
            _spawns.InsertArrayElementAtIndex(i);
            var element = _spawns.GetArrayElementAtIndex(i);
            // Default the new spawn just after the current latest time.
            float latest = 0f;
            for (int k = 0; k < i; k++)
                latest = Mathf.Max(latest, _spawns.GetArrayElementAtIndex(k).FindPropertyRelative("time").floatValue);
            element.FindPropertyRelative("time").floatValue = i == 0 ? 0f : latest + 1.5f;
            element.FindPropertyRelative("column").intValue = 0;
        }

        void SortByTime()
        {
            var level = (LevelDefinition)target;
            Undo.RecordObject(level, "Sort spawns by time");
            level.spawns.Sort((a, b) => a.time.CompareTo(b.time));
            EditorUtility.SetDirty(level);
            serializedObject.Update();
        }

        void DrawPreview()
        {
            var level = (LevelDefinition)target;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"{level.TotalEnemies} enemies over {level.Duration:0.0}s on a {level.gridWidth}x{level.gridHeight} grid.");
            foreach (var s in level.SortedSpawns())
            {
                string name = s.enemy != null ? s.enemy.displayName : "(none)";
                sb.AppendLine($"  t={s.time:0.0}s  col {s.column}  {name}");
            }
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(sb.ToString(), MessageType.None);
        }
    }
}
