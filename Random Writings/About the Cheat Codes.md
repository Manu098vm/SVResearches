I started poking at the ROM gamecode to understand what the statements in there exactly meant. 

By forcing some if-condition I came to the conclusion that the games generates Pokémon PID based on the following pseudo enumeration:

`PidType {
	ShinyLocked = 1,
	ShinyForced = 2,
	Random = 3,
	ShinyHighChance = 4,
}`

There should be another PidType, being "FixedPid", which I assume being either value 0 or value 5.

The ShinyHighChance PidType was particular. If the game branches to that case, an RNG.Next(2) is made.

Two possible values are returned by that RNG call, and based on that the game branches to PidType1 (ShinyLocked) or PidType2 (ShinyForced), giving a de facto 50-50 chance of obtaining a Shiny Pokémon.


From there it was pretty straight forward editing the code so it always branches to PidType4, then from there to PidType2. 

This means that all the Pokémon that pass through `0x710089c3a0` to be generated, will have their PidType overwritten to ShinyForced, if they were Shiny Locked.


Trying to take a less invasive approach to make every Pokémon shiny, I tried to NOP (literally, makes it do nothing) the `Rolls = Rolls + 1` so the PID would've been rerolled until it was Shiny.

![img4](https://user-images.githubusercontent.com/52102823/202528008-10a093cb-1851-4578-8bcf-2c92e11c12d6.png)

This approach works great with Raids, but not with the Overworld Pokémon. Due to overworld being generated in huge amounts everywhere, this made the game to calculate a huge amount of PIDs making it to lag enormously.
