using OutbreakParser.Properties;
using pkNX.Structures;
using pkNX.Structures.FlatBuffers;
using pkNX.Structures.FlatBuffers.SV;
using System.Data;
using System.Text.Json;

namespace OutbreakParser;

public static class Program
{
    public static void Main()
    {
        if (Environment.GetCommandLineArgs().Length == 2 || Environment.GetCommandLineArgs().Length == 3)
        {
            try
            {
                if (Environment.GetCommandLineArgs().Length == 3 && (Environment.GetCommandLineArgs()[2].Equals("-n") || Environment.GetCommandLineArgs()[2].Equals("--null")))
                    DumpDistributionOutbreaksInit(Environment.GetCommandLineArgs()[1], true);
                else
                    DumpDistributionOutbreaksInit(Environment.GetCommandLineArgs()[1], false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        else
            Console.WriteLine($"Drag and drop the event \"files\" folder into the .exe.\n" +
                $"The files folder must contain the following files:\n" +
                $"- pokedata_array\n" +
                $"- zone_main_array\n" +
                $"- zone_su1_array\n" +
                $"- zone_su2_array");

        Console.WriteLine("Process finished. Press any key to exit.");
        Console.ReadKey();
    }

    public static void DumpDistributionOutbreaksInit(string path, bool parsenull)
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

        DumpDeliveryOutbreakData(path, parsenull);
    }

    private static void DumpDeliveryOutbreakData(string path, bool parsenull)
    {
        var v = 3;

        var zoneF2path = Path.Combine(path, "zone_su2_array_3_0_0");

        if (!File.Exists(zoneF2path))
            v = 2;

        var pokedatapath = Path.Combine(path, $"pokedata_array_{v}_0_0");
        var zoneF0path = Path.Combine(path, $"zone_main_array_{v}_0_0");
        var zoneF1path = Path.Combine(path, $"zone_su1_array_{v}_0_0");
        var version = pokedatapath.Substring(pokedatapath.IndexOf($"_{v}"), 6);

        var dataPokeData = GetDistributionContents(pokedatapath);
        var dataZoneF0 = GetDistributionContents(zoneF0path);
        var dataZoneF1 = GetDistributionContents(zoneF1path);
        var dataZoneF2 = v > 2 ? GetDistributionContents(zoneF2path) : Resources.zone_su2_array_3_0_0;

        var tableZoneF0 = FlatBufferConverter.DeserializeFrom<DeliveryOutbreakArray>(dataZoneF0);
        var tableZoneF1 = FlatBufferConverter.DeserializeFrom<DeliveryOutbreakArray>(dataZoneF1);
        var tableZoneF2 = FlatBufferConverter.DeserializeFrom<DeliveryOutbreakArray>(dataZoneF2);
        var tablePokeData = FlatBufferConverter.DeserializeFrom<DeliveryOutbreakPokeDataArray>(dataPokeData);

        var index = tablePokeData.Table[0].ID > 0 ? uint.Parse($"{tablePokeData.Table[0].ID}"[..8]) : 0;

        var dirDistText = Path.Combine(path, "../Json");

        ExportParse(dirDistText, tableZoneF0, tableZoneF1, tableZoneF2, tablePokeData, version, index, parsenull);
        ExportIdentifierBlock(index, path);
    }

    private static byte[] GetDistributionContents(string path) =>
        File.ReadAllBytes(path);

    private static void ExportIdentifierBlock(uint index, string path) =>
        File.WriteAllText($"{path}\\..\\Identifier.txt", $"{index}");

    private static void ExportParse(string dir, DeliveryOutbreakArray tableZoneF0, DeliveryOutbreakArray tableZoneF1, DeliveryOutbreakArray tableZoneF2, DeliveryOutbreakPokeDataArray tablePokeData, string version, uint identifier, bool parsenull)
    {
        Directory.CreateDirectory(dir);

        if (!parsenull) {
            tablePokeData.RemoveEmptyEntries();
            tableZoneF0.RemoveEmptyEntries();
            tableZoneF1.RemoveEmptyEntries();
            tableZoneF2.RemoveEmptyEntries();
        }

        DumpJson(tableZoneF0, dir, $"zone_main_array{version}");
        DumpJson(tableZoneF1, dir, $"zone_su1_array{version}");
        DumpJson(tableZoneF2, dir, $"zone_su2_array{version}");
        DumpJson(tablePokeData, dir, $"pokedata_array{version}");

        var dpF0 = Resources.outbreak_point_main;
        var dpF1 = Resources.outbreak_point_su1;
        var dpF2 = Resources.outbreak_point_su2;

        var pointsF0 = FlatBufferConverter.DeserializeFrom<OutbreakPointArray>(dpF0).Table;
        var pointsF1 = FlatBufferConverter.DeserializeFrom<OutbreakPointArray>(dpF1).Table;
        var pointsF2 = FlatBufferConverter.DeserializeFrom<OutbreakPointArray>(dpF2).Table;

        var main = GetForMap(pointsF0, tableZoneF0, tablePokeData, ZoneType.Main);
        var su1 = GetForMap(pointsF1, tableZoneF1, tablePokeData, ZoneType.Su1);
        var su2 = GetForMap(pointsF2, tableZoneF2, tablePokeData, ZoneType.Su2);

        DumpPretty(identifier, main.Concat(su1).Concat(su2), dir);
    }

    private static OutbreakEncounter[] GetForMap(IEnumerable<OutbreakPointData> points, DeliveryOutbreakArray possible, DeliveryOutbreakPokeDataArray pd, ZoneType baseMet)
    {
        var encs = GetMetaEncounter(possible.Table, pd);
        foreach (var enc in encs)
        {
            enc.MetBase = ZoneType.None;
            enc.MetLevel = new LevelRange(100, 1);
        }

        foreach (var enc in encs)
        {
            foreach (var point in points)
            {
                var poke = enc.Poke;
                if (!poke.IsLevelRangeCompatible(point.LevelRange))
                    continue;
                if (!poke.IsEnableCompatible(point.EnableTable))
                    continue;
                if (!poke.IsCompatibleArea((byte)point.AreaNo))
                    continue;
                if (!poke.IsCompatibleArea(point.AreaName))
                    continue;

                var min = Math.Min(enc.MetLevel.Min, (byte)point.LevelRange.X);
                var max = Math.Max(enc.MetLevel.Max, (byte)point.LevelRange.Y);
                enc.MetLevel = new LevelRange(min, max);
                enc.MetBase = baseMet;
            }

            if (enc.MetBase != ZoneType.None)
            {
                var min = Math.Max((byte)enc.Poke.MinLevel, enc.MetLevel.Min);
                var max = Math.Min((byte)enc.Poke.MaxLevel, enc.MetLevel.Max);
                enc.MetLevel = new LevelRange(min, max);
            }
        }
        return encs;
    }

    private static OutbreakEncounter[] GetMetaEncounter(IEnumerable<DeliveryOutbreak> possibleTable, DeliveryOutbreakPokeDataArray pd)
    {
        var ret = new List<OutbreakEncounter>();
        var hs = new HashSet<ulong>();
        foreach (var outbreak in possibleTable)
        {
            TryAdd(outbreak.Poke1, outbreak.Poke1LotValue);
            TryAdd(outbreak.Poke2, outbreak.Poke2LotValue);
            TryAdd(outbreak.Poke3, outbreak.Poke3LotValue);
            TryAdd(outbreak.Poke4, outbreak.Poke4LotValue);
            TryAdd(outbreak.Poke5, outbreak.Poke5LotValue);
            continue;
            void TryAdd(ulong ID, short rate)
            {
                if (ID == 0 || rate <= 0 || !hs.Add(ID))
                    return;
                var poke = pd.Table.First(z => z.ID == ID);
                ret.Add(new OutbreakEncounter { ZoneID = outbreak.ZoneID, Poke = poke });
            }
        }
        return [.. ret];
    }

    private class OutbreakEncounter
    {
        public ulong ZoneID { get; init; }
        public required DeliveryOutbreakPokeData Poke { get; init; }
        public ZoneType MetBase { get; set; }
        public LevelRange MetLevel { get; set; }
    }

    private enum ZoneType : byte
    {
        None = 0,
        Main = 1,
        Su1 = 2,
        Su2 = 3,
    }

    private record struct LevelRange(byte Min, byte Max);

    private static void RemoveEmptyEntries(this DeliveryOutbreakPokeDataArray encounters) =>
        encounters.Table = encounters.Table.Where(z => z.DevId != 0).ToArray();

    private static void RemoveEmptyEntries(this DeliveryOutbreakArray encounters) =>
    encounters.Table = encounters.Table.Where(z => z.ZoneID != 0).ToArray();

    private static void DumpJson(object flat, string dir, string name)
    {
        var opt = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(flat, opt);

        var fileName = Path.ChangeExtension(name, ".json");
        File.WriteAllText(Path.Combine(dir, fileName), json);
    }

    private static string[] GetCommonText(string name, TextConfig cfg)
    {
        byte[] data;
        if (name.Equals("monsname"))
            data = Resources.monsname_english;
        else if (name.Equals("itemname"))
            data = Resources.itemname_english;
        else
            throw new ArgumentOutOfRangeException(name);

        return new TextFile(data, cfg).Lines;
    }

    private static void DumpPretty(uint identifier, IEnumerable<OutbreakEncounter> encounters, string dir)
    {
        var cfg = new TextConfig(GameVersion.SV);
        var lines = new List<string>();
        var ident = identifier;

        var species = GetCommonText("monsname", cfg);
        var items = GetCommonText("itemname", cfg);

        lines.Add($"Event Outbreak Identifier: {ident}");

        foreach (var enc in encounters.Where(e => e.MetBase is not ZoneType.None))
        {
            lines.Add("");

            var entry = enc.Poke;

            string version = "";
            if (entry.Version is not null)
                if (entry.Version.A && entry.Version.B)
                    version = string.Empty;
                else if (entry.Version.A)
                    version = "Scarlet";
                else
                    version = "Violet";

            string shiny = "";
            if (!entry.EnableRarePercentage)
                shiny = "Standard rate";
            else if (entry.RarePercentage == 0)
                shiny = "Never";
            else if (entry.RarePercentage == 100)
                shiny = "Always";
            else
                shiny = $"{entry.RarePercentage.ToString().Replace(',', '.')}% rate";

            string size = "";
            if (!entry.EnableScaleRange)
                size = string.Empty;
            else
                size = $"{entry.MinScale}-{entry.MaxScale}";

            string zone = enc.MetBase switch
            {
                ZoneType.Main => "Paldea",
                ZoneType.Su1 => "Kitakami",
                ZoneType.Su2 => "Blueberry Academy",
                _ => string.Empty,
            };

            var gender = entry.Sex switch
            {
                SexType.MALE => "Male",
                SexType.FEMALE => "Female",
                _ => string.Empty,
            };

            ItemID item;
            if (!entry.Item.HasValue || entry.Item.Value.ItemID == 0 || entry.Item.Value.BringRate <= 0)
                item = ItemID.ITEMID_NONE;
            else
                item = entry.Item.Value.ItemID;

            var form = entry.FormId == 0 ? string.Empty : $"-{(int)entry.FormId}";

            RibbonType ribbon = RibbonType.NONE;
            if (entry.AddRibbonPercentage > 0 && entry.AddRibbonType > RibbonType.NONE)
                ribbon = entry.AddRibbonType;

            lines.Add($"{species[(int)entry.DevId]}{form} Mass Outbreak");

            if (version != string.Empty)
                lines.Add($"\tVersion: {version}");

            lines.Add($"\tLevel: {entry.MinLevel}-{entry.MaxLevel}");

            if (entry.Sex != SexType.DEFAULT)
                lines.Add($"\tGender: {gender}");

            lines.Add($"\tShiny: {shiny}");

            if (ribbon != RibbonType.NONE)
                lines.Add($"\tRibbon: {(PKHeX.Core.RibbonIndex)(int)ribbon - 1} ({entry.AddRibbonPercentage.ToString().Replace(',', '.')}%)");

            if (size != string.Empty)
                lines.Add($"\tScale: {size}");

            if (item != ItemID.ITEMID_NONE)
                lines.Add($"\tHeld Item: {items[(int)item]} ({entry.Item!.Value.BringRate.ToString().Replace(',', '.')}%)");

            lines.Add($"\tRegion: {zone}");
        }

        File.WriteAllLines(Path.Combine(dir, $"../Encounters.txt"), lines);
    }
}