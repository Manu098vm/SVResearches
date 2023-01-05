using pkNX.Structures;
using pkNX.Structures.FlatBuffers;
using System.Buffers.Binary;
using System.Diagnostics;

namespace RaidParser;

public static class Program
{
    public static void Main()
    {
        if (Environment.GetCommandLineArgs().Length == 2)
            try {
                DumpDistributionRaids(Environment.GetCommandLineArgs()[1]);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        else
            Console.WriteLine($"Drag and drop the event \"files\" folder into the .exe.\n" +
                $"The files folder must contain the following files:\n" +
                $"- fixed_reward_item_array\n" +
                $"- lottery_reward_item_array\n" +
                $"- raid_enemy_array\n" +
                $"- raid_priority_array");

        Console.WriteLine("Process finished. Press any key to exit.");
        Console.ReadKey();
    }

    private static readonly int[][] StageStars =
    {
        new [] { 1, 2 },
        new [] { 1, 2, 3 },
        new [] { 1, 2, 3, 4 },
        new [] { 3, 4, 5 },
    };

    public static void DumpDistributionRaids(string path)
    {
        Console.WriteLine("Processing...");
        var list = new List<byte[]>();
        if (path.Contains("files"))
        {
            var newpath = path.Replace("files", "Files");
            Directory.Move(path, newpath);
            path = newpath;
        }
        DumpDistributionRaids(path, list);
    }

    private static void DumpDistributionRaids(string path, List<byte[]> list)
    {
        var dataEncounters = GetDistributionContents(Path.Combine(path, "raid_enemy_array"), out int indexEncounters);
        var dataDrop = GetDistributionContents(Path.Combine(path, "fixed_reward_item_array"), out int indexDrop);
        var dataBonus = GetDistributionContents(Path.Combine(path, "lottery_reward_item_array"), out int indexBonus);
        var priority = GetDistributionContents(Path.Combine(path, "raid_priority_array"), out int indexPriority);

        var tableEncounters = FlatBufferConverter.DeserializeFrom<DeliveryRaidEnemyTableArray>(dataEncounters);
        var tableDrops = FlatBufferConverter.DeserializeFrom<DeliveryRaidFixedRewardItemArray>(dataDrop);
        var tableBonus = FlatBufferConverter.DeserializeFrom<DeliveryRaidLotteryRewardItemArray>(dataBonus);
        var tablePriority = FlatBufferConverter.DeserializeFrom<DeliveryRaidPriorityArray>(priority);
        var index = tablePriority.Table[0].VersionNo;

        AddToList(tableEncounters.Table, list);

        var dirDistText = Path.Combine(path, "..\\Json");
        ExportParse(dirDistText, tableEncounters, tableDrops, tableBonus, tablePriority);
        ExportIdentifierBlock(index, path);
    }

    private static void ExportIdentifierBlock(int index, string path)
    {
        var data = BitConverter.GetBytes((uint)index);
        File.WriteAllBytes($"{path}\\event_raid_identifier", data);
        File.WriteAllText($"{path}\\..\\Identifier.txt", $"{index}");
    }

    private static void AddToList(IReadOnlyCollection<DeliveryRaidEnemyTable> table, List<byte[]> list)
    {
        // Get the total weight for each stage of star count
        Span<ushort> weightTotalS = stackalloc ushort[StageStars.Length];
        Span<ushort> weightTotalV = stackalloc ushort[StageStars.Length];
        foreach (var enc in table)
        {
            var info = enc.RaidEnemyInfo;
            if (info.Rate == 0)
                continue;
            var difficulty = info.Difficulty;
            for (int stage = 0; stage < StageStars.Length; stage++)
            {
                if (!StageStars[stage].Contains(difficulty))
                    continue;
                if (info.RomVer != RaidRomType.TYPE_B)
                    weightTotalS[stage] += (ushort)info.Rate;
                if (info.RomVer != RaidRomType.TYPE_A)
                    weightTotalV[stage] += (ushort)info.Rate;
            }
        }

        Span<ushort> weightMinS = stackalloc ushort[StageStars.Length];
        Span<ushort> weightMinV = stackalloc ushort[StageStars.Length];
        foreach (var enc in table)
        {
            var info = enc.RaidEnemyInfo;
            if (info.Rate == 0)
                continue;
            var difficulty = info.Difficulty;
            for (int stage = 0; stage < StageStars.Length; stage++)
            {
                if (!StageStars[stage].Contains(difficulty))
                    continue;
                if (info.RomVer != RaidRomType.TYPE_B)
                    weightMinS[stage] += (ushort)info.Rate;
                if (info.RomVer != RaidRomType.TYPE_A)
                    weightMinV[stage] += (ushort)info.Rate;
            }
        }
    }

    private static void ExportParse(string dir,
        DeliveryRaidEnemyTableArray tableEncounters,
        DeliveryRaidFixedRewardItemArray tableDrops,
        DeliveryRaidLotteryRewardItemArray tableBonus,
        DeliveryRaidPriorityArray tablePriority)
    {
        var dumpE = TableUtil.GetTable(tableEncounters.Table);
        var dumpEnc = TableUtil.GetTable(tableEncounters.Table.Select(z => z.RaidEnemyInfo.BossPokePara));
        var dumpRate = TableUtil.GetTable(tableEncounters.Table.Select(z => z.RaidEnemyInfo));
        var dumpSize = TableUtil.GetTable(tableEncounters.Table.Select(z => z.RaidEnemyInfo.BossPokeSize));
        var dumpD = TableUtil.GetTable(tableDrops.Table);
        var dumpB = TableUtil.GetTable(tableBonus.Table);
        var dumpP = TableUtil.GetTable(tablePriority.Table);
        var dumpP_2 = TableUtil.GetTable(tablePriority.Table.Select(z => z.DeliveryGroupID));
        var dump = new[]
        {
            ("encounters", dumpE),
            ("encounters_poke", dumpEnc),
            ("encounters_rate", dumpRate),
            ("encounters_size", dumpSize),
            ("drops", dumpD),
            ("bonus", dumpB),
            ("priority", dumpP),
            ("priority_alt", dumpP_2),
        };

        Directory.CreateDirectory(dir);

        DumpJson(tableEncounters, dir, "raid_enemy_array");
        DumpJson(tableDrops, dir, "fixed_reward_item_array");
        DumpJson(tableBonus, dir, "lottery_reward_item_array");
        DumpJson(tablePriority, dir, "raid_priority_array");
    }

    private static void DumpJson(object flat, string dir, string name)
    {
        var opt = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
        var json = System.Text.Json.JsonSerializer.Serialize(flat, opt);

        var fileName = Path.ChangeExtension(name, ".json");
        File.WriteAllText(Path.Combine(dir, fileName), json);
    }

    private static byte[] GetDistributionContents(string path, out int index)
    {
        index = 0; //  todo
        return File.ReadAllBytes(path);
    }
}
