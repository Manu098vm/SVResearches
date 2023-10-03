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

                    var KFixedSymbolRetainer01 = sav9.Where(i => i.Key == 0x74ABBD32).FirstOrDefault()!;
                    var KFixedSymbolRetainer02 = sav9.Where(i => i.Key == 0x74ABBEE5).FirstOrDefault()!;
                    var KFixedSymbolRetainer03 = sav9.Where(i => i.Key == 0x74ABB9CC).FirstOrDefault()!;
                    var KFixedSymbolRetainer04 = sav9.Where(i => i.Key == 0x74ABBB7F).FirstOrDefault()!;
                    var KFixedSymbolRetainer05 = sav9.Where(i => i.Key == 0x74ABB666).FirstOrDefault()!;
                    var KFixedSymbolRetainer06 = sav9.Where(i => i.Key == 0x74ABB819).FirstOrDefault()!;
                    var KFixedSymbolRetainer07 = sav9.Where(i => i.Key == 0x74ABB300).FirstOrDefault()!;
                    var KFixedSymbolRetainer08 = sav9.Where(i => i.Key == 0x74ABB4B3).FirstOrDefault()!;
                    var KFixedSymbolRetainer09 = sav9.Where(i => i.Key == 0x74ABCACA).FirstOrDefault()!;
                    var KFixedSymbolRetainer10 = sav9.Where(i => i.Key == 0x74ABCC7D).FirstOrDefault()!;

                    var raidPaldea = sav9.Where(i => i.Key == 0xCAAC8800).FirstOrDefault()!;
                    var raidKitakami = sav9.Where(i => i.Key == 0x100B93DA).FirstOrDefault()!;
                    var raid7star = sav9.Where(i => i.Key == 0x8B14392F).FirstOrDefault()!;

                    var mystatus = sav9.Where(i => i.Key == 0xE3E89BD1).FirstOrDefault()!;
                    var overworld = sav9.Where(i => i.Key == 0x173304D8).FirstOrDefault()!;
                    var kcoordinates = sav9.Where(i => i.Key == 0x708D1511).FirstOrDefault()!;
                    var mysterygift = sav9.Where(i => i.Key == 0x99E1625E).FirstOrDefault()!;

                    var fixedrewarditemarray = sav9.Where(i => i.Key == 0x7D6C2B82).FirstOrDefault()!;
                    var lotteryrewarditemwarray = sav9.Where(i => i.Key == 0xA52B4811).FirstOrDefault()!;
                    var raidenemyarray = sav9.Where(i => i.Key == 0x0520A1B0).FirstOrDefault()!;
                    var raidpriorityarray = sav9.Where(i => i.Key == 0x095451E4).FirstOrDefault()!;
                    var eventraididentifier = sav9.Where(i => i.Key == 0x37B99B4D).FirstOrDefault()!;

                    var KBCATOutbreakZonesPaldea = sav9.Where(i => i.Key == 0x3FDC5DFF).FirstOrDefault()!;
                    var KBCATOutbreakZonesKitakami = sav9.Where(i => i.Key == 0xF9F156A3).FirstOrDefault()!;
                    var KBCATOutbreakZonesBlueberry = sav9.Where(i => i.Key == 0x1B45E41C).FirstOrDefault()!;
                    var KBCATOutbreakPokeData = sav9.Where(i => i.Key == 0x6C1A131B).FirstOrDefault()!;

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

                    Directory.CreateDirectory($"{path}\\KFixedSymbolRetainer01\\Encrypted");
                    Directory.CreateDirectory($"{path}\\KFixedSymbolRetainer01\\Decrypted");
                    File.WriteAllBytes($"{path}\\KFixedSymbolRetainer01\\{KFixedSymbolRetainer01.Key:X}.bin", KFixedSymbolRetainer01.Data);
                    ExportPokemons(KFixedSymbolRetainer01.Data, $"{path}\\KFixedSymbolRetainer01", intensive: true);

                    Directory.CreateDirectory($"{path}\\KFixedSymbolRetainer02\\Encrypted");
                    Directory.CreateDirectory($"{path}\\KFixedSymbolRetainer02\\Decrypted");
                    File.WriteAllBytes($"{path}\\KFixedSymbolRetainer02\\{KFixedSymbolRetainer02.Key:X}.bin", KFixedSymbolRetainer02.Data);
                    ExportPokemons(KFixedSymbolRetainer02.Data, $"{path}\\KFixedSymbolRetainer02", intensive: true);

                    Directory.CreateDirectory($"{path}\\KFixedSymbolRetainer03\\Encrypted");
                    Directory.CreateDirectory($"{path}\\KFixedSymbolRetainer03\\Decrypted");
                    File.WriteAllBytes($"{path}\\KFixedSymbolRetainer03\\{KFixedSymbolRetainer03.Key:X}.bin", KFixedSymbolRetainer03.Data);
                    ExportPokemons(KFixedSymbolRetainer03.Data, $"{path}\\KFixedSymbolRetainer03", intensive: true);

                    Directory.CreateDirectory($"{path}\\KFixedSymbolRetainer04\\Encrypted");
                    Directory.CreateDirectory($"{path}\\KFixedSymbolRetainer04\\Decrypted");
                    File.WriteAllBytes($"{path}\\KFixedSymbolRetainer04\\{KFixedSymbolRetainer04.Key:X}.bin", KFixedSymbolRetainer04.Data);
                    ExportPokemons(KFixedSymbolRetainer04.Data, $"{path}\\KFixedSymbolRetainer04", intensive: true);

                    Directory.CreateDirectory($"{path}\\KFixedSymbolRetainer05\\Encrypted");
                    Directory.CreateDirectory($"{path}\\KFixedSymbolRetainer05\\Decrypted");
                    File.WriteAllBytes($"{path}\\KFixedSymbolRetainer05\\{KFixedSymbolRetainer05.Key:X}.bin", KFixedSymbolRetainer05.Data);
                    ExportPokemons(KFixedSymbolRetainer05.Data, $"{path}\\KFixedSymbolRetainer05", intensive: true);

                    Directory.CreateDirectory($"{path}\\KFixedSymbolRetainer06\\Encrypted");
                    Directory.CreateDirectory($"{path}\\KFixedSymbolRetainer06\\Decrypted");
                    File.WriteAllBytes($"{path}\\KFixedSymbolRetainer06\\{KFixedSymbolRetainer06.Key:X}.bin", KFixedSymbolRetainer06.Data);
                    ExportPokemons(KFixedSymbolRetainer06.Data, $"{path}\\KFixedSymbolRetainer06", intensive: true);

                    Directory.CreateDirectory($"{path}\\KFixedSymbolRetainer07\\Encrypted");
                    Directory.CreateDirectory($"{path}\\KFixedSymbolRetainer07\\Decrypted");
                    File.WriteAllBytes($"{path}\\KFixedSymbolRetainer07\\{KFixedSymbolRetainer07.Key:X}.bin", KFixedSymbolRetainer07.Data);
                    ExportPokemons(KFixedSymbolRetainer07.Data, $"{path}\\KFixedSymbolRetainer07", intensive: true);

                    Directory.CreateDirectory($"{path}\\KFixedSymbolRetainer08\\Encrypted");
                    Directory.CreateDirectory($"{path}\\KFixedSymbolRetainer08\\Decrypted");
                    File.WriteAllBytes($"{path}\\KFixedSymbolRetainer08\\{KFixedSymbolRetainer08.Key:X}.bin", KFixedSymbolRetainer08.Data);
                    ExportPokemons(KFixedSymbolRetainer08.Data, $"{path}\\KFixedSymbolRetainer08", intensive: true);

                    Directory.CreateDirectory($"{path}\\KFixedSymbolRetainer09\\Encrypted");
                    Directory.CreateDirectory($"{path}\\KFixedSymbolRetainer09\\Decrypted");
                    File.WriteAllBytes($"{path}\\KFixedSymbolRetainer09\\{KFixedSymbolRetainer09.Key:X}.bin", KFixedSymbolRetainer09.Data);
                    ExportPokemons(KFixedSymbolRetainer09.Data, $"{path}\\KFixedSymbolRetainer09", intensive: true);

                    Directory.CreateDirectory($"{path}\\KFixedSymbolRetainer10\\Encrypted");
                    Directory.CreateDirectory($"{path}\\KFixedSymbolRetainer10\\Decrypted");
                    File.WriteAllBytes($"{path}\\KFixedSymbolRetainer10\\{KFixedSymbolRetainer10.Key:X}.bin", KFixedSymbolRetainer10.Data);
                    ExportPokemons(KFixedSymbolRetainer10.Data, $"{path}\\KFixedSymbolRetainer10", intensive: true);

                    Directory.CreateDirectory($"{path}\\Raid Paldea");
                    File.WriteAllBytes($"{path}\\Raid Paldea\\{raidPaldea.Key:X}.bin", raidPaldea.Data);

                    Directory.CreateDirectory($"{path}\\Raid Kitakami");
                    File.WriteAllBytes($"{path}\\Raid Kitakami\\{raidKitakami.Key:X}.bin", raidKitakami.Data);

                    Directory.CreateDirectory($"{path}\\Raid 7 Star");
                    File.WriteAllBytes($"{path}\\Raid 7 Star\\{raid7star.Key:X}.bin", raid7star.Data);

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

                    Directory.CreateDirectory($"{path}\\EventOutbreakData");
                    File.WriteAllBytes($"{path}\\EventOutbreakData\\KBCATOutbreakZonesPaldea", KBCATOutbreakZonesPaldea.Data);
                    File.WriteAllBytes($"{path}\\EventOutbreakData\\KBCATOutbreakZonesKitakami", KBCATOutbreakZonesKitakami.Data);
                    File.WriteAllBytes($"{path}\\EventOutbreakData\\KBCATOutbreakZonesBlueberry", KBCATOutbreakZonesBlueberry.Data);
                    File.WriteAllBytes($"{path}\\EventOutbreakData\\KBCATOutbreakPokeData", KBCATOutbreakPokeData.Data);

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