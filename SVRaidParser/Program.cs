using pkNX.Structures;
using pkNX.Structures.FlatBuffers;
using System.Diagnostics;
using RaidParser.Properties;

namespace RaidParser;

public static class Program
{
    public static void Main()
    {
        if (Environment.GetCommandLineArgs().Length == 2 || Environment.GetCommandLineArgs().Length == 3)
        {
            try
            {
                if (Environment.GetCommandLineArgs().Length == 3 && (Environment.GetCommandLineArgs()[2].Equals("-n") || Environment.GetCommandLineArgs()[2].Equals("--null")))
                    DumpDistributionRaids(Environment.GetCommandLineArgs()[1], true);
                else
                    DumpDistributionRaids(Environment.GetCommandLineArgs()[1], false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
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

    public static void DumpDistributionRaids(string path, bool parsenull)
    {
        Console.WriteLine("Processing...");
        var type2 = new List<byte[]>();
        var type3 = new List<byte[]>();

        if (path.Contains("files"))
        {
            var newpath = path.Replace("files", "Files");
            Directory.Move(path, newpath);
            path = newpath;
        }

        if (path.Contains("null"))
            parsenull = true;

        DumpDistributionRaids(path, type2, type3, parsenull);
    }

    private static void DumpDistributionRaids(string path, List<byte[]> type2, List<byte[]> type3, bool parsenull)
    {
        var dataEncounters = GetDistributionContents(Path.Combine(path, "raid_enemy_array"), out int indexEncounters);
        var dataDrop = GetDistributionContents(Path.Combine(path, "fixed_reward_item_array"), out int indexDrop);
        var dataBonus = GetDistributionContents(Path.Combine(path, "lottery_reward_item_array"), out int indexBonus);
        var priority = GetDistributionContents(Path.Combine(path, "raid_priority_array"), out int indexPriority);

        // BCAT Indexes can be reused by mixing and matching old files when reverting temporary distributions back to prior long-running distributions.
        // They don't have to match, but just note if they do.
        Debug.WriteLineIf(indexEncounters == indexDrop && indexDrop == indexBonus && indexBonus == indexPriority,
            $"Info: BCAT indexes are inconsistent! enc:{indexEncounters} drop:{indexDrop} bonus:{indexBonus} priority:{indexPriority}");

        var tableEncounters = FlatBufferConverter.DeserializeFrom<DeliveryRaidEnemyTableArray>(dataEncounters);
        var tableDrops = FlatBufferConverter.DeserializeFrom<DeliveryRaidFixedRewardItemArray>(dataDrop);
        var tableBonus = FlatBufferConverter.DeserializeFrom<DeliveryRaidLotteryRewardItemArray>(dataBonus);
        var tablePriority = FlatBufferConverter.DeserializeFrom<DeliveryRaidPriorityArray>(priority);
        var index = tablePriority.Table[0].VersionNo;

        var byGroupID = tableEncounters.Table
            .Where(z => z.RaidEnemyInfo.Rate != 0)
            .GroupBy(z => z.RaidEnemyInfo.DeliveryGroupID);

        var seven = DistroGroupSet.None;
        var other = DistroGroupSet.None;

        foreach (var group in byGroupID)
        {
            var items = group.ToArray();
            var groupSet = Evaluate(items);

            if (items.Any(z => z.RaidEnemyInfo.Difficulty > 7))
                throw new Exception($"Undocumented difficulty {items.First(z => z.RaidEnemyInfo.Difficulty > 7).RaidEnemyInfo.Difficulty}");

            if (items.All(z => z.RaidEnemyInfo.Difficulty == 7))
            {
                if (items.Any(z => z.RaidEnemyInfo.CaptureRate != 2))
                    throw new Exception($"Undocumented 7 star capture rate {items.First(z => z.RaidEnemyInfo.CaptureRate != 2).RaidEnemyInfo.CaptureRate}");

                if (!TryAdd(ref seven, groupSet))
                    Console.WriteLine("Already saw a 7-star group. How do we differentiate this slot determination from prior?");

                AddToList(items, type3, RaidSerializationFormat.Type3);
                continue;
            }

            if (items.Any(z => z.RaidEnemyInfo.Difficulty == 7))
                throw new Exception($"Mixed difficulty {items.First(z => z.RaidEnemyInfo.Difficulty > 7).RaidEnemyInfo.Difficulty}");

            if (!TryAdd(ref other, groupSet))
                Console.WriteLine("Already saw a not-7-star group. How do we differentiate this slot determination from prior?");

            AddToList(items, type2, RaidSerializationFormat.Type2);
        }

        var dirDistText = Path.Combine(path, "../Json");
        ExportParse(dirDistText, tableEncounters, tableDrops, tableBonus, tablePriority, parsenull);
        ExportIdentifierBlock(index, path);
    }

    private static bool TryAdd(ref DistroGroupSet exist, DistroGroupSet add)
    {
        if ((exist & add) != 0)
            return false;
        exist |= add;
        return true;
    }

    [Flags]
    private enum DistroGroupSet
    {
        None = 0,
        SL = 1,
        VL = 2,
        Both = SL | VL,
    }

    private static DistroGroupSet Evaluate(DeliveryRaidEnemyTable[] items)
    {
        var versions = items.Select(z => z.RaidEnemyInfo.RomVer).Distinct().ToArray();
        if (versions.Length == 2 && versions.Contains(RaidRomType.TYPE_A) && versions.Contains(RaidRomType.TYPE_B))
            return DistroGroupSet.Both;
        if (versions.Length == 1)
        {
            return versions[0] switch
            {
                RaidRomType.BOTH => DistroGroupSet.Both,
                RaidRomType.TYPE_A => DistroGroupSet.SL,
                RaidRomType.TYPE_B => DistroGroupSet.VL,
                _ => throw new Exception("Unknown type."),
            };
        }
        throw new Exception("Unknown version");
    }

    private static void ExportIdentifierBlock(int index, string path)
    {
        var data = BitConverter.GetBytes((uint)index);
        File.WriteAllBytes($"{path}\\event_raid_identifier", data);
        File.WriteAllText($"{path}\\..\\Identifier.txt", $"{index}");
    }

    private static void AddToList(IReadOnlyCollection<DeliveryRaidEnemyTable> table, List<byte[]> list, RaidSerializationFormat format)
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
            TryAddToPickle(info, list, format, weightTotalS, weightTotalV, weightMinS, weightMinV);
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

    private static void TryAddToPickle(RaidEnemyInfo enc, ICollection<byte[]> list, RaidSerializationFormat format,
    ReadOnlySpan<ushort> totalS, ReadOnlySpan<ushort> totalV, ReadOnlySpan<ushort> minS, ReadOnlySpan<ushort> minV)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        enc.SerializePKHeX(bw, (byte)enc.Difficulty, enc.Rate, format);
        for (int stage = 0; stage < StageStars.Length; stage++)
        {
            bool noTotal = !StageStars[stage].Contains(enc.Difficulty);
            ushort mS = minS[stage];
            ushort mV = minV[stage];
            bw.Write(noTotal ? (ushort)0 : mS);
            bw.Write(noTotal ? (ushort)0 : mV);
            bw.Write(noTotal || enc.RomVer is RaidRomType.TYPE_B ? (ushort)0 : totalS[stage]);
            bw.Write(noTotal || enc.RomVer is RaidRomType.TYPE_A ? (ushort)0 : totalV[stage]);
        }

        if (format == RaidSerializationFormat.Type2)
            enc.SerializeType2(bw);

        if (format == RaidSerializationFormat.Type3)
            enc.SerializeType3(bw);

        var bin = ms.ToArray();
        if (!list.Any(z => z.SequenceEqual(bin)))
            list.Add(bin);
    }

    private static void ExportParse(string dir,
        DeliveryRaidEnemyTableArray tableEncounters,
        DeliveryRaidFixedRewardItemArray tableDrops,
        DeliveryRaidLotteryRewardItemArray tableBonus,
        DeliveryRaidPriorityArray tablePriority,
        bool parsenull)
    {
        Directory.CreateDirectory(dir);

        if(!parsenull)
            tableEncounters.RemoveEmptyEntries();

        DumpJson(tableEncounters, dir, "raid_enemy_array");
        DumpJson(tableDrops, dir, "fixed_reward_item_array");
        DumpJson(tableBonus, dir, "lottery_reward_item_array");
        DumpJson(tablePriority, dir, "raid_priority_array");
        DumpPretty(tableEncounters, tableDrops, tableBonus, dir);
    }

    private static void DumpPretty(DeliveryRaidEnemyTableArray tableEncounters, DeliveryRaidFixedRewardItemArray tableDrops, DeliveryRaidLotteryRewardItemArray tableBonus, string dir)
    {
        var cfg = new TextConfig(GameVersion.SV);
        var lines = new List<string>();
        var ident = tableEncounters.Table[0].RaidEnemyInfo.No;

        var species = GetCommonText("monsname", cfg);
        var items = GetCommonText("itemname", cfg);
        var moves = GetCommonText("wazaname", cfg);
        var types = GetCommonText("typename", cfg);
        var natures = GetCommonText("seikaku", cfg);

        lines.Add($"Event Raid Identifier: {ident}");

        foreach (var entry in tableEncounters.Table)
        {
            var boss = entry.RaidEnemyInfo.BossPokePara;
            var extra = entry.RaidEnemyInfo.BossDesc;
            var nameDrop = entry.RaidEnemyInfo.DropTableFix;
            var nameBonus = entry.RaidEnemyInfo.DropTableRandom;

            if (boss.DevId == DevID.DEV_NULL)
                continue;

            var version = entry.RaidEnemyInfo.RomVer switch
            {
                RaidRomType.TYPE_A => "Scarlet",
                RaidRomType.TYPE_B => "Violet",
                _ => string.Empty,
            };

            var gem = boss.GemType switch
            {
                GemType.DEFAULT => "Default",
                GemType.RANDOM => "Random",
                _ => $"{types[(int)boss.GemType - 2]}",
            };

            var ability = boss.Tokusei switch
            {
                TokuseiType.SET_1 => "1 Only",
                TokuseiType.SET_2 => "2 Only",
                TokuseiType.SET_3 => "Hidden Only",
                TokuseiType.RANDOM_12 => "1/2",
                _ => "1/2/H",
            };

            var shiny = boss.RareType switch
            {
                RareType.RARE => "Always",
                RareType.NO_RARE => "Never",
                _ => string.Empty,
            };

            var talent = boss.TalentValue;
            var iv = boss.TalentType switch
            {
                TalentType.VALUE when talent.HP == 31 && talent.ATK == 31 && talent.DEF == 31 && talent.SPA == 31 && talent.SPD == 31 && talent.SPE == 31 => "6 Flawless",
                TalentType.VALUE => $"{boss.TalentValue.HP}/{boss.TalentValue.ATK}/{boss.TalentValue.DEF}/{boss.TalentValue.SPA}/{boss.TalentValue.SPD}/{boss.TalentValue.SPE}",
                _ => $"{boss.TalentVnum} Flawless",
            };

            var capture = entry.RaidEnemyInfo.CaptureRate switch
            {
                // 0 never?
                // 1 always
                2 => "Only Once",
                _ => $"{entry.RaidEnemyInfo.CaptureRate}",
            };

            var form = boss.FormId == 0 ? string.Empty : $"-{(int)boss.FormId}";

            lines.Add($"{entry.RaidEnemyInfo.Difficulty}-Star {species[(int)boss.DevId]}{form}");
            if (entry.RaidEnemyInfo.RomVer != RaidRomType.BOTH)
                lines.Add($"\tVersion: {version}");

            lines.Add($"\tTera Type: {gem}");
            lines.Add($"\tCapture Level: {entry.RaidEnemyInfo.CaptureLv}");
            lines.Add($"\tAbility: {ability}");

            if (boss.Seikaku != SeikakuType.DEFAULT)
                lines.Add($"\tNature: {natures[(int)boss.Seikaku - 1]}");

            lines.Add($"\tIVs: {iv}");

            if (boss.RareType != RareType.DEFAULT)
                lines.Add($"\tShiny: {shiny}");

            if (boss.Item != ItemID.ITEMID_NONE)
                lines.Add($"\tHeld Item: {items[(int)boss.Item]}");

            if (entry.RaidEnemyInfo.CaptureRate != 1)
                lines.Add($"\tCatchable: {capture}");

            lines.Add($"\t\tMoves:");
            lines.Add($"\t\t\t- {moves[(int)boss.Waza1.WazaId]}");
            if ((int)boss.Waza2.WazaId != 0) lines.Add($"\t\t\t- {moves[(int)boss.Waza2.WazaId]}");
            if ((int)boss.Waza3.WazaId != 0) lines.Add($"\t\t\t- {moves[(int)boss.Waza3.WazaId]}");
            if ((int)boss.Waza4.WazaId != 0) lines.Add($"\t\t\t- {moves[(int)boss.Waza4.WazaId]}");

            lines.Add($"\t\tExtra Moves:");

            if ((int)extra.ExtraAction1.Wazano == 0 && (int)extra.ExtraAction2.Wazano == 0 && (int)extra.ExtraAction3.Wazano == 0 && (int)extra.ExtraAction4.Wazano == 0 && (int)extra.ExtraAction5.Wazano == 0 && (int)extra.ExtraAction6.Wazano == 0)
            {
                lines.Add("\t\t\tNone!");
            }

            else
            {
                if ((int)extra.ExtraAction1.Wazano != 0) lines.Add($"\t\t\t- {moves[(int)extra.ExtraAction1.Wazano]}");
                if ((int)extra.ExtraAction2.Wazano != 0) lines.Add($"\t\t\t- {moves[(int)extra.ExtraAction2.Wazano]}");
                if ((int)extra.ExtraAction3.Wazano != 0) lines.Add($"\t\t\t- {moves[(int)extra.ExtraAction3.Wazano]}");
                if ((int)extra.ExtraAction4.Wazano != 0) lines.Add($"\t\t\t- {moves[(int)extra.ExtraAction4.Wazano]}");
                if ((int)extra.ExtraAction5.Wazano != 0) lines.Add($"\t\t\t- {moves[(int)extra.ExtraAction5.Wazano]}");
                if ((int)extra.ExtraAction6.Wazano != 0) lines.Add($"\t\t\t- {moves[(int)extra.ExtraAction6.Wazano]}");
            }

            lines.Add("\t\tItem Drops:");

            foreach (var item in tableDrops.Table.Where(z => z.TableName == nameDrop))
            {
                const int count = RaidFixedRewardItem.Count;
                for (int i = 0; i < count; i++)
                {
                    if (nameDrop != item.TableName)
                        continue;

                    var drop = item.GetReward(i);
                    var limitation = drop.SubjectType switch
                    {
                        RaidRewardItemSubjectType.HOST => " (Only Host)",
                        RaidRewardItemSubjectType.CLIENT => " (Only Guests)",
                        RaidRewardItemSubjectType.ONCE => " (Only Once)",
                        _ => string.Empty,
                    };

                    if (drop.Category == RaidRewardItemCategoryType.POKE) // Material
                        lines.Add($"\t\t\t{drop.Num,2} × Crafting Material{limitation}");

                    if (drop.Category == RaidRewardItemCategoryType.GEM) // Material
                        lines.Add($"\t\t\t{drop.Num,2} × Tera Shard{limitation}");

                    if (drop.ItemID != 0)
                        lines.Add($"\t\t\t{drop.Num,2} × {GetItemName((ushort)drop.ItemID, items, moves)}{limitation}");
                }
            }

            lines.Add("\t\tBonus Drops:");

            foreach (var item in tableBonus.Table.Where(z => z.TableName == nameBonus))
            {
                const int count = RaidLotteryRewardItem.RewardItemCount;
                float totalRate = 0;
                for (int i = 0; i < count; i++)
                    totalRate += item.GetRewardItem(i).Rate;

                for (int i = 0; i < count; i++)
                {
                    if (nameBonus != item.TableName)
                        continue;

                    var drop = item.GetRewardItem(i);
                    float rate = (float)(Math.Round((item.GetRewardItem(i).Rate / totalRate) * 100f, 2));

                    if (drop.Category == RaidRewardItemCategoryType.POKE) // Material
                        lines.Add($"\t\t\t{rate,5}% {drop.Num,2} × Crafting Material");

                    if (drop.Category == RaidRewardItemCategoryType.GEM) // Tera Shard
                        lines.Add($"\t\t\t{rate,5}% {drop.Num,2} × Tera Shard");

                    if (drop.ItemID != 0)
                        lines.Add($"\t\t\t{rate,5}% {drop.Num,2} × {items[(ushort)drop.ItemID]}");
                }
            }

            lines.Add("");
        }

        File.WriteAllLines(Path.Combine(dir, $"../Encounters.txt"), lines);
    }

    private static void RemoveEmptyEntries(this DeliveryRaidEnemyTableArray encounters)
    {
        encounters.Table = encounters.Table.Where(z => z.RaidEnemyInfo.BossPokePara.DevId != 0).ToArray();
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

    private static string[] GetCommonText(string name, TextConfig cfg)
    {
        byte[] data;
        if (name.Equals("monsname"))
            data = Resources.monsname_eng;
        else if (name.Equals("itemname"))
            data = Resources.itemname_eng;
        else if (name.Equals("wazaname"))
            data = Resources.wazaname_eng;
        else if (name.Equals("typename"))
            data = Resources.typename_eng;
        else if (name.Equals("seikaku"))
            data = Resources.seikaku_eng;
        else
            throw new ArgumentOutOfRangeException(name);

        return new TextFile(data, cfg).Lines;
    }

    private static string GetItemName(ushort item, ReadOnlySpan<string> items, ReadOnlySpan<string> moves)
    {
        bool isTM = IsTM(item);
        var tm = new PKHeX.Core.PersonalInfo9SV(new byte[] { 0x0 }).RecordPermitIndexes.ToArray();

        if (isTM) // append move name to TM
            return GetNameTM(item, items, moves, tm);
        return $"{items[item]}";
    }

    private static bool IsTM(ushort item) => item switch
    {
        >= 328 and <= 419 => true, // TM001 to TM092, skip TM000 Mega Punch
        618 or 619 or 620 => true, // TM093 to TM095
        690 or 691 or 692 or 693 => true, // TM096 to TM099
        >= 2160 and <= 2231 => true, // TM100 to TM171
        _ => false,
    };

    private static string GetNameTM(ushort item, ReadOnlySpan<string> items, ReadOnlySpan<string> moves, ReadOnlySpan<ushort> tm) => item switch
    {
        >= 328 and <= 419 => $"{items[item]} {moves[tm[001 + item - 328]]}", // TM001 to TM092, skip TM000 Mega Punch
        618 or 619 or 620 => $"{items[item]} {moves[tm[093 + item - 618]]}", // TM093 to TM095
        690 or 691 or 692 or 693 => $"{items[item]} {moves[tm[096 + item - 690]]}", // TM096 to TM099
        _ => $"{items[item]} {moves[tm[100 + item - 2160]]}" // TM100 to TM171
    };
}
