<p align="center">
  <img src="Docs/images/wonderland-cover.png" alt="Wonderland — Utforska här och ha det trevligt" width="100%">
</p>

<p align="center"><em>MAMF45 — Virtual Reality in Theory and Practice</em></p>

# Wonderland

> *Utforska här och ha det trevligt.*

En PC VR-sagopark — en förstapersons, komfortinriktad upplevelse med fjärilar, en musikplattform i lotusdammen, partikelmagi, riddjur, en handplanterad svamplund, ett blommande körsbärsträd och fyrverkerier.

**Språk:** [English](README.md) · [中文](README.zh-CN.md) · **Svenska**

---

## Om projektet

Wonderland (internt namn: *Wonderful World*) är en VR-utforskningsupplevelse för en spelare, byggd i **Unity 6** med **OpenXR** och **Universal Render Pipeline**. Spelaren vandrar genom en parkdel som upplevs som sömlös, uppbyggd av sju sammanlänkade attraktionszoner — där varje zon är en liten magisk händelse i sig, inte ett uppdrag att checka av.

Designprinciperna är:

1. **Komfort först.** Stabil bildtakt, tunneleringsvinjett per läge, teleport som standard, blink-övergångar vid skalbyte, ingen påtvingad rörelse.
2. **Upptäcktsbar förundran.** Varje zon är en självständig interaktion som är värd att hitta — inte en kontrollpunkt att klara av.
3. **Stiliserat, inte fotorealistiskt.** Cel-shading (Toon Fantasy Nature) ovanpå Single Pass Instanced URP.

Produktionsscenen är [`Assets/_Project/World/Persistent/World_WonderlandPark.unity`](Assets/_Project/World/Persistent/World_WonderlandPark.unity), släppt som **v1.0.0**.

---

## Höjdpunkter

- **Sju zoner i en sammanhängande park** — Välkomstingång, Magisk partikelträdgård, Lotusdamm, Djurskog (Kattträdgård), Vattenfall & fyrverkeriplats, Svamptillväxt, Körsbärsträdgård.
- **Tre spelarstorlekar** — växla mellan Normal, Liten (0,25×) och Stor (1,75×) via en gest på höger spaktryck (dubbelklick inom 0,32 s eller långtryck i 0,45 s); ögonhöjd, rörelsehastighet och interaktionsräckvidd anpassas automatiskt via en 0,4 s blink-övergång.
- **Musiksequenser i lotusdammen** — sju flytande lotusblad stämda till den diatoniska dur-skalan *do · re · mi · fa · sol · la · si*, spelas genom att skjuta kurvade vatten-magi-projektiler från valfri handkontroll. Ett åttonde blad är en låt-startare som slumpvis väljer en melodi att spela efter.
- **Riddjurssystem i Djurskogen** — tre oberoende riddjur (Kissen, Hunden, Hästen), var och en med idle-vandring, hover-kontur och en röst när man närmar sig. Skalkrav per djur: **katten och hunden kräver Liten skala, hästen kräver Normal skala**. Hästen kan kallas på från valfri plats med **vänster X**.
- **Vägledningsfjärilar** — tre realtids-fjärilar som lyfter längs splined flygbanor när spelaren närmar sig **medan hen rider på katten**.
- **Rosa kristall — kronblads- & pollenmagi** — håll in höger avtryckare riktat mot den magiska kristallen i Partikelträdgården; partiklar flödar in i en sfär framför ansiktet. Efter 3 s blir frisläppandet "laddat". Släpp för en av sex procedurella utbrott: `SpiralBloom`, `MathRibbon`, `TornadoVortex`, `AizawaFountain`, `DreamAttractor`, `GalaxyVeil`.
- **Svampplantering** — tryck för att så en svamp (jordmagi-projektil med flygtid 1,55 s). Håll inne ≥ 0,65 s och släpp för en laddad ring med 5–8 svampar inom 4 m radie. Tryck på en befintlig svamp för att kultivera den (+0,35× storlek, max 2,4×).
- **Fyrverkerimörsare** — sikta på den magiska mörsaren (räckvidd 36 m); ett spiralformat eldband flyger längs en kubisk Bézier-båge till enheten och drar igång punktmolns-fyrverkeriet.
- **Körsbärs-kristallkula** — en lysande kristall svävar ovanför körsbärsträdet (radie 1,05 m, spawnad av `CherryGardenCrystalOrbTrigger`). Höger avtryckare kollapsar den på 0,72 s och spelar den fyrfasiga tillväxtanimationen och en kronbladsvirvel.
- **Komfortlager för Quest 3 Link** — specialbyggd `QuestLocomotionComfortProfile`, håll-för-centrering på höger B (0,40 s), montering-medveten vyåtercentrering, och en tunneleringsvinjett per rörelseläge.

---

## Tekniska komponenter

| Område | Verktyg / Version |
| --- | --- |
| Spelmotor | Unity `6000.3.12f1` (Unity 6) |
| Renderingspipeline | Universal Render Pipeline `17.3.0` |
| Stereo-rendering | Single Pass Instanced |
| XR-runtime | OpenXR `1.16.1` via XR Management `4.5.4` |
| Interaktion | XR Interaction Toolkit `3.3.1`, XR Hands `1.7.3` |
| Indata | Unity Input System `1.19.0` |
| Skript-backend | IL2CPP (release), Mono (editor) |
| Mål-headset | Meta Quest 3 via Link-kabel, Windows PC VR |
| Bildtakt | Minst 72 Hz, mål 90 Hz |

---

## Snabbstart

> **Läs detta först.** Det här repot lagrar alla binära resurser (scener `.unity`, prefabs, material, texturer, ljud, FBX-modeller, `.asset`-filer) i **Git LFS**. En vanlig `git clone` lämnar kvar dem som ~100 byte stora pekarfiler och Unity kommer inte att kunna öppna projektet. Följ stegen nedan i ordning.

### Steg 0 — Installera förutsättningar (engångsinstallation)

| Vad | Var / Hur |
| --- | --- |
| Hårdvara | Windows 10/11-PC med VR-kapabelt grafikkort + Meta Quest 3 + Link-kabel (eller en USB-C 3.0+-kabel som stödjer Quest Link) |
| Unity Hub | <https://unity.com/download> |
| Unity Editor `6000.3.12f1` | Installera via Unity Hub → **Installs → Install Editor**. I modulsteget, **bocka i `Windows Build Support (IL2CPP)`**. (Du kan också bocka i *Documentation* och din favorit-IDE.) |
| Git for Windows | <https://git-scm.com/download/win> |
| Git LFS | <https://git-lfs.com/> — efter installationen, öppna valfri terminal och kör `git lfs install` en gång. |
| Meta Quest Link desktop | <https://www.meta.com/quest/setup/> |

### Steg 1 — Klona repot *med LFS-innehåll*

Öppna en terminal (PowerShell, Git Bash eller Windows Terminal) i mappen där du vill att projektet ska ligga, och kör:

```bash
git lfs install                                              # en gång per maskin; säkert att köra om
git clone https://github.com/fury471/WONDERFULWORLD-VR.git
cd WONDERFULWORLD-VR
git lfs pull                                                 # hämtar alla LFS-spårade binära resurser
```

Förväntad nedladdning: **~2–3 GB**. Själva `git clone` är snabbt; `git lfs pull` är det långa steget.

**Kontroll.** När `git lfs pull` är klart ska produktionsscenen vara en riktig binärfil, inte en pekare:

```bash
# PowerShell
(Get-Item Assets/_Project/World/Persistent/World_WonderlandPark.unity).Length
# Git Bash / WSL
wc -c < Assets/_Project/World/Persistent/World_WonderlandPark.unity
```

Ett friskt resultat är flera **megabyte**. Om du bara ser några hundra byte hämtades inte LFS — kör `git lfs pull` igen.

> Har du redan klonat utan LFS? Du behöver inte klona om. Gå in i mappen och kör `git lfs install && git lfs pull`.

### Steg 2 — Öppna projektet i Unity

1. Starta **Unity Hub** → **Add** → **Add project from disk** → välj `WONDERFULWORLD-VR`-mappen.
2. Projektkortet visar editorversionen `6000.3.12f1`. Om den inte är installerad erbjuder sig Unity Hub att installera den — acceptera, och **se till att `Windows Build Support (IL2CPP)` är ibockat** i modullistan.
3. Klicka på projektet för att öppna det. Förstaimporten bygger det lokala `Library/` från grunden och **tar typiskt 10–30 minuter** beroende på disk- och CPU-hastighet. **Stäng inte Unity under importen.**
4. När importen är klar, titta på **Console** — det ska **inte finnas några kompileringsfel**.
5. I **Project**-fönstret, dubbelklicka på [`Assets/_Project/World/Persistent/World_WonderlandPark.unity`](Assets/_Project/World/Persistent/World_WonderlandPark.unity) för att läsa in produktionsscenen.

### Steg 3 — Spela på Quest 3 via Link

1. Anslut din Quest 3 till PC:n med en Link-kabel (eller en USB-C 3.0+-kabel som stödjer Quest Link).
2. Öppna skrivbordsappen **Meta Quest Link** i Windows och bekräfta att headsetet hittas (status: *Connected*).
3. Ta på dig headsetet. Acceptera dialogrutan **"Enable Quest Link?"**, eller öppna headsetets universalmeny → **Snabbinställningar → Quest Link** och starta en session.
4. Gå tillbaka till Unity på PC:n och tryck **▶ Play**. Ta på dig headsetet inom några sekunder — XR Origin ska följa ditt huvud och båda händer.

### (Valfritt) Bygg en Windows-version själv

Repot levereras inte med någon färdigbyggd binär (`Builds/` ligger i `.gitignore`). Så här gör du en själv:

1. I Unity, öppna **File → Build Profiles** (eller **Build Settings**).
2. Välj **Windows, Mac, Linux** med Target Platform **Windows** och arkitektur **x86_64**.
3. Bekräfta **Scripting Backend = IL2CPP** och **Color Space = Linear** under *Project Settings → Player*.
4. Klicka **Build** och välj en utdatamapp (förslag: `Builds/Windows/`).

> Byggmål: **Windows / x86_64 / IL2CPP / Linear färgrymd / Single Pass Instanced**.

### Felsökning

| Symtom | Trolig orsak | Åtgärd |
| --- | --- | --- |
| Rosa/magenta material, saknade skript, "Could not extract GUID"-fel | LFS-objekten hämtades inte | Kör `git lfs install` och sedan `git lfs pull` inne i repot; högerklicka sedan `Assets/` → *Reimport* i Unity |
| Unity Hub säger att editorversionen saknas | `6000.3.12f1` är inte installerad | Installera den via Unity Hub → **Installs → Install Editor**, bocka i **Windows Build Support (IL2CPP)** |
| Headsetet upptäcks inte av Quest Link | Kabeln är USB-C 2.0, Link är avstängt eller en drivrutinsstrul | Använd en Quest Link- eller USB-C 3.0+-kabel; i headsetet, slå på *Inställningar → System → Quest Link*; starta om Meta Quest Link-appen |
| Många kompileringsfel direkt efter första öppning | Skadat eller halvimporterat `Library/` | Stäng Unity, ta bort `Library/`, `Temp/`, `obj/`, öppna projektet igen och låt det importera färdigt |
| Svart flimmer, tearing eller låg bildfrekvens i headsetet | Prestanda- / inställningsproblem | Följ triageflödet i [`Docs/VR_PERFORMANCE_GUIDE.md`](Docs/VR_PERFORMANCE_GUIDE.md) |
| `git lfs pull` är långsam eller låser sig | LFS-bandbredd eller nätverksproblem | Kör `git lfs pull` igen; LFS återupptar där den slutade |

---

## Kontroller — snabbreferens

### Globalt

| Handling | Indata |
| --- | --- |
| Teleport (standard) | Tryck **vänster spak** framåt → släpp |
| Mjuk förflyttning (alt.) | Tryck **vänster spak** (`smoothMoveSpeed = 1,6 m/s`) |
| Snäpp-vridning (standard) | **Höger spak** vänster/höger (`snapTurnAmount = 30°`) |
| Mjuk vridning (alt.) | **Höger spak** vänster/höger (`smoothTurnSpeed = 45°/s`) |
| Skala: Normal ↔ Liten | **Höger spaktryck — dubbelklick inom 0,32 s** |
| Skala: Normal ↔ Stor | **Höger spaktryck — långtryck ≥ 0,45 s** |
| Centrera vyn | **Håll höger B** i 0,40 s |
| Kalla på hästen | Tryck **vänster X** |
| Systemmeny | Tryck **vänster Menu** (höger Menu ägs av Oculus-skalet) |

### Zonsinteraktioner (kontrollerstråle + höger avtryckare)

| Mål | Effekt |
| --- | --- |
| Lotusblad | Spelar en av sju toner; bladet vaggar och vattnet får ringar |
| Partikelkristall (tryck eller håll) | Laddar en partikelsfär; släpp för ett procedurellt utbrott |
| Svampzon-mark | Tryck för 1 svamp; håll och släpp för en ring med 5–8 |
| Befintlig svamp | Tryck för att kultivera (+0,35× storlek, max 2,4×) |
| Fyrverkerimörsare | Skickar ett eldband in i enheten och drar igång showen |
| Körsbärskristall | Kollapsar kulan och spelar trädets tillväxt + kronbladsvirveln |
| Riddjur | Höger avtryckare för att montera (skalkrav per djur); höger A för att avmontera; vänster spak rör sig, höger spak vrider |

Fullständig referens: [`Docs/InteractionBindings.md`](Docs/InteractionBindings.md).

---

## Projektstruktur

```text
Assets/
  _Project/              # Allt teamägt innehåll bor här
    Art/                 # Shaders, material, texturer, props
    Audio/               # Musik, ljudeffekter, ambient loops
    Characters/          # Resurser för specifika varelser
    Core/                # Delade runtime-system
      Runtime/           #   - GameFlowManager, ParkAttractionState
      XR/                #   - XR-rigg, komfortprofil, recenter, ray-broker, haptics, performance-bootstrap
    Editor/              # Verktyg i editorn för produktion
    Features/            # Modulära spelsystem (en mapp per system)
      CherryGarden/      #   - runtime-kristallkula + trädtillväxt + kronbladsvirvel
      Fireworks/         #   - magisk mörsare + uppskjutningsplatta + punktmolns-show
      Growth/            #   - svampsåningszon + kultivering
      LotusPond/         #   - 7-tons diatonisk musiksequenser
      Mounts/            #   - katt/hund/häst-ridkontroller, häst-kallelse, vägledningsfjärilar
      ParticleVitality/  #   - rosa kristall: kronblads-/pollenmagi
      ScaleShift/        #   - skalning Normal/Liten/Stor
      Weather/           #   - väderförinställningar + regional respons
    UI/                  # World-space-UI: WelcomePanel, systemmeny, anslagstavlor, lokalisering (EN/ZH/SV)
    World/               # Mästerscen, terräng, regioner, delad världsgrafik
      Persistent/        #   - World_WonderlandPark.unity (produktionsscenen)
      Regions/           #   - Innehåll per region
        CatRoute/        #     (scenens root: Region_CatGarden)
        FireworksClearing/  #  (scenens root: Region_FireworksClearing — vattenfall + fyrverkerier)
        FlowerField/     #     (scenens root: Region_FlowerGarden — rosa kristall)
        HumanEntry/      #     (staging-innehåll; entrén realiseras via UI/WelcomePanel)
        LotusPond/       #     (scenens root: Region_LotusPond)
        MushroomGrove/   #     (scenens root: Region_MushroomGrowth)
        Terrain/         #     (terrängdelar)
      Shared/            #   - Belysning/ljud/material som återanvänds över parken
Builds/Windows/          # Senast levererad Windows-build (WONDERFULWORLD.exe)
Docs/                    # Produktionsdokumentation (engelska)
Packages/                # Unitys paketmanifest
ProjectSettings/         # Unity-projektinställningar (Linear, SPI, IL2CPP m.m.)
```

Tredjepartsinnehåll (Toon Fantasy Nature, NamuFX, ithappy, XR Interaction Toolkit-exempel) ligger kvar i sina leverantörsmappar och **refereras** — inte kopieras — från produktionsscenen.

---

## Prestandamål

Runtime-målet är Quest 3 över Link-kabel.

| Mått | Minimum | Mål |
| --- | --- | --- |
| Stabil refresh på headset | 72 Hz | 90 Hz |
| Renderingsskala | 1,0 | 1,0 |
| MSAA | 4× | 4× |
| HDR | av | av |
| Opaque texture | av (om det inte krävs) | av |
| SRP Batcher | på | på |
| Stereo-rendering | Single Pass Instanced | Single Pass Instanced |

Arbetsflöde för profilering och triage: [`Docs/VR_PERFORMANCE_GUIDE.md`](Docs/VR_PERFORMANCE_GUIDE.md).

---

## Dokumentation

All underhållen dokumentation ligger i [`Docs/`](Docs/) och är endast på engelska enligt teamets policy:

- [Project Overview](Docs/PROJECT_OVERVIEW.md) — produktinramning, målplattform, aktuell scen, regioninventering
- [Build & Run](Docs/BUILD_AND_RUN.md) — Unity-version, Quest 3 Link-arbetsflöde, smoke test-steg
- [System Structure](Docs/SYSTEM_STRUCTURE.md) — mappstruktur, scenhierarki, kärnprefabs, runtime-system
- [Interaction Bindings](Docs/InteractionBindings.md) — varje spelarvänd interaktion i produktionsscenen, korsverifierad mot skripten
- [Cleanup & Standardisation](Docs/CLEANUP_AND_STANDARDIZATION.md) — regler för hierarki, resurser, namngivning och dokumentation
- [Asset Reference Audit](Docs/Asset_Reference_Audit.md) — aktuell ögonblicksbild av externa beroenden
- [VR Performance Guide](Docs/VR_PERFORMANCE_GUIDE.md) — profileringsflöde, målbudgetar, triage-steg
- [Scale Shift Controller Flow](Docs/ScaleShiftCharacterControllerFlow.md) — säker `CharacterController`-mutationsordning vid skalbyte
- [Final Release Checklist](Docs/FINAL_RELEASE_CHECKLIST.md) — godkännande i Editor, Play Mode och via Quest 3 Link

---

## Editor-verktyg

Produktionsverktygen i editorn ligger under Unity-menyn **Wonderful World > Production**:

- *Create Standard Project Folders*
- *Generate Production Audit*
- *Generate Asset Reference Audit*
- *Internalize Referenced Temp Art*
- *Normalize Main Scene Hierarchy*

Flytta och döp alltid om Unity-resurser via **Project-fönstret** eller `AssetDatabase` — aldrig genom operativsystemet — så att `.meta`-filer och GUID-referenser överlever.

---

## Tack till

Wonderland bygger på generöst licensierat tredjepartsinnehåll. De viktigaste byggstenarna är:

- **Toon Fantasy Nature** — stiliserad miljögrafik (träd, stenar, paviljonger, gungor, dekorationer).
- **NamuFX – Stylized Water Effects** — vattenmaterial, ringar, stänk och bubbeleffekter.
- **ithappy – Animals FREE** — mesh, material och animationskontroller för katt, hund och häst.
- **Unity XR Interaction Toolkit – Starter Assets** och **XR Device Simulator** — kontrollerprefabs, teleportreticle, källan till tunneleringsvinjetten, hand-expression-captures.
- **Liberation Sans (TextMesh Pro)** — fallback-typsnitt.
- **Butterfly (Ulysses)** — basmesh och animationskontroller för fjärilen.
- **freesound.org – `jaz_the_man_2`** — lotusdammens tonprover (`do`, `re`, `mi`, `fa`, `sol`, `la`, `si`).

Allt leverantörsinnehåll ligger kvar i sin ursprungliga mapp under `Assets/`. Se [`Docs/Asset_Reference_Audit.md`](Docs/Asset_Reference_Audit.md) för den fullständiga ögonblicksbilden av beroenden.

---

## Bidra

Detta repo är produktionskällan för v1.0.0-leveransen. Innan du öppnar en pull request:

1. Skapa en branch från `main`.
2. Öppna projektet i Unity `6000.3.12f1` och bekräfta att det inte finns några kompileringsfel.
3. Kör **Wonderful World > Production > Generate Production Audit** och **Generate Asset Reference Audit**.
4. Kör [smoke-testet](Docs/BUILD_AND_RUN.md#smoke-test) via Quest 3 Link.
5. Dela när det är möjligt upp commits för hierarki, resursorganisation, dokumentation och prestanda var för sig.

---

## Licens

Släppt under [MIT-licensen](LICENSE). Tredjepartsresurser omfattas av sina respektive licenser — se varje leverantörsmapp under `Assets/` samt [Asset Reference Audit](Docs/Asset_Reference_Audit.md).
