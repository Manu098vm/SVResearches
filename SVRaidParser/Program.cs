using pkNX.Structures;
using pkNX.Structures.FlatBuffers.SV;
using RaidParser.Properties;
using System.Text.Json;

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
                    DumpDistributionRaidsInit(Environment.GetCommandLineArgs()[1], true);
                else
                    DumpDistributionRaidsInit(Environment.GetCommandLineArgs()[1], false);
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

    private record RaidStorage(RaidEnemyTable Enemy, int File)
    {
        private PokeDataBattle Poke => Enemy.Info.BossPokePara;

        public int Stars => Enemy.Info.Difficulty == 0 ? File + 1 : Enemy.Info.Difficulty;
        public DevID Species => Poke.DevId;
        public short Form => Poke.FormId;
        public int Delivery => Enemy.Info.DeliveryGroupID;
        public sbyte Rate => Enemy.Info.Rate;

        public int RandRateStartScarlet { get; set; }
        public int RandRateStartViolet { get; set; }

        public short GetScarletRandMinScarlet()
        {
            if (Enemy.Info.RomVer == RaidRomType.TYPE_B)
                return -1;
            return (short)RandRateStartScarlet;
        }

        public short GetVioletRandMinViolet()
        {
            if (Enemy.Info.RomVer == RaidRomType.TYPE_A)
                return -1;
            return (short)RandRateStartViolet;
        }
    }

    public static void DumpDistributionRaidsInit(string path, bool parsenull)
    {
        Console.WriteLine("Processing...");
        if (path.Contains("files"))
        {
            var newpath = path.Replace("files", "Files");
            Directory.Move(path, newpath);
            path = newpath;
        }

        if (path.Contains("null"))
            parsenull = true;

        DumpDistributionRaids(path, parsenull);
    }

    private static void DumpDistributionRaids(string path, bool parsenull)
    {
        var encounterspath = Path.Combine(path, "raid_enemy_array_3_0_0");
        var dropspath = Path.Combine(path, "fixed_reward_item_array_3_0_0");
        var bonuspath = Path.Combine(path, "lottery_reward_item_array_3_0_0");
        var prioritypath = Path.Combine(path, "raid_priority_array_3_0_0");

        if (!File.Exists(encounterspath))
            encounterspath = Path.Combine(path, "raid_enemy_array_2_0_0");
        if (!File.Exists(dropspath))
            dropspath = Path.Combine(path, "fixed_reward_item_array_2_0_0");
        if (!File.Exists(bonuspath))
            bonuspath = Path.Combine(path, "lottery_reward_item_array_2_0_0");
        if (!File.Exists(prioritypath))
            prioritypath = Path.Combine(path, "raid_priority_array_2_0_0");

        if (!File.Exists(encounterspath))
            encounterspath = Path.Combine(path, "raid_enemy_array_1_3_0");
        if (!File.Exists(dropspath))
            dropspath = Path.Combine(path, "fixed_reward_item_array_1_3_0");
        if (!File.Exists(bonuspath))
            bonuspath = Path.Combine(path, "lottery_reward_item_array_1_3_0");
        if (!File.Exists(prioritypath))
            prioritypath = Path.Combine(path, "raid_priority_array_1_3_0");

        if(!File.Exists(encounterspath))
            encounterspath = Path.Combine(path, "raid_enemy_array");
        if(!File.Exists(dropspath))
            dropspath = Path.Combine(path, "fixed_reward_item_array");
        if(!File.Exists(bonuspath))
            bonuspath = Path.Combine(path, "lottery_reward_item_array");
        if(!File.Exists(prioritypath))
            prioritypath = Path.Combine(path, "raid_priority_array");

        var isMajorVersion = encounterspath.IndexOf("_1");

        if (isMajorVersion == -1)
            isMajorVersion = encounterspath.IndexOf("_2");

        if (isMajorVersion == -1)
            isMajorVersion = encounterspath.IndexOf("_3");

        var version = isMajorVersion > 0 ? encounterspath.Substring(isMajorVersion, 6) : "";

        var dataEncounters = GetDistributionContents(encounterspath);
        var dataDrop = GetDistributionContents(dropspath);
        var dataBonus = GetDistributionContents(bonuspath);
        var dataPriority = GetDistributionContents(prioritypath);

        var tableEncounters = pkNX.Structures.FlatBuffers.FlatBufferConverter.DeserializeFrom<DeliveryRaidEnemyTableArray>(dataEncounters);
        var tableDrops = pkNX.Structures.FlatBuffers.FlatBufferConverter.DeserializeFrom<DeliveryRaidFixedRewardItemArray>(dataDrop);
        var tableBonus = pkNX.Structures.FlatBuffers.FlatBufferConverter.DeserializeFrom<DeliveryRaidLotteryRewardItemArray>(dataBonus);
        var tablePriority = pkNX.Structures.FlatBuffers.FlatBufferConverter.DeserializeFrom<DeliveryRaidPriorityArray>(dataPriority);
        var index = tablePriority.Table[0].VersionNo;

        var byGroupID = tableEncounters.Table
            .Where(z => z.Info.Rate != 0)
            .GroupBy(z => z.Info.DeliveryGroupID);

        var dirDistText = Path.Combine(path, "../Json");
        ExportParse(dirDistText, tableEncounters, tableDrops, tableBonus, tablePriority, version, parsenull);
        ExportIdentifierBlock(index, path, version);
    }

    [Flags]
    private enum DistroGroupSet
    {
        None = 0,
        SL = 1,
        VL = 2,
        Both = SL | VL,
    }

    private static void ExportIdentifierBlock(int index, string path, string version)
    {
        var data = BitConverter.GetBytes((uint)index);
        File.WriteAllBytes($"{path}\\event_raid_identifier{version}", data);
        File.WriteAllText($"{path}\\..\\Identifier.txt", $"{index}");
    }

    private static void ExportParse(string dir,
        DeliveryRaidEnemyTableArray tableEncounters,
        DeliveryRaidFixedRewardItemArray tableDrops,
        DeliveryRaidLotteryRewardItemArray tableBonus,
        DeliveryRaidPriorityArray tablePriority,
        string version,
        bool parsenull)
    {
        Directory.CreateDirectory(dir);

        if(!parsenull)
            tableEncounters.RemoveEmptyEntries();

        DumpJson(tableEncounters, dir, $"raid_enemy_array{version}");
        DumpJson(tableDrops, dir, $"fixed_reward_item_array{version}");
        DumpJson(tableBonus, dir, $"lottery_reward_item_array{version}");
        DumpJson(tablePriority, dir, $"raid_priority_array{version}");
        DumpPretty(tableEncounters, tableDrops, tableBonus, tablePriority, dir);
    }

    private static void DumpPretty(DeliveryRaidEnemyTableArray tableEncounters, DeliveryRaidFixedRewardItemArray tableDrops, DeliveryRaidLotteryRewardItemArray tableBonus, DeliveryRaidPriorityArray tablePriority, string dir)
    {
        var cfg = new TextConfig(GameVersion.SV);
        var lines = new List<string>();
        var ident = tablePriority.Table[0].VersionNo;

        var species = GetCommonText("monsname", cfg);
        var items = GetCommonText("itemname", cfg);
        var moves = GetCommonText("wazaname", cfg);
        var types = GetCommonText("typename", cfg);
        var natures = GetCommonText("seikaku", cfg);

        lines.Add($"Event Raid Identifier: {ident}");

        foreach (var entry in tableEncounters.Table)
        {
            var boss = entry.Info.BossPokePara;
            var extra = entry.Info.BossDesc;
            var nameDrop = entry.Info.DropTableFix;
            var nameBonus = entry.Info.DropTableRandom;

            if (boss.DevId == DevID.DEV_NULL)
                continue;

            var version = entry.Info.RomVer switch
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

            var capture = entry.Info.CaptureRate switch
            {
                // 0 never
                // 1 always
                2 => "Only Once",
                _ => $"{entry.Info.CaptureRate}",
            };

            var size = boss.ScaleType switch
            {
                SizeType.VALUE => $"{boss.ScaleValue}",
                SizeType.XS => "0-15",
                SizeType.S => "16-47",
                SizeType.M => "48-207",
                SizeType.L => "208-239",
                SizeType.XL => "240-255",
                _ => string.Empty,
            };

            var gender = boss.Sex switch
            {
                SexType.MALE => "Male",
                SexType.FEMALE => "Female",
                _ => string.Empty,
            };

            var form = boss.FormId == 0 ? string.Empty : $"-{(int)boss.FormId}";

            lines.Add($"{entry.Info.Difficulty}-Star {species[(int)boss.DevId]}{form}");
            if (entry.Info.RomVer != RaidRomType.BOTH)
                lines.Add($"\tVersion: {version}");

            lines.Add($"\tTera Type: {gem}");
            if (boss.Level != entry.Info.CaptureLv)
                lines.Add($"\tBattle Level: {boss.Level}");
            lines.Add($"\tCapture Level: {entry.Info.CaptureLv}");
            lines.Add($"\tAbility: {ability}");

            if (boss.Seikaku != SeikakuType.DEFAULT)
                lines.Add($"\tNature: {natures[(int)boss.Seikaku - 1]}");

            if (boss.Sex != SexType.DEFAULT)
                lines.Add($"\tGender: {gender}");

            lines.Add($"\tIVs: {iv}");

            var evs = boss.EffortValue.ToArray();
            if (evs.Any(z => z != 0))
            {
                string[] names = ["HP", "Atk", "Def", "SpA", "SpD", "Spe"];
                var spread = new List<string>();

                for (int i = 0; i < evs.Length; i++)
                {
                    if (evs[i] == 0)
                        continue;
                    spread.Add($"{evs[i]} {names[i]}");
                }

                lines.Add($"\tEVs: {string.Join(" / ", spread)}");
            }

            if (boss.RareType != RareType.DEFAULT)
                lines.Add($"\tShiny: {shiny}");

            if (boss.ScaleType != SizeType.RANDOM)
                lines.Add($"\tScale: {size}");

            if (entry.Info.Difficulty == 7)
            {
                float hp = entry.Info.BossDesc.HpCoef / 100f;
                lines.Add($"\tHP Multiplier: {hp:0.0}×");
            }

            if (boss.Item != ItemID.ITEMID_NONE)
                lines.Add($"\tHeld Item: {items[(int)boss.Item]}");

            if (entry.Info.CaptureRate != 1)
                lines.Add($"\tCatchable: {capture}");

            lines.Add($"\t\tMoves:");
            lines.Add($"\t\t\t- {moves[(int)boss.Waza1.WazaId]}");
            if ((int)boss.Waza2.WazaId != 0) lines.Add($"\t\t\t- {moves[(int)boss.Waza2.WazaId]}");
            if ((int)boss.Waza3.WazaId != 0) lines.Add($"\t\t\t- {moves[(int)boss.Waza3.WazaId]}");
            if ((int)boss.Waza4.WazaId != 0) lines.Add($"\t\t\t- {moves[(int)boss.Waza4.WazaId]}");

            if (extra.PowerChargeTrigerHp != 0 && extra.PowerChargeTrigerTime != 0)
            {
                lines.Add($"\t\tShield Activation:");
                lines.Add($"\t\t\t- {extra.PowerChargeTrigerHp}% HP Remaining");
                lines.Add($"\t\t\t- {extra.PowerChargeTrigerTime}% Time Remaining");
            }

            var actions = new[] { extra.ExtraAction1, extra.ExtraAction2, extra.ExtraAction3, extra.ExtraAction4, extra.ExtraAction5, extra.ExtraAction6 };
            if (actions.Any(IsExtraActionValid))
            {
                lines.Add("\t\tExtra Actions:");
                if (IsExtraActionValid(actions[0])) lines.Add($"\t\t\t- {GetExtraActionInfo(actions[0], moves)}");
                if (IsExtraActionValid(actions[1])) lines.Add($"\t\t\t- {GetExtraActionInfo(actions[1], moves)}");
                if (IsExtraActionValid(actions[2])) lines.Add($"\t\t\t- {GetExtraActionInfo(actions[2], moves)}");
                if (IsExtraActionValid(actions[3])) lines.Add($"\t\t\t- {GetExtraActionInfo(actions[3], moves)}");
                if (IsExtraActionValid(actions[4])) lines.Add($"\t\t\t- {GetExtraActionInfo(actions[4], moves)}");
                if (IsExtraActionValid(actions[5])) lines.Add($"\t\t\t- {GetExtraActionInfo(actions[5], moves)}");
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
                        lines.Add($"\t\t\t{drop.Num,2} × TM Material{limitation}");

                    if (drop.Category == RaidRewardItemCategoryType.GEM) // Tera Shard
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
                {
                    RaidLotteryRewardItemInfo? drop = item.GetRewardItem(i);
                    totalRate += drop is null ? 0 : item.GetRewardItem(i).Rate;
                }

                for (int i = 0; i < count; i++)
                {
                    if (nameBonus != item.TableName)
                        continue;

                    RaidLotteryRewardItemInfo? drop = item.GetRewardItem(i);
                    float rate = (float)(Math.Round((drop is null ? 0 : drop.Rate / totalRate) * 100f, 2));

                    if (drop is null)
                        lines.Add($"\t\t\t{rate,5}% {drop?.Num,2} × Null");

                    else if (drop?.Category == RaidRewardItemCategoryType.POKE) // Material
                        lines.Add($"\t\t\t{rate,5}% {drop?.Num,2} × TM Material");

                    else if (drop?.Category == RaidRewardItemCategoryType.GEM) // Tera Shard
                        lines.Add($"\t\t\t{rate,5}% {drop?.Num,2} × Tera Shard");

                    else if (drop?.ItemID != 0)
                        lines.Add($"\t\t\t{rate,5}% {drop?.Num,2} × {items[drop is null ? 0 : (ushort)drop.ItemID]}");
                }
            }

            lines.Add("");
        }

        File.WriteAllLines(Path.Combine(dir, $"../Encounters.txt"), lines);
    }

    private static void RemoveEmptyEntries(this DeliveryRaidEnemyTableArray encounters)
    {
        encounters.Table = encounters.Table.Where(z => z.Info.BossPokePara.DevId != 0).ToArray();
    }

    private static bool IsExtraActionValid(RaidBossExtraData action)
    {
        if (action.Action == RaidBossExtraActType.NONE) // no action set
            return false;
        if (action.Value == 0) // no percentage set
            return false;
        if (action.Action == RaidBossExtraActType.WAZA && action.Wazano == WazaID.WAZA_NULL) // no move set
            return false;
        return true;
    }

    private static string GetExtraActionInfo(RaidBossExtraData action, string[] moves)
    {
        var type = action.Timing == RaidBossExtraTimingType.TIME ? "Time" : "HP";
        return action.Action switch
        {
            RaidBossExtraActType.BOSS_STATUS_RESET => $"Reset Raid Boss' Stat Changes at {action.Value}% {type} Remaining",
            RaidBossExtraActType.PLAYER_STATUS_RESET => $"Reset Player's Stat Changes at {action.Value}% {type} Remaining",
            RaidBossExtraActType.WAZA => $"Use {moves[(int)action.Wazano]} at {action.Value}% {type} Remaining",
            RaidBossExtraActType.GEM_COUNT => $"Reduce Tera Orb Charge at {action.Value}% {type} Remaining",
            _ => "",
        };
    }

    private static void DumpJson(object flat, string dir, string name)
    {
        var opt = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(flat, opt);

        if (name.Contains("raid_priority_array"))
        {
            var table = ((DeliveryRaidPriorityArray)flat).Table;
            var groups = table.Select(g => g.GroupID.Groups).ElementAt(0);
            var list = new List<string>();

            for (var i = 0; i < groups.Groups_Length; i++)
            {
                var groupID = GroupSet.Groups_Item(ref groups, i);
                if (groupID != 0)
                    list.Add($"{(i > 0 && list.Count > 0 ? "," : "")}\"GroupID{i+1:D2}\": {groupID}");
            }

            var ids = "";
            foreach (var group in list)
                ids += group;

            json = json.Replace("\"Groups_Length\": 10", ids);
            var obj = JsonSerializer.Deserialize<JsonElement>(json);
            json = JsonSerializer.Serialize(obj, opt);
        }

        var fileName = Path.ChangeExtension(name, ".json");
        File.WriteAllText(Path.Combine(dir, fileName), json);
    }

    private static byte[] GetDistributionContents(string path) => File.ReadAllBytes(path);

    private static string[] GetCommonText(string name, TextConfig cfg)
    {
        byte[] data;
        if (name.Equals("monsname"))
            data = Resources.monsname_english;
        else if (name.Equals("itemname"))
            data = Resources.itemname_english;
        else if (name.Equals("wazaname"))
            data = Resources.wazaname_english;
        else if (name.Equals("typename"))
            data = Resources.typename_english;
        else if (name.Equals("seikaku"))
            data = Resources.seikaku_english;
        else
            throw new ArgumentOutOfRangeException(name);

        return new TextFile(data, cfg).Lines;
    }

    private static string GetItemName(ushort item, ReadOnlySpan<string> items, ReadOnlySpan<string> moves)
    {
        bool isTM = IsTM(item);
        var tm = PersonalDumperSV.TMIndexes;

        if (isTM) // append move name to TM
            return GetNameTM(item, items, moves, tm);
        return $"{items[item]}";
    }

    private static bool IsTM(ushort item) => item switch
    {
        >= 328 and <= 419 => true, // TM001 to TM092, skip TM000 Mega Punch
        618 or 619 or 620 => true, // TM093 to TM095
        690 or 691 or 692 or 693 => true, // TM096 to TM099
        >= 2160 and <= 2289 => true, // TM100 to TM229
        _ => false,
    };

    private static string GetNameTM(ushort item, ReadOnlySpan<string> items, ReadOnlySpan<string> moves, ReadOnlySpan<ushort> tm) => item switch
    {
        >= 328 and <= 419 => $"{items[item]} {moves[tm[001 + item - 328]]}", // TM001 to TM092, skip TM000 Mega Punch
        618 or 619 or 620 => $"{items[item]} {moves[tm[093 + item - 618]]}", // TM093 to TM095
        690 or 691 or 692 or 693 => $"{items[item]} {moves[tm[096 + item - 690]]}", // TM096 to TM099
        _ => $"{items[item]} {moves[tm[100 + item - 2160]]}", // TM100 to TM229
    };
}
