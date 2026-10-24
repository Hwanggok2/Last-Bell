using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using LastBell;

internal static class Program
{
    private static readonly JsonSerializerOptions Json = new JsonSerializerOptions { IncludeFields = true };
    private static string catalogJson;
    private static int assertions;

    private static GameCatalog Catalog() { return JsonSerializer.Deserialize<GameCatalog>(catalogJson, Json); }
    private static GameSaveData RoundTrip(GameSaveData save) { return JsonSerializer.Deserialize<GameSaveData>(JsonSerializer.Serialize(save, Json), Json); }
    private static void Check(bool condition, string message) { assertions++; if (!condition) throw new Exception(message); }
    private static GameSaveData Lobby(GameCatalog data)
    {
        var save = new GameSession(data).ExportSave();
        save.phase = SessionPhase.Lobby;
        save.tutorialStep = 2; save.prologueSeen=true; save.comboHintSeen=true;
        save.run = null;
        return save;
    }
    private static GameSession Fighter(int relicLevel = 0)
    {
        var data = Catalog();
        var save = Lobby(data);
        save.characters[0].statLevels = new[] { 50, 50, 0, 0 };
        if (relicLevel > 0) foreach (var relic in data.relics) save.relics.Add(new OwnedRelicData { id = relic.id, level = relicLevel });
        var session = new GameSession(data, save);
        Check(session.StartRun(), "start prepared run");
        return session;
    }
    private static void Reach(GameSession session, int stage, SessionPhase phase)
    {
        for (int i = 0; i < 500 && !(session.Stage == stage && session.Phase == phase); i++)
        {
            if (session.Phase == SessionPhase.Transition) { session.Tick(10); continue; }
            if (session.Phase == SessionPhase.StoryDialogue) { session.SkipStory(); continue; }
            if (session.Phase == SessionPhase.RewardChoice) throw new Exception("unhandled earlier reward");
            Check(session.Phase == SessionPhase.Combat, "unexpected non-combat phase");
            session.Attack();
            session.Tick(.1f);
        }
        Check(session.Stage == stage && session.Phase == phase, "could not reach target " + stage + "/" + phase);
    }

    private static void GrowthAndCosts()
    {
        var data = Catalog(); var save = Lobby(data); save.gold = 20000000000L;
        var session = new GameSession(data, save);
        for (int stat = 0; stat < 4; stat++) for (int target = 1; target <= 50; target++)
        {
            long before = session.Gold;
            Check(session.GetUpgradeCost(stat) == data.economy.upgradeCosts[target], "target-level cost mismatch");
            Check(session.Upgrade(stat) && session.GetStatLevel(stat) == target, "upgrade failed");
            Check(before - session.Gold == data.economy.upgradeCosts[target], "wrong debit");
        }
        Check(session.Gold == 3284226944L, "all four stats must cost 16715773056, without Int32 truncation");
        Check(!session.Upgrade(0), "Lv51 must not be bought");
        int[] levels = { 9, 10, 11, 20, 21, 30, 31, 40, 41, 50 };
        int[] attacks = { 46, 50, 130, 216, 541, 749, 1852, 2350, 5773, 6967 };
        int[] hp = { 316, 340, 655, 1044, 1957, 2657, 4922, 6182, 11379, 13647 };
        for (int i = 0; i < levels.Length; i++)
        {
            save = Lobby(data); save.characters[0].statLevels = new[] { levels[i], levels[i], levels[i], levels[i] };
            session = new GameSession(data, save);
            Check(Math.Abs(session.PlayerStats.attack - attacks[i]) < .0051, "planning attack mismatch at Lv" + levels[i]);
            Check(session.PlayerStats.maxHpMilli == hp[i] * 1000L, "planning HP mismatch at Lv" + levels[i]);
        }
    }

    private static void ExchangeAndRestore()
    {
        int[] balances = { 0, 124, 125, 224 };
        int[] fragments = { 3, 2, 1, 1 };
        int[] remaining = { 75, 99, 0, 99 };
        for (int i = 0; i < balances.Length; i++)
        {
            var data = Catalog(); var save = Lobby(data);
            save.characters[0].statLevels[0] = 10; save.gold = balances[i]; save.fragments = 10;
            var session = new GameSession(data, save);
            session.Upgrade(0);
            Check(session.PendingExchange.fragments == fragments[i], "ceil quote mismatch");
            session = new GameSession(data, RoundTrip(session.ExportSave()));
            Check(session.ConfirmExchange(), "restored valid quote should require and accept explicit confirm");
            Check(session.GetStatLevel(0) == 10, "exchange must not upgrade");
            long confirmedGold = session.Gold;
            Check(!session.ConfirmExchange() && session.Gold == confirmedGold, "double confirm paid twice");
            session = new GameSession(data, RoundTrip(session.ExportSave()));
            Check(!session.ConfirmExchange() && session.Gold == confirmedGold, "committed exchange replayed after load");
            Check(session.Upgrade(0) && session.GetStatLevel(0) == 11 && session.Gold == remaining[i], "separate upgrade incorrect");
        }
        for (int scenario = 0; scenario < 4; scenario++)
        {
            var data = Catalog(); var save = Lobby(data); save.characters[0].statLevels[0] = 10; save.fragments = 2;
            var session = new GameSession(data, save); session.Upgrade(0);
            var pending = RoundTrip(session.ExportSave());
            if (scenario == 0) pending.gold = 1;
            if (scenario == 1) pending.fragments = 3;
            if (scenario == 2) pending.characters[0].statLevels[0] = 11;
            if (scenario == 3) data.economy.upgradeCosts[11] = 226;
            session = new GameSession(data, pending);
            Check(session.Phase == SessionPhase.Lobby && !session.ConfirmExchange(), "stale quote must be discarded " + scenario);
            Check(session.Gold == pending.gold && session.Fragments == pending.fragments, "stale quote spent currency");
        }
        {
            var data = Catalog(); var save = Lobby(data); save.characters[0].statLevels[0] = 10; save.fragments = 1;
            var session = new GameSession(data, save); session.Upgrade(0);
            Check(!session.ConfirmExchange(), "insufficient fragments accepted");
            Check(session.CancelExchange() && session.Gold == 0 && session.Fragments == 1, "cancel changed wallet");
        }
    }

    private static void RewardsAndCheckpoints()
    {
        var session = Fighter(); Reach(session, 6, SessionPhase.Combat);
        Check(session.Gold == 268 && session.Fragments == 1 && session.CheckpointStartStage == 1, "first five floor totals");
        Check(session.RelicChoices.Count == 0 && session.ExportSave().relics.Count == 0, "miniboss must not offer relic");
        Reach(session, 10, SessionPhase.RewardChoice);
        Check(session.Gold == 1374 && session.Fragments == 4 && session.CheckpointStartStage == 11, "boss prepayment/checkpoint timing");
        string[] choices = session.RelicChoices.ToArray();
        Check(choices.Distinct().Count() == 3, "candidate duplication");
        session = new GameSession(Catalog(), RoundTrip(session.ExportSave()));
        Check(session.RelicChoices.SequenceEqual(choices) && session.Gold == 1374 && session.Fragments == 4, "pending restore rerolled or paid twice");
        Check(!session.ReturnToLobby() && !session.StartRun(), "pending choice skipped");
        Check(session.ChooseRelic(choices[0]) && session.GetRelicLevel(choices[0]) == 1, "new relic was not granted");
        Check(!session.ChooseRelic(choices[0]) && session.Gold == 1374, "choice double applied");
        Check(session.Stage == 11 && session.CheckpointStartStage == 11 && session.Phase == SessionPhase.Transition && session.PlayerHp == session.PlayerMaxHp, "prototype completion/heal failed");
        Reach(session,20,SessionPhase.RewardChoice);
        long totalTwenty=Enumerable.Range(1,20).Sum(n=>Catalog().GetBaseGold(Catalog().GetStage(n)));
        Check(session.Gold==totalTwenty&&session.Fragments==8,"natural twenty-stage reward total");
        Check(session.ChooseRelic(session.RelicChoices[0])&&session.Phase==SessionPhase.MvpComplete,"twenty-stage completion");
        session=Fighter();Reach(session,10,SessionPhase.RewardChoice);session.ChooseRelic(session.RelicChoices[0]);
        session = new GameSession(Catalog(), RoundTrip(session.ExportSave()));
        Check(!session.ChooseRelic(choices[0]) && session.Gold == 1374 && session.Fragments == 4, "committed boss reward replayed");
        Check(session.ReturnToLobby() && session.StartRun() && session.Stage == 11, "gate checkpoint restart failed");

        session = Fighter(); Reach(session, 6, SessionPhase.Combat);
        int runId=session.ExportSave().run.runId;
        Check(session.ReturnToLobby() && session.StartRun(), "repeat run failed");
        Reach(session, 6, SessionPhase.Combat);
        Check(session.Gold == 536 && session.Fragments == 2 && session.ExportSave().run.runId != runId, "valid repeated kill denied or conflated");

        foreach (int level in new[] { 1, 4, 5 })
        {
            session = Fighter(level); Reach(session, 10, SessionPhase.RewardChoice);
            string id = session.RelicChoices[0]; session.ChooseRelic(id);
            Check(session.GetRelicLevel(id) == Math.Min(5, level + 1), "duplicate level incorrect");
            Check(session.Gold == (level == 5 ? 1449 : 1374), "ordinary duplicate/MAX gold incorrect");
        }

        session = Fighter(); Reach(session, 9, SessionPhase.Combat);
        var dying = session.ExportSave(); dying.run.playerHpMilli = 1000;
        session = new GameSession(Catalog(), RoundTrip(dying)); session.SetPaused(false);
        session.Tick(session.EnemyIntent.SecondsToHit + .001f);
        Check(session.Phase == SessionPhase.Transition && session.Transition.Kind == TransitionKind.Death, "death must show lantern transition");
        session.Tick(1.8f);
        Check(session.Phase == SessionPhase.DeathResult && session.Result.DefeatedStage == 9 && session.Result.RetryStage == 1, "death9 result lost the defeated stage or checkpoint");
        Check(session.RetryRun() && session.Stage == 1, "restart after death9 failed");
    }

    private static int Main()
    {
        try
        {
            catalogJson = File.ReadAllText("Client/Assets/Game/Data/Generated/mvp-catalog.json");
            GrowthAndCosts(); ExchangeAndRestore(); RewardsAndCheckpoints();
            Console.WriteLine("PASS: independent catalog/session contract review; " + assertions + " assertions. Growth0..50, exchange/restore/stale quotes, natural1..20 rewards, MAX, repeat kills, death9/restart1, story10/clear20/restart11.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
