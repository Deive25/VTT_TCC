// ============================================================
// CharacterManager.cs
// Sistema de Save/Load local usando JSON e ficheiros PNG.
// Atualização em Tempo Real das Estatísticas do Token.
// ============================================================
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;

[Serializable]
public class CharField { public string key; public string value; }

[Serializable]
public class CharacterRecord
{
    public string id; public string system; public string name; public string subText;
    public string statsStr; public string avatarFileName;
    public List<CharField> fields = new List<CharField>();
}

[Serializable]
public class CharacterDB { public List<CharacterRecord> records = new List<CharacterRecord>(); }

public class CharacterManager : MonoBehaviour
{
    public static CharacterManager Instance { get; private set; }
    public CharacterDB Database = new CharacterDB();
    private string dirPath; private string dbPath;

    public event Action OnCharactersUpdated; // Aviso de alteração

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
    }

    public void SaveDatabase()
    {
        string json = JsonUtility.ToJson(Database, true);
        File.WriteAllText(dbPath, json);
        OnCharactersUpdated?.Invoke();
    }

    public CharacterRecord GetCharacter(string id) => Database.records.Find(r => r.id == id);

    // EDITA APENAS UM CAMPO (Ex: HP no Token) E RECALCULA O PAINEL
    public void UpdateCharacterField(string id, string key, string value)
    {
        var record = GetCharacter(id);
        if (record != null)
        {
            var field = record.fields.Find(f => f.key == key);
            if (field != null) field.value = value;
            else record.fields.Add(new CharField { key = key, value = value });

            RebuildStatsStr(record);
            SaveDatabase();
        }
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

    public void SaveCharacter(CharacterRecord record, Texture2D newAvatar, bool isEdit, bool avatarChanged)
    {
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
                    }
                    else record.avatarFileName = "";
                    if (!string.IsNullOrEmpty(existing.avatarFileName))
                    {
                        string oldPath = Path.Combine(dirPath, existing.avatarFileName);
                        if (File.Exists(oldPath)) File.Delete(oldPath);
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
            }
            Database.records.Remove(record); SaveDatabase();
        }
    }

    public Texture2D LoadAvatar(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return null;
        string path = Path.Combine(dirPath, fileName);
        if (File.Exists(path))
        {
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2); tex.LoadImage(bytes);
            return tex;
        }
        return null;
    }
}