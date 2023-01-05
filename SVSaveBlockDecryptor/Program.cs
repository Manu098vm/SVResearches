using PKHeX.Core;

public static class Program
{
    const int EK9Size = 344;

    public static void Main(string[] args)
    {
        if (args.Length == 1)
        {
            var input = args[0];
            try
            {
                if (Path.GetFileName(input).Equals("main") || Path.GetFileName(input).Equals("backup"))
                {
                    Console.WriteLine("Processing...");

                    var path = Path.GetDirectoryName(input)!;
                    var sav9 = SwishCrypto.Decrypt(File.ReadAllBytes(input));

                    var box = sav9.Where(i => i.Key == 0x0D66012C).FirstOrDefault()!;
                    var party = sav9.Where(i => i.Key == 0x3AA1A9AD).FirstOrDefault()!;
                    var spawn = sav9.Where(i => i.Key == 0x74ABBD32).FirstOrDefault()!;
                    var raid = sav9.Where(i => i.Key == 0xCAAC8800).FirstOrDefault()!;

                    var mystatus = sav9.Where(i => i.Key == 0xE3E89BD1).FirstOrDefault()!;
                    var overworld = sav9.Where(i => i.Key == 0x173304D8).FirstOrDefault()!;
                    var kcoordinates = sav9.Where(i => i.Key == 0x708D1511).FirstOrDefault()!;
                    var mysterygift = sav9.Where(i => i.Key == 0x99E1625E).FirstOrDefault()!;

                    var fixedrewarditemarray = sav9.Where(i => i.Key == 0x7D6C2B82).FirstOrDefault()!;
                    var lotteryrewarditemwarray = sav9.Where(i => i.Key == 0xA52B4811).FirstOrDefault()!;
                    var raidenemyarray = sav9.Where(i => i.Key == 0x0520A1B0).FirstOrDefault()!;
                    var raidpriorityarray = sav9.Where(i => i.Key == 0x095451E4).FirstOrDefault()!;
                    var eventraididentifier = sav9.Where(i => i.Key == 0x37B99B4D).FirstOrDefault()!;

                    path = $"{path}\\Dump";
                    Directory.CreateDirectory(path);
                    SCBlockUtil.ExportAllBlocksAsSingleFile(sav9, $"{path}\\Blocks.bin");

                    Directory.CreateDirectory($"{path}\\Box\\Encrypted");
                    Directory.CreateDirectory($"{path}\\Box\\Decrypted");
                    File.WriteAllBytes($"{path}\\Box\\{box.Key:X}.bin", box.Data);
                    ExportPokemons(box.Data, $"{path}\\Box");

                    Directory.CreateDirectory($"{path}\\Party\\Encrypted");
                    Directory.CreateDirectory($"{path}\\Party\\Decrypted");
                    File.WriteAllBytes($"{path}\\Party\\{party.Key:X}.bin", party.Data);
                    ExportPokemons(party.Data, $"{path}\\Party");

                    Directory.CreateDirectory($"{path}\\Spawn\\Encrypted");
                    Directory.CreateDirectory($"{path}\\Spawn\\Decrypted");
                    File.WriteAllBytes($"{path}\\Spawn\\{spawn.Key:X}.bin", spawn.Data);
                    ExportPokemons(spawn.Data, $"{path}\\Spawn", intensive: true);

                    Directory.CreateDirectory($"{path}\\Raid");
                    File.WriteAllBytes($"{path}\\Raid\\{raid.Key:X}.bin", raid.Data);

                    Directory.CreateDirectory($"{path}\\MyStatus");
                    File.WriteAllBytes($"{path}\\MyStatus\\{mystatus.Key:X}.bin", mystatus.Data);

                    Directory.CreateDirectory($"{path}\\Overworld\\Encrypted");
                    Directory.CreateDirectory($"{path}\\Overworld\\Decrypted");
                    File.WriteAllBytes($"{path}\\Overworld\\{overworld.Key:X}.bin", overworld.Data);
                    ExportPokemons(overworld.Data, $"{path}\\Overworld", intensive: true);

                    Directory.CreateDirectory($"{path}\\KCoordinates");
                    File.WriteAllBytes($"{path}\\KCoordinates\\{kcoordinates.Key:X}.bin", kcoordinates.Data);

                    Directory.CreateDirectory($"{path}\\KMysteryGift");
                    File.WriteAllBytes($"{path}\\KMysteryGift\\{mysterygift.Key:X}.bin", mysterygift.Data);

                    Directory.CreateDirectory($"{path}\\EventRaidData");
                    File.WriteAllBytes($"{path}\\EventRaidData\\fixed_reward_item_array", fixedrewarditemarray.Data);
                    File.WriteAllBytes($"{path}\\EventRaidData\\lottery_reward_item_array", lotteryrewarditemwarray.Data);
                    File.WriteAllBytes($"{path}\\EventRaidData\\raid_enemy_array", raidenemyarray.Data);
                    File.WriteAllBytes($"{path}\\EventRaidData\\raid_priority_array", raidpriorityarray.Data);
                    File.WriteAllBytes($"{path}\\EventRaidData\\event_raid_identifier", eventraididentifier.Data);

                    Console.WriteLine($"Exported decrypted blocks from save file.\nPress any key to exit.");
                    Console.ReadKey();
                    return;
                }
                Console.WriteLine($"{Path.GetFileName(input)} is not a valid SV save file. Press any key to exit.");
                Console.ReadKey();
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.WriteLine("An error occured. Was it a valid SV save file?\nPress any key to exit.");
                Console.ReadKey();
                return;
            }
        }
        else
        {
            Console.WriteLine("No file loaded. Drag & Drop a save file into the executabile. Press any key to exit.");
            Console.ReadKey();
            return;
        }
    }

    public static void ExportPokemons(ReadOnlySpan<byte> block, string path, bool intensive = false)
    {
        var offset = 0;
        while (offset < block.Length && offset + EK9Size < block.Length)
        {
            var computed = false;
            var data = block.Slice(offset, EK9Size);
            var pkm = new PK8(data.ToArray());
            if (pkm.Species > 0 && pkm.Species <= 1010 && pkm.ChecksumValid)
            {
                var name = pkm.FileName;
                File.WriteAllBytes($"{path}\\Encrypted\\{name.Replace("pk8", "ek9")}", data.ToArray());
                File.WriteAllBytes($"{path}\\Decrypted\\{name.Replace("pk8", "pk9")}", pkm.Data);
                computed = true;
            }

            if (computed || !intensive)
                offset += EK9Size;
            else
                offset++;
        }
    }
}