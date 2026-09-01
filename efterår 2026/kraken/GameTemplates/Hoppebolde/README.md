# Hoppebolde

Et lille netvaerksspil: alle spillere har hver deres markoer og styrer den med pilene.
Tryk Enter, og der skyder en hoppebold ud som hopper rundt mellem kanterne - det er
vaerten der ejer boldene, saa alle ser praecis de samme. P viser hvem der er med.

## Saadan faar du spillet

1. Kopier HELE `Hoppebolde`-mappen ind i `MyGames/` - i **Stifinder** eller terminalen,
   ALDRIG inde fra Visual Studio.
2. Kopier saa `MyGames/Hoppebolde/program.cs` op i roden af `kraken/` - oven i den der
   ligger der.
3. `dotnet run` - og vaelg et spil i listen, eller "Start dit eget spil" oeverst.

Eller nemmest: bed Claude - "kopier Hoppebolde-skabelonen ind som mit spil".

Ret ALDRIG i skabelonen her i mappen - kopier den ud, og rod saa alt det du vil.

## Proev at aendre...

- Farven og stoerrelsen paa din markoer (i `MarkoerPerSpiller`).
- Boldenes fart og stoerrelse - eller lad dem blive STOERRE for hvert hop.
- Giv boldene lyd naar de rammer en kant (`Assets.Tone` i `HoppeBold`).
- Lad boldene forsvinde efter 10 sekunder (`context.After` i `OnAdded`).
- Lad markoererne skubbe til boldene naar de roerer dem (`OnCollision` + `IPushable`).
- Saet en kode paa dit spil naar du starter som vaert - og se laasen i spillelisten.
- En taeller oeverst: hvor mange bolde er der i luften? (`TagCounter` med `Tag = "hoppebold"`.)
- Det vilde: goer det til et spil. Foerst til 10 bolde? Undgaa boldene? Du bestemmer.
