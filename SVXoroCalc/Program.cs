using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

public static class SVXoroCalc
{
    const int MAX_SHINYROLLS = 1;
    const int MAX_LEGALITYROLLS = 1;

    const ushort UserTID = 12345; //Player's classic TID
    const ushort UserSID = 54321; //Player's classic SID

    const int BlockSize = 0xC98;
    const int SeedDistance = 0x1C;
    const int SeedSize = 0x4;

    const int NFlawless = 1;
    const int NIV = 6;
    const int IVMaxValue = 31;
    const int NAbility = 2;
    const int NNature = 25;

    const int UNSET = -1;

    public static void Main()
    {
        Console.WriteLine("Basic SV Raid RNG Calculator by SkyLink98\n\n");

        Console.WriteLine("This program has been discontinued in favor of Tera Finder.\nDo you want to download the latest Tera Finder release?\n[Y\\n]:");
        var str = Console.ReadLine();
        if(!string.IsNullOrWhiteSpace(str) && (str.ToLower().Equals("y") || str.ToLower().Equals("yes")))
        {
            Process.Start(new ProcessStartInfo { FileName = @"https://github.com/Manu098vm/Tera-Finder/releases/latest", UseShellExecute = true });
        }

        while (true)
        {
            Console.WriteLine($"\nSelect mode:\n" +
                $"0 - Close program\n" +
                $"1 - Calculate Pokémon Details and Stats\n" +
                $"2 - Compute a Shiny seed\n" +
                $"3 - Check Encounter Legality");

            str = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(str))
            {
                if (str.Equals("0"))
                {
                    return;
                }
                else if (str.Equals("1"))
                {
                    Console.WriteLine("\nInput a seed:");
                    str = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(str))
                        continue;
                    var seed = Convert.ToUInt32(str, 16);
                    CalculateFromSeed(seed);
                    Console.WriteLine("Done.\n");
                }
                else if (str.Equals("2"))
                {
                    Console.WriteLine("\nInput a seed (leave blank for default):");
                    str = Console.ReadLine();
                    var seed = string.IsNullOrWhiteSpace(str) ? 0 : Convert.ToUInt32(str, 16);
                    ComputeShinySeed(seed, showDetails: true);
                    Console.WriteLine("Done.\n");
                }
                else if (str.Equals("3"))
                {
                    Console.WriteLine("////////////////////////////////////////\n" +
                        "Required parameters are Encryption Constant (EC) and PID. You can ignore other inputs.\n" +
                        "////////////////////////////////////////");

                    int HP, ATK, DEF, SpA, SpD, SPE, Ability;
                    HP = ATK = DEF = SpA = SpD = SPE = Ability = UNSET;

                    var Nature = SVXoroCalc.Nature.None;
                    Console.WriteLine("\nInput the Encryption Constant (EC):");
                    str = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(str))
                        continue;
                    var EC = Convert.ToUInt32(str, 16);
                    Console.WriteLine("Input the PID:");
                    str = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(str))
                        continue;
                    var PID = Convert.ToUInt32(str, 16);
                    Console.WriteLine("Input the IV_HP:");
                    str = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(str))
                        HP = Convert.ToInt16(str);
                    Console.WriteLine("Input the IV_ATK:");
                    str = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(str))
                        ATK = Convert.ToInt16(str);
                    Console.WriteLine("Input the IV_DEF:");
                    str = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(str))
                        DEF = Convert.ToInt16(str);
                    Console.WriteLine("Input the IV_SpA:");
                    str = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(str))
                        SpA = Convert.ToInt16(str);
                    Console.WriteLine("Input the IV_SpD:");
                    str = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(str))
                        SpD = Convert.ToInt16(str);
                    Console.WriteLine("Input the IV_SPE:");
                    str = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(str))
                        SPE = Convert.ToInt16(str);
                    Console.WriteLine("Input the Ability (1/2):");
                    str = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(str))
                        Ability = Convert.ToInt16(str);
                    Console.WriteLine("Input the Nature:");
                    str = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(str))
                        Enum.TryParse<SVXoroCalc.Nature>(str, out Nature);

                    CheckLegality(EC, PID, new int[] {HP, ATK, DEF, SpA, SpD, SPE}, Ability, Nature);
                    Console.WriteLine("Done.\n");
                }
            }
        }
    }

    public static void CalculateFromSeed(uint seed = 0)
    {
        int i;
        var xoro = seed == 0 ? new Xoroshiro() : new Xoroshiro(seed);
        var personalRnd = xoro.NextUInt();
        var fakeTrainer = xoro.NextUInt();

        var rareRnd = (uint)0;
        var isShiny = false;
        for (i = 0; i < MAX_SHINYROLLS; i++)
        {
            rareRnd = xoro.NextUInt();
            isShiny = IsShiny(rareRnd, fakeTrainer);
            if (isShiny)
            {
                if(!IsShiny(rareRnd))
                    rareRnd = ForceShiny(rareRnd);
                break;
            }
            else
                if(IsShiny(rareRnd))
                    rareRnd = ForceNonShiny(rareRnd);
        }


        int[] ivs = { UNSET, UNSET, UNSET, UNSET, UNSET, UNSET };
        var determined = 0;
        while (determined < NFlawless)
        {
            var idx = xoro.NextInt(NIV);
            if (ivs[idx] != UNSET)
                continue;
            ivs[idx] = 31;
            determined++;
        }

        for (i = 0; i < ivs.Length; i++)
            if (ivs[i] == UNSET)
                ivs[i] = (int)xoro.NextInt(IVMaxValue + 1);

        var IV_HP = ivs[0];
        var IV_ATK = ivs[1];
        var IV_DEF = ivs[2];
        var IV_SPA = ivs[3];
        var IV_SPD = ivs[4];
        var IV_SPE = ivs[5];

        var ability = xoro.NextInt(NAbility);
        var nature = xoro.NextInt(NNature);
        var nature2 = xoro.NextInt(NNature);

        var userString = isShiny ? $"(For TID {UserTID} and SID {UserSID} - Change settings in the code)" : "";

        Console.WriteLine($"\nEC: {personalRnd:X}\n" +
            $"PID: {rareRnd:X} {userString}\n" +
            $"Shiny: {isShiny}\n" +
            $"IVs: {IV_HP}/{IV_ATK}/{IV_DEF}/{IV_SPA}/{IV_SPD}/{IV_SPE}\n" +
            $"Ability: {ability + 1}\n" +
            $"Nature (without Gender call): {(Nature)nature}\n" +
            $"Nature (with standard Gender call): {(Nature)nature2}\n" +
            $"[Nature calculation might be inaccurate for some Pokémon]");
    }

    public static void ComputeShinySeed(uint seed = 0, bool showDetails = false)
    {
        var prev = seed == 0 ? Xoroshiro.XOROSHIRO_CONST : seed;
        var isShiny = false;
        var personalRnd = (uint)0;
        uint fakeTrainer;
        uint rareRnd;
        Xoroshiro xoro;

        while (!isShiny)
        {
            xoro = new Xoroshiro(prev);
            personalRnd = xoro.NextUInt();
            seed = xoro.NextUInt();
            fakeTrainer = seed;
            prev = ReverseSeed(seed);

            for (var j = 0; j < MAX_SHINYROLLS; j++)
            {
                rareRnd = xoro.NextUInt();
                isShiny = IsShiny(rareRnd, fakeTrainer);
                if (isShiny)
                    break;
            }
        }

        seed = ReverseSeed(personalRnd);
        Console.WriteLine($"Shiny Seed: {seed:X}");

        if(showDetails)
            CalculateFromSeed(seed);
    }

    public static void CheckLegality(uint EC, uint PID, int[] IVS, int ABILITY, Nature NATURE)
    {
        var originalSeed = ReverseSeed(EC);
        var isValid = IsValidEncounter(EC, PID, IVS, ABILITY, NATURE);
        var reversedString = GetValidationString(originalSeed, isValid);
        Console.WriteLine($"Original Seed: {reversedString:X}");
    }

    private static uint ReverseSeed(uint EC) => EC - (uint)(Xoroshiro.XOROSHIRO_CONST & 0xFFFFFFFF);

    private static bool IsValidEncounter(uint EC, uint PID, int[] IVS, int ABILITY, Nature NATURE)
    {
        var reversed = ReverseSeed(EC);
        var xoro = new Xoroshiro(reversed);

        xoro.Next();
        var fakeTrainer = xoro.NextUInt();

        var rareRnd = (uint)0;
        for (int i = 0; i < MAX_LEGALITYROLLS; i++)
        {
            rareRnd = xoro.NextUInt();
            var isShiny = IsShiny(rareRnd, fakeTrainer);

            if (isShiny)
            {
                if (!IsShiny(rareRnd))
                    rareRnd = ForceShiny(rareRnd);
                break;
            }
            else
            {
                if (IsShiny(rareRnd))
                    rareRnd = ForceNonShiny(rareRnd);
            }

            if (rareRnd == PID)
                break;
        }

        if (rareRnd != PID)
            return false;

        if (!IsUnset(IVS)) 
        {
            int[] ivs = { UNSET, UNSET, UNSET, UNSET, UNSET, UNSET };
            var determined = 0;
            while (determined < NFlawless)
            {
                var idx = xoro.NextInt(NIV);
                if (ivs[idx] != UNSET)
                    continue;
                ivs[idx] = 31;
                determined++;
            }

            for (var i = 0; i < ivs.Length; i++)
                if (ivs[i] == UNSET)
                    ivs[i] = (int)xoro.NextInt(IVMaxValue + 1);

            if (!Enumerable.SequenceEqual(ivs, IVS))
                return false;

            if (ABILITY != UNSET)
            {
                var ability = (int)xoro.NextInt(NAbility);
                if (ability+1 != ABILITY)
                    return false;

                if (NATURE != Nature.None)
                {
                    var nature = (Nature)xoro.NextInt(NNature);
                    var nature2 = (Nature)xoro.NextInt(NNature);

                    if (nature != NATURE && nature2 != NATURE)
                        return false;
                }
            }
        }

        return true;
    }

    private static bool IsShiny(uint PID, ushort TID = UserTID, ushort SID = UserSID) =>
        (ushort)(SID ^ TID ^ (PID >> 16) ^ PID) < 16;

    private static bool IsShiny(uint PID, uint FTID) =>
        IsShiny(PID, (ushort)(FTID >> 16), (ushort)(FTID & 0xFFFF));

    private static uint ForceShiny(uint PID, uint TID = UserTID, uint SID = UserSID) =>
        ((TID ^ SID ^ (PID & 0xFFFF) ^ 1) << 16) | (PID & 0xFFFF);

    private static uint ForceNonShiny(uint PID) => PID ^ 0x10000000;

    private static string GetValidationString(uint seed, bool valid)
    {
        if (valid)
            return $"{seed:X} (VALID)";
        return "(INVALID)";
    }

    private static bool IsUnset(int[] array)
    {
        foreach (var i in array)
            if (i == UNSET)
                return true;
        return false;
    }

    public enum Nature : byte
    {
        Hardy = 0,
        Lonely = 1,
        Brave = 2,
        Adamant = 3,
        Naughty = 4,
        Bold = 5,
        Docile = 6,
        Relaxed = 7,
        Impish = 8,
        Lax = 9,
        Timid = 10,
        Hasty = 11,
        Serious = 12,
        Jolly = 13,
        Naive = 14,
        Modest = 15,
        Mild = 16,
        Quiet = 17,
        Bashful = 18,
        Rash = 19,
        Calm = 20,
        Gentle = 21,
        Sassy = 22,
        Careful = 23,
        Quirky = 24,
        None = 25,
    }
}