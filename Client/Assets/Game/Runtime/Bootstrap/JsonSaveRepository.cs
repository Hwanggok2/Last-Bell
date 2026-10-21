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
            Directory.CreateDirectory(directory);
            path=Path.Combine(directory,"last-bell-save.json");
        }

        public GameSaveData Load(out bool recoveredBackup)
        {
            recoveredBackup=false;
            if(!File.Exists(path)&&!File.Exists(path+".bak"))return null;
            Exception primaryError=null;
            if(File.Exists(path))try{return Read(path);}catch(Exception error){primaryError=error;}
            if(File.Exists(path+".bak"))try{var saved=Read(path+".bak");recoveredBackup=true;return saved;}catch(Exception){ }
            throw new InvalidDataException("저장 파일을 읽지 못했습니다. 원본을 보존했습니다.",primaryError);
        }

        private static GameSaveData Read(string filename)
        {
            var envelope=JsonUtility.FromJson<Envelope>(File.ReadAllText(filename,Encoding.UTF8));
            if(envelope==null||envelope.envelopeVersion!=1||string.IsNullOrEmpty(envelope.payload)||Hash(envelope.payload)!=envelope.sha256)
                throw new InvalidDataException("Save envelope/checksum mismatch");
            var value=JsonUtility.FromJson<GameSaveData>(envelope.payload);
            if(value==null||value.schemaVersion!=1)throw new InvalidDataException("Unsupported save version");
            return value;
        }

        public void Save(GameSaveData value)
        {
            if(value==null||value.schemaVersion!=1)throw new InvalidDataException("Unsupported save version");
            string payload=JsonUtility.ToJson(value);
            string encoded=JsonUtility.ToJson(new Envelope{payload=payload,sha256=Hash(payload)},true);
            string temporary=path+".tmp";
            using(var stream=new FileStream(temporary,FileMode.Create,FileAccess.Write,FileShare.None))
            {
                byte[] bytes=Encoding.UTF8.GetBytes(encoded);stream.Write(bytes,0,bytes.Length);stream.Flush(true);
            }
            if(File.Exists(path))File.Replace(temporary,path,path+".bak");
            else File.Move(temporary,path);
        }

        private static string Hash(string value)
        {
            using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-","").ToLowerInvariant();
        }
    }
}
