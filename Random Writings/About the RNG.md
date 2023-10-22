My first impression was that the games used Xoroshiro for pretty much all the Pokémon generation. I was wrong.

Even if the Xoroshiro constants are always initialized, they get ignored if there is not a valid Xoroshiro Seed as `param_2` in the `0x7100d0a518` function.

![img1](https://user-images.githubusercontent.com/52102823/202528343-29c055e2-f1d4-4ee0-923c-4e52c9c42b1e.png)


In this case, the game branches code from a pointer. I followed that pointer with the GDB debugger, and that pointed me to the function `0x7101ee08b0`. CryptoRNG. :-(

![img2](https://user-images.githubusercontent.com/52102823/202528363-e3eb8fa7-6617-4362-8a2b-05c1a94d2895.png)


I still had hope due to the Xoroshiro initialization in the Else statement. 

I noted that if a Xoroshiro states do exists, then that is always used for the RNG calls in the Pokémon generation. If it does not exists, CryptoRNG is called.

Theoretically we can make a patch to force the Xoroshiro initialization, but I'm still unsure how it would get seeded. It might still not be manipulable even with the mentioned patch.


I used GDB breakpoints to check whenever the CryptoRNG got called and whenever the Xoroshiro got called. Xoroshiro was there for Raids. Yay!

I started to do experiments with raid seed only to see that only the Pokémon generation was actually predictable.

While it is possible to advance the Raid seeds by advancing the date in the system settings (no more weird connection tricks like in SwSh), the seeds becomes completely weirdly unpredictable by advancing over the second day.

I assumed the seeding being Crypto, but note that it's only an assumption based on empiric tests and ~~I still do not have any code evidences~~. [Anubis confirmed it is Crypto].

While at the time of writing I'm under the impression nothing in this game is legally RNG manipulable, we can indeed check legality for Pokémon obtained through Raids. This is something.

We can also easily inject prefabricated seeds to encounter a wanted Pokémon. While this would result in a legal Pokémon, it's notably still cheating.

I'm still unsure if the Host seed get sent to Guests in the multiplayer functionalities. Based on the SwSh games, that's most likely the case. [Edit: it is].


A test I did was injecting the same seed in all the raids. All the raids resulted in the very same Pokémon and Difficulty Stars. This means both the species and the stars are likely to be determined by the Xoroshiro seed.