using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace LastBell
{
    // Local MVP only. No paid purchases or server-authoritative balances live here.
    public sealed class JsonSaveRepository
    {
        [Serializable] private sealed class Envelope
        {
            public int envelopeVersion=1;
            public string payload;
            public string sha256;
        }
        private readonly string path;
        public string FilePath => path;
        public JsonSaveRepository(string directory)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            path="LastBell.Prototype.Save.v1";
#else
            Directory.CreateDirectory(directory);
            path=Path.Combine(directory,"last-bell-save.json");
#endif
        }

        private static bool Exists(string key)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return PlayerPrefs.HasKey(key);
#else
            return File.Exists(key);
#endif
        }

        public GameSaveData Load(out bool recoveredBackup)
        {
            recoveredBackup=false;
            if(!Exists(path)&&!Exists(path+".bak"))return null;
            Exception primaryError=null;
            if(Exists(path))try{return Read(path);}catch(Exception error){primaryError=error;}
            if(Exists(path+".bak"))try{var saved=Read(path+".bak");recoveredBackup=true;return saved;}catch(Exception){ }
            throw new InvalidDataException("저장 파일을 읽지 못했습니다. 원본을 보존했습니다.",primaryError);
        }

        private static GameSaveData Read(string filename)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            string encoded=PlayerPrefs.GetString(filename);
#else
            string encoded=File.ReadAllText(filename,Encoding.UTF8);
#endif
            var envelope=JsonUtility.FromJson<Envelope>(encoded);
            if(envelope==null||envelope.envelopeVersion!=1||string.IsNullOrEmpty(envelope.payload)||Hash(envelope.payload)!=envelope.sha256)
                throw new InvalidDataException("Save envelope/checksum mismatch");
            var value=JsonUtility.FromJson<GameSaveData>(envelope.payload);
            if(value==null||(value.schemaVersion!=1&&value.schemaVersion!=2&&value.schemaVersion!=3&&value.schemaVersion!=4&&value.schemaVersion!=5&&value.schemaVersion!=6&&value.schemaVersion!=7&&value.schemaVersion!=8&&value.schemaVersion!=9))throw new InvalidDataException("Unsupported save version");
            // Normalize the in-memory DTO only, after verifying the original payload.
            // Never rewrite the envelope/checksum or discard a partially populated record.
            value.NormalizeOptionalRecords();
            return value;
        }

        public void Save(GameSaveData value)
        {
            if(value==null||(value.schemaVersion!=1&&value.schemaVersion!=2&&value.schemaVersion!=3&&value.schemaVersion!=4&&value.schemaVersion!=5&&value.schemaVersion!=6&&value.schemaVersion!=7&&value.schemaVersion!=8&&value.schemaVersion!=9))throw new InvalidDataException("Unsupported save version");
            string payload=JsonUtility.ToJson(value);
            string encoded=JsonUtility.ToJson(new Envelope{payload=payload,sha256=Hash(payload)},true);
#if UNITY_WEBGL && !UNITY_EDITOR
            if(PlayerPrefs.HasKey(path))PlayerPrefs.SetString(path+".bak",PlayerPrefs.GetString(path));
            PlayerPrefs.SetString(path,encoded);PlayerPrefs.Save();
#else
            string temporary=path+".tmp";
            using(var stream=new FileStream(temporary,FileMode.Create,FileAccess.Write,FileShare.None))
            {
                byte[] bytes=Encoding.UTF8.GetBytes(encoded);stream.Write(bytes,0,bytes.Length);stream.Flush(true);
            }
            if(File.Exists(path))File.Replace(temporary,path,path+".bak");
            else File.Move(temporary,path);
#endif
        }

        private static string Hash(string value)
        {
            using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-","").ToLowerInvariant();
        }
    }
}
