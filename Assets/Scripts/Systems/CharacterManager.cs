// ============================================================
// CharacterManager.cs
// Sistema de Save/Load local usando JSON e ficheiros PNG.
// Otimizado com Cache de Texturas para evitar Memory Leaks.
// ============================================================
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;

[Serializable]
public class CharField { public string key; public string value; }

[Serializable]
public enum CharacterType { Player = 0, NPC = 1, Enemy = 2 }

[Serializable]
public enum CharacterState { Active = 0, Dead = 1, Hidden = 2 }

[Serializable]
public class AvatarCropData
{
    public float zoom = 1f;
    public float offsetX = 0f;
    public float offsetY = 0f;
}
[Serializable]
public class CharacterRecord
{
    public string id; public string system; public string name; public string subText;
    public string statsStr; public string avatarFileName;
    public CharacterType characterType = CharacterType.Player;
    public CharacterState state = CharacterState.Active;
    public AvatarCropData avatarCrop = new AvatarCropData();
    public bool defaultRenderInProjection = true;
    public List<CharField> fields = new List<CharField>();
}

[Serializable]
public class CharacterDB { public List<CharacterRecord> records = new List<CharacterRecord>(); }

public class CharacterManager : MonoBehaviour
{
    public static CharacterManager Instance { get; private set; }
    public CharacterDB Database = new CharacterDB();
    private string dirPath; private string dbPath;

    // --- OTIMIZAÇÃO: Cache de Texturas na Memória RAM ---
    private Dictionary<string, Texture2D> avatarCache = new Dictionary<string, Texture2D>();

    public event Action OnCharactersUpdated;

    private void Awake()
    {
        if (Instance == null) Instance = this; else { Destroy(gameObject); return; }
        dirPath = Path.Combine(Application.persistentDataPath, "Characters");
        if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);
        dbPath = Path.Combine(dirPath, "chardb.json");
        LoadDatabase();
    }

    public void LoadDatabase()
    {
        if (File.Exists(dbPath))
        {
            string json = File.ReadAllText(dbPath);
            Database = JsonUtility.FromJson<CharacterDB>(json) ?? new CharacterDB();
        }
        NormalizeDatabase();
    }

    private void NormalizeDatabase()
    {
        if (Database == null) Database = new CharacterDB();
        if (Database.records == null) Database.records = new List<CharacterRecord>();

        foreach (var record in Database.records)
        {
            if (record.fields == null) record.fields = new List<CharField>();
            if (record.avatarCrop == null) record.avatarCrop = new AvatarCropData();
        }
    }

    public void SaveDatabase()
    {
        string json = JsonUtility.ToJson(Database, true);
        File.WriteAllText(dbPath, json);
        OnCharactersUpdated?.Invoke();
    }

    public CharacterRecord GetCharacter(string id) => Database.records.Find(r => r.id == id);
    public static string GetCharacterTypeLabel(CharacterType type)
    {
        switch (type)
        {
            case CharacterType.NPC: return "NPC";
            case CharacterType.Enemy: return "Inimigo";
            default: return "Jogador";
        }
    }

    public static string GetCharacterStateLabel(CharacterState state)
    {
        switch (state)
        {
            case CharacterState.Dead: return "Morto";
            case CharacterState.Hidden: return "Oculto";
            default: return "Ativo";
        }
    }

    public void UpdateCharacterField(string id, string key, string value)
    {
        var record = GetCharacter(id);
        if (record != null)
        {
            var field = record.fields.Find(f => f.key == key);
            if (field != null) field.value = value;
            else record.fields.Add(new CharField { key = key, value = value });

            RebuildDerivedFields(record);
            RebuildStatsStr(record);
            SaveDatabase();
        }
    }


    public void UpdateCharacterSessionFields(string id, Dictionary<string, string> fieldValues)
    {
        var record = GetCharacter(id);
        if (record == null || fieldValues == null) return;

        EnsureRecordDefaults(record);
        foreach (var kvp in fieldValues)
        {
            var field = record.fields.Find(f => f.key == kvp.Key);
            if (field != null) field.value = kvp.Value;
            else record.fields.Add(new CharField { key = kvp.Key, value = kvp.Value });
        }

        RebuildDerivedFields(record);
        RebuildStatsStr(record);
        SaveDatabase();
    }
    private void RebuildStatsStr(CharacterRecord record)
    {
        string cGold = "#E8C84A", cBlue = "#598CD9", cRed = "#D95959", cPurp = "#9D59D9";
        if (record.system == "D&D 5e")
        {
            record.statsStr = $"HP: <color={cGold}>{GetField(record, "dnd_hp_curr")}/{GetField(record, "dnd_hp_max")}</color>   CA: <color={cRed}>{GetField(record, "dnd_ac")}</color>   MOV: <color={cBlue}>{GetField(record, "dnd_spd")}</color>";
        }
        else
        {
            record.statsStr = $"PV: <color={cGold}>{GetField(record, "ord_pv_curr")}/{GetField(record, "ord_pv_max")}</color>   PE: <color={cBlue}>{GetField(record, "ord_pe_curr")}/{GetField(record, "ord_pe_max")}</color>   SAN: <color={cPurp}>{GetField(record, "ord_san_curr")}/{GetField(record, "ord_san_max")}</color>   DEF: <color={cRed}>{GetField(record, "ord_defesa")}</color>";
        }
    }
    private string GetField(CharacterRecord r, string k) { var f = r.fields.Find(x => x.key == k); return f != null && !string.IsNullOrEmpty(f.value) ? f.value : "0"; }
    private void RebuildDerivedFields(CharacterRecord record)
    {
        if (record == null) return;
        EnsureRecordDefaults(record);

        if (record.system == "D&D 5e")
        {
            string[] attrs = { "str", "dex", "con", "int", "wis", "cha" };
            foreach (string attr in attrs)
            {
                int score = GetIntField(record, "dnd_" + attr, 10);
                UpsertField(record, "dnd_" + attr + "_mod", FormatSigned(Mathf.FloorToInt((score - 10) / 2f)));
            }

            int level = Mathf.Max(1, GetIntField(record, "dnd_level", 1));
            int proficiency = 2 + Mathf.FloorToInt((level - 1) / 4f);
            UpsertField(record, "dnd_prof", FormatSigned(proficiency));
        }
        else if (record.system == "Ordem Paranormal")
        {
            string[] attrs = { "agi", "for", "int", "pre", "vig" };
            foreach (string attr in attrs)
            {
                int score = GetIntField(record, "ord_" + attr, 0);
                UpsertField(record, "ord_" + attr + "_mod", FormatSigned(score));
            }
        }
    }

    private int GetIntField(CharacterRecord record, string key, int fallback)
    {
        if (record == null || record.fields == null) return fallback;
        var field = record.fields.Find(f => f.key == key);
        if (field == null || string.IsNullOrWhiteSpace(field.value)) return fallback;
        return int.TryParse(field.value, out int parsed) ? parsed : fallback;
    }

    private void UpsertField(CharacterRecord record, string key, string value)
    {
        var field = record.fields.Find(f => f.key == key);
        if (field != null) field.value = value;
        else record.fields.Add(new CharField { key = key, value = value });
    }

    private string FormatSigned(int value)
    {
        return value >= 0 ? "+" + value : value.ToString();
    }

    private void EnsureRecordDefaults(CharacterRecord record)
    {
        if (record == null) return;
        if (record.fields == null) record.fields = new List<CharField>();
        if (record.avatarCrop == null) record.avatarCrop = new AvatarCropData();
    }

    public void SaveCharacter(CharacterRecord record, Texture2D newAvatar, bool isEdit, bool avatarChanged)
    {
        EnsureRecordDefaults(record);
        RebuildDerivedFields(record);
        if (isEdit)
        {
            var existing = Database.records.Find(r => r.id == record.id);
            if (existing != null)
            {
                if (avatarChanged)
                {
                    if (newAvatar != null)
                    {
                        string fileName = "avatar_" + record.id + "_" + DateTime.Now.Ticks + ".png";
                        File.WriteAllBytes(Path.Combine(dirPath, fileName), newAvatar.EncodeToPNG());
                        record.avatarFileName = fileName;

                        // Atualiza o cache imediatamente
                        avatarCache[fileName] = newAvatar;
                    }
                    else record.avatarFileName = "";

                    if (!string.IsNullOrEmpty(existing.avatarFileName))
                    {
                        string oldPath = Path.Combine(dirPath, existing.avatarFileName);
                        if (File.Exists(oldPath)) File.Delete(oldPath);

                        // Limpa a textura velha da memória
                        UnloadAvatarFromCache(existing.avatarFileName);
                    }
                }
                else record.avatarFileName = existing.avatarFileName;
                Database.records.Remove(existing);
            }
        }
        else
        {
            if (newAvatar != null)
            {
                string fileName = "avatar_" + record.id + ".png";
                File.WriteAllBytes(Path.Combine(dirPath, fileName), newAvatar.EncodeToPNG());
                record.avatarFileName = fileName;

                // Insere no cache
                avatarCache[fileName] = newAvatar;
            }
        }
        Database.records.Add(record); SaveDatabase();
    }

    public void DeleteCharacter(string id)
    {
        var record = Database.records.Find(r => r.id == id);
        if (record != null)
        {
            if (!string.IsNullOrEmpty(record.avatarFileName))
            {
                string imgPath = Path.Combine(dirPath, record.avatarFileName);
                if (File.Exists(imgPath)) File.Delete(imgPath);

                // Limpa a textura da memória
                UnloadAvatarFromCache(record.avatarFileName);
            }
            Database.records.Remove(record); SaveDatabase();
        }
    }

    public Texture2D LoadAvatar(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return null;

        // Se já carregamos essa foto antes, apenas devolve ela da memória RAM
        if (avatarCache.TryGetValue(fileName, out Texture2D cachedTex) && cachedTex != null)
        {
            return cachedTex;
        }

        string path = Path.Combine(dirPath, fileName);
        if (File.Exists(path))
        {
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(bytes);

            // Salva no cache para a próxima vez
            avatarCache[fileName] = tex;
            return tex;
        }
        return null;
    }

    // Método utilitário para destruir texturas com segurança
    private void UnloadAvatarFromCache(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return;

        if (avatarCache.TryGetValue(fileName, out Texture2D tex))
        {
            if (tex != null) Destroy(tex);
            avatarCache.Remove(fileName);
        }
    }
}