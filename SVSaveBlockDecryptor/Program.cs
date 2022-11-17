using PKHeX.Core;
using System.Text;

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
                    var path = Path.GetDirectoryName(input)!;
                    var sav9 = SwishCrypto.Decrypt(File.ReadAllBytes(input));

                    var box = sav9.Where(i => i.Key == 0x0D66012C).FirstOrDefault()!;
                    var party = sav9.Where(i => i.Key == 0x3AA1A9AD).FirstOrDefault()!;
                    var spawn = sav9.Where(i => i.Key == 0x74ABBD32).FirstOrDefault()!;
                    var raid = sav9.Where(i => i.Key == 0xCAAC8800).FirstOrDefault()!;

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

                    Console.WriteLine($"Exported decrypted blocks from save file: {path}\\Blocks.bin\n" +
                        $"Exported Box EK9: {path}\\decrypted\\Box\n" +
                        $"Exported Party EK9: {path}\\Party\n" +
                        $"Exported Strong Spawns EK9: {path}\\Spawn\n" +
                        $"Exported Raid Block: {path}\\Raid\n" +
                        $"Press any key to exit.");
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