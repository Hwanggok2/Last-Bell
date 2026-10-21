using System;
using System.IO;
using System.Text.Json;
using LastBell;

internal static class CatalogSmoke
{
    private static int Main(string[] args)
    {
        try
        {
            string path = args.Length == 0 ? "Client/Assets/Game/Data/Generated/mvp-catalog.json" : args[0];
            string json = File.ReadAllText(path);
            var options = new JsonSerializerOptions { IncludeFields = true };
            Func<GameCatalog> fresh = () => JsonSerializer.Deserialize<GameCatalog>(json, options);
            GameCatalog data = fresh();
            data.ValidateOrThrow();
            Check(data.GetCharacter("on").stars[0] == 3, "character ID lookup");
            Check(data.GetEnemy("gate_warden").artId == "gate_warden", "enemy ID lookup");
            Check(data.GetStage(10).maxHp == 706 && data.GetStage(11).damage == 40, "boss/boundary lookup");
            Check(data.GetUpgradeCost(11) == 187 && data.GetUpgradeCost(41) == 1203707, "target-level costs");
            Check(data.GetBaseGold(data.GetStage(5)) == 150 && data.GetBaseGold(data.GetStage(10)) == 750, "base gold");
            Check(data.GetFragmentReward(data.GetStage(5)) == 1 && data.GetFragmentReward(data.GetStage(10)) == 3, "fixed fragments");
            Check(data.GetMaxRelicGold(11) == 1388 && data.GetStunDurationMs("boss") == 1000, "zone and stun lookup");
            Check(Math.Abs(data.GetRelic("relic_05").EffectAtLevel(5) - .08f) < .00001f, "relic total effect");
            StageDefinition unused;
            Check(!data.TryGetStage(12, out unused), "out-of-scope boundary");
            long sum = 0;
            for (int i = 1; i <= 50; i++) sum += data.GetUpgradeCost(i);
            Check(sum == 42297564, "upgrade sum");
            GameCatalog bad = fresh();
            bad.stages[0].enemyId = "missing";
            Check(bad.Validate().Length > 0, "unknown enemy rejected");
            bad = fresh(); bad.relics[1].id = bad.relics[0].id;
            Check(bad.Validate().Length > 0, "duplicate relic rejected");
            bad = fresh(); bad.stages[4].patternSteps[1].intervalMs = 259;
            Check(bad.Validate().Length > 0, "short combo interval rejected");
            bad = fresh(); bad.economy.upgradeCosts = new long[0];
            Check(bad.Validate().Length > 0, "missing costs rejected");
            Check(new GameCatalog().Validate().Length > 0, "empty DTO rejected without null crash");
            Console.WriteLine("PASS: C# 7.3 DTO compile, field deserialization, lookups, economics, invalid-data checks. JsonUtility execution is verified separately in Unity.");
            return 0;
        }
        catch (Exception exception) { Console.Error.WriteLine(exception); return 1; }
    }
    private static void Check(bool condition, string name) { if (!condition) throw new Exception("FAIL: " + name); }
}
