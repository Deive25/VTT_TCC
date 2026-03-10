// ============================================================
// CharacterManager.cs
// Sistema de Save/Load local usando JSON e ficheiros PNG.
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
    public string id;
    public string system;
    public string name;
    public string subText;
    public string statsStr;
    public string avatarFileName;

    // Lista que guarda TODOS os inputs preenchidos (Status, Inventário, etc)
    public List<CharField> fields = new List<CharField>();
}

[Serializable]
public class CharacterDB
{
    public List<CharacterRecord> records = new List<CharacterRecord>();
}

public class CharacterManager : MonoBehaviour
{
    public static CharacterManager Instance { get; private set; }

    public CharacterDB Database = new CharacterDB();
    private string dirPath;
    private string dbPath;

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); return; }

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
    }

    // --- ATUALIZADO: Gere Criações e Edições Inteligentes ---
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
                        string fullPath = Path.Combine(dirPath, fileName);
                        File.WriteAllBytes(fullPath, newAvatar.EncodeToPNG());
                        record.avatarFileName = fileName;
                    }
                    else
                    {
                        record.avatarFileName = "";
                    }

                    // Apaga foto velha para libertar espaço
                    if (!string.IsNullOrEmpty(existing.avatarFileName))
                    {
                        string oldPath = Path.Combine(dirPath, existing.avatarFileName);
                        if (File.Exists(oldPath)) File.Delete(oldPath);
                    }
                }
                else
                {
                    record.avatarFileName = existing.avatarFileName; // Mantém a foto antiga
                }
                Database.records.Remove(existing); // Remove o Registo antigo
            }
        }
        else // Criação Nova
        {
            if (newAvatar != null)
            {
                string fileName = "avatar_" + record.id + ".png";
                string fullPath = Path.Combine(dirPath, fileName);
                File.WriteAllBytes(fullPath, newAvatar.EncodeToPNG());
                record.avatarFileName = fileName;
            }
        }

        Database.records.Add(record);
        SaveDatabase();
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
            Database.records.Remove(record);
            SaveDatabase();
        }
    }

    public Texture2D LoadAvatar(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return null;
        string path = Path.Combine(dirPath, fileName);
        if (File.Exists(path))
        {
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(bytes);
            return tex;
        }
        return null;
    }
}