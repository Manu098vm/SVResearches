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
                $"- pokedata_array_2_0_0\n" +
                $"- zone_main_array_2_0_0\n" +
                $"- zone_su1_array_2_0_0\n" +
                $"- zone_su2_array_2_0_0 (optional)");

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
        var zoneF0path = Path.Combine(path, "zone_main_array_2_0_0");
        var zoneF1path = Path.Combine(path, "zone_su1_array_2_0_0");
        //var zoneF2path = Path.Combine(path, "zone_su2_array_2_0_0");
        var pokedatapath = Path.Combine(path, "pokedata_array_2_0_0");

        var isMajorVersion = pokedatapath.IndexOf("_1");

        if (isMajorVersion == -1)
            isMajorVersion = pokedatapath.IndexOf("_2");

        var version = isMajorVersion > 0 ? pokedatapath.Substring(isMajorVersion, 6) : "";

        var dataZoneF0 = GetDistributionContents(zoneF0path);
        var dataZoneF1 = GetDistributionContents(zoneF1path);
        //var dataZoneF2 = GetDistributionContents(zoneF2path);
        var dataPokeData = GetDistributionContents(pokedatapath);

        var tableZoneF0 = FlatBufferConverter.DeserializeFrom<DeliveryOutbreakArray>(dataZoneF0);
        var tableZoneF1 = FlatBufferConverter.DeserializeFrom<DeliveryOutbreakArray>(dataZoneF1);
        //var tableZoneF2 = FlatBufferConverter.DeserializeFrom<DeliveryOutbreakArray>(dataZoneF2);
        var tablePokeData = FlatBufferConverter.DeserializeFrom<DeliveryOutbreakPokeDataArray>(dataPokeData);

        var index = tablePokeData.Table[0].ID > 0 ? uint.Parse($"{tablePokeData.Table[0].ID}"[..^3]) : 0;

        var dirDistText = Path.Combine(path, "../Json");

        ExportParse(dirDistText, tableZoneF0, tableZoneF1, tablePokeData, version, index, parsenull);
        ExportIdentifierBlock(index, path, version);
    }

    private static byte[] GetDistributionContents(string path) =>
        File.ReadAllBytes(path);

    private static void ExportIdentifierBlock(uint index, string path, string version)
    {
        var data = BitConverter.GetBytes(index);
        //File.WriteAllBytes($"{path}\\event_outbreak_identifier{version}", data);
        File.WriteAllText($"{path}\\..\\Identifier.txt", $"{index}");
    }

    private static void ExportParse(string dir, DeliveryOutbreakArray tableZoneF0, DeliveryOutbreakArray tableZoneF1, DeliveryOutbreakPokeDataArray tablePokeData, string version, uint identifier, bool parsenull)
    {
        Directory.CreateDirectory(dir);

        if (!parsenull) {
            tablePokeData.RemoveEmptyEntries();
            tableZoneF0.RemoveEmptyEntries();
            tableZoneF1.RemoveEmptyEntries();
        }

        DumpJson(tableZoneF0, dir, $"zone_main_array{version}");
        DumpJson(tableZoneF1, dir, $"zone_su1_array{version}");
        //DumpJson(tableZoneF2, dir, "zone_su2");
        DumpJson(tablePokeData, dir, $"pokedata_array{version}");
        DumpPretty(identifier, tablePokeData, dir);
    }

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

    private static void DumpPretty(uint identifier, DeliveryOutbreakPokeDataArray tablePokeData, string dir)
    {
        var cfg = new TextConfig(GameVersion.SV);
        var lines = new List<string>();
        var ident = identifier;

        var species = GetCommonText("monsname", cfg);
        var items = GetCommonText("itemname", cfg);

        lines.Add($"Event Outbreak Identifier: {ident}");

        foreach (var entry in tablePokeData.Table)
        {
            lines.Add("");

            if (entry.DevId == DevID.DEV_NULL)
                continue;

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
                shiny = $"{entry.RarePercentage}% rate";

            string size = "";
            if (!entry.EnableScaleRange)
                size = string.Empty;
            else
                size = $"{entry.MinScale}-{entry.MaxScale}";

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
                lines.Add($"\tRibbon: {(PKHeX.Core.RibbonIndex)(int)ribbon - 1} ({entry.AddRibbonPercentage}%)");

            if (size != string.Empty)
                lines.Add($"\tScale: {size}");

            if (item != ItemID.ITEMID_NONE)
                lines.Add($"\tHeld Item: {items[(int)item]} ({entry.Item!.Value.BringRate}%)");
        }

        File.WriteAllLines(Path.Combine(dir, $"../Encounters.txt"), lines);
    }
}