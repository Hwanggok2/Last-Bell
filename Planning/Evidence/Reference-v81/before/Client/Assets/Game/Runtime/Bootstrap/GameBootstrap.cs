using System;
using System.IO;
using UnityEngine;

namespace LastBell
{
    [DefaultExecutionOrder(-100)]
    public sealed class GameBootstrap : MonoBehaviour
    {
        public TextAsset CatalogJson;
        public MvpPresenter Presenter;
        public GameSession Session { get; private set; }
        public string SavePath => repository?.FilePath;
        public string LastStorageError { get; private set; }
        private JsonSaveRepository repository;
        private float nextAutosave;
        private bool storageBlocked;

        void Start() { Initialize(); }

        public void Initialize()
        {
            try
            {
                if(CatalogJson==null||Presenter==null)throw new InvalidOperationException("씬의 카탈로그 또는 화면 참조가 비어 있습니다.");
                if(Session!=null)Session.SaveRequested-=Save;
                var data=JsonUtility.FromJson<GameCatalog>(CatalogJson.text);
                data.ValidateOrThrow();
                string directory=Application.persistentDataPath;
#if !UNITY_WEBGL || UNITY_EDITOR
                var args=Environment.GetCommandLineArgs();
                for(int i=0;i+1<args.Length;i++)if(args[i]=="-lastbell-save-dir")directory=Path.GetFullPath(args[i+1]);
#endif
                repository=new JsonSaveRepository(directory);
                var saved=repository.Load(out bool recovered);
                Session=new GameSession(data,saved);
                Session.SaveRequested+=Save;
                storageBlocked=false;LastStorageError=null;
                Presenter.ClearBlockingMessage();Presenter.Bind(Session);
                Save();
                if(recovered)Debug.LogWarning("LAST_BELL_SAVE_BACKUP_RECOVERED: primary kept until verified backup was loaded.");
                Debug.Log("LAST_BELL_READY: "+Session.Phase+" / Stage "+Session.Stage);
            }
            catch(Exception error)
            {
                Debug.LogException(error);LastStorageError=error.Message;storageBlocked=true;
                if(Presenter)Presenter.ShowBlockingMessage("저장을 불러오지 못했어요","기존 기록은 보존했습니다.\n다시 시도해 주세요.",Initialize);
            }
        }

        void Update()
        {
            if(storageBlocked||Session==null)return;
            nextAutosave-=Time.unscaledDeltaTime;
            if(nextAutosave<=0&&Session.IsDirty)Save();
        }

        public void Save()
        {
            if(Session==null||repository==null)return;
            try
            {
                repository.Save(Session.ExportSave());Session.MarkSaved();nextAutosave=2f;
                if(storageBlocked){storageBlocked=false;LastStorageError=null;Presenter.ClearBlockingMessage();}
            }
            catch(Exception error)
            {
                storageBlocked=true;LastStorageError=error.Message;Session.SetPaused(true);
                Debug.LogException(error);
                Presenter.ShowBlockingMessage("저장하지 못했어요","진행을 멈췄습니다.\n다시 시도해 주세요.",Save);
            }
        }

        void OnApplicationPause(bool paused)
        {
            if(!paused||Session==null)return;
            if(Session.Phase==SessionPhase.ComboHint){Save();return;}
            if(Session.Phase==SessionPhase.Prologue){if(!Session.ProloguePaused)Session.ToggleProloguePause();Save();return;}
            Session.SetPaused(true);Save();
        }
        void OnApplicationQuit(){Save();}
        void OnDestroy(){if(Session!=null)Session.SaveRequested-=Save;}
    }
}
