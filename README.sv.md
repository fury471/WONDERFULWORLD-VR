# Wonderland

En handgjord PC VR-sagopark — en förstapersons, komfortinriktad underbar värld med fjärilar, lotusmusik, blommagi, vänliga riddjur, växande svampar och ett blommande körsbärsträd.

**Språk:** [English](README.md) · [中文](README.zh-CN.md) · **Svenska**

---

## Om projektet

Wonderland (internt även känt som *Butterfly House* / *Wonderful World*) är en utforskningsinriktad VR-upplevelse för en spelare, byggd i **Unity 6** med **OpenXR** och **Universal Render Pipeline**. Spelaren vandrar genom en park-skiva som upplevs som sömlös, uppbyggd av sju sammanlänkade attraktionszoner — där varje zon är en liten magisk händelse i sig, inte ett uppdrag att checka av.

Designprinciperna är tydliga och oförändrade:

1. **Komfort först.** Stabil bildtakt, tunneleringsvinjett, teleport som standard, blink-övergångar vid skalbyte, ingen påtvingad rörelse.
2. **Upptäcktsbar förundran.** Varje region är en självständig interaktion som är värd att hitta — inte en kontrollpunkt att klara av.
3. **Stiliserat, inte fotorealistiskt.** Cel-shading (Toon Fantasy Nature + egna shaders) ovanpå Single Pass Instanced URP, finjusterat för Quest 3 Link.

Den aktuella produktionsscenen är [`Assets/_Project/World/Persistent/World_WonderlandPark.unity`](Assets/_Project/World/Persistent/World_WonderlandPark.unity) och levereras som **v1.0.0**.

---

## Höjdpunkter

- **Sju tematiska regioner i en sammanhängande park** — Människans entré, Blomsterträdgården, Lotusdammen, Kattträdgården, Fyrverkeriplatsen, Svamptillväxten, Körsbärsträdgården.
- **Tre olika spelarstorlekar** — växla mellan Normal, Liten (0,25×) och Stor (1,75×) med en 0,4 s blink-övergång; ögonhöjd, rörelsehastighet och interaktionsräckvidd anpassas automatiskt.
- **Musiksequenser i lotusdammen** — sju flytande lotusblad stämda till den diatoniska dur-skalan *do · re · mi · fa · sol · la · si*, spelas genom att skjuta kurvade vatten-magi-projektiler från handkontrollen. En låtväljare lottar fram en melodi att spela efter.
- **Riddjurssystem i Kattträdgården** — tre oberoende riddjur (Kissen, Hunden, Hästen), var och en med egen ridrutt, idle-vandring, hover-kontur och röst när man närmar sig. Ridning kräver **Liten** skala. Hästen kan kallas på från valfri plats med vänster X; katten monterar du automatiskt när du går fram.
- **Vägledningsfjärilar** — tre realtids-fjärilar som lyfter längs splined flygbanor när spelaren rider i närheten.
- **Kronblad- & pollenmagi** — håll in höger avtryckare riktat mot den stora blomman för att dra partiklar längs en kvadratisk Bézier-båge in i en svävande sfär framför ansiktet. Släpp för en av sex procedurella utbrott: `SpiralBloom`, `MathRibbon`, `TornadoVortex`, `AizawaFountain`, `DreamAttractor`, `GalaxyVeil`.
- **Svampplantering** — tryck för att så en enskild svamp, eller håll inne och släpp för en ring av 5–8. Kultivera valfri befintlig svamp med ytterligare ett avtryck.
- **Fyrverkerifinal** — sikta på den magiska mörsaren för att skicka iväg ett spiralformat eldband längs en kubisk Bézier-båge, och se sedan punktmolns-fyrverkeriet ta vid.
- **Körsbärskula** — en kristallkula som spawnar i runtime ovanför körsbärsträdet; när du aktiverar den spelas den fyrfasiga tillväxtanimationen och en virvel av kronblad upp.
- **Komfortlager för Quest 3 Link** — specialbyggd komfortprofil för förflyttning, hold-to-recenter, montering-medveten vyåtercentrering och en tunneleringsvinjett per rörelseläge.

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

### Förutsättningar

- Windows 10/11 med VR-kapabelt grafikkort
- Meta Quest 3 + Link-kabel (eller kompatibel USB-C-kabel med stöd för Quest Link)
- Skrivbordsappen [Meta Quest Link](https://www.meta.com/quest/setup/)
- Unity `6000.3.12f1` (installera via Unity Hub)
- Git med [Git LFS](https://git-lfs.com/) rekommenderat för grafiska resurser

### Klona

```bash
git clone https://github.com/fury471/WONDERFULWORLD-VR.git
cd WONDERFULWORLD-VR
```

### Öppna i Unity

1. Starta **Unity Hub** → *Lägg till projekt från disk* → välj den här mappen.
2. Öppna med Unity `6000.3.12f1`. Låt editorn importera vid första körningen (`Library/` byggs lokalt).
3. Säkerställ att det inte finns några kompileringsfel i Console.
4. Öppna scenen **[`Assets/_Project/World/Persistent/World_WonderlandPark.unity`](Assets/_Project/World/Persistent/World_WonderlandPark.unity)**.

### Kör på Quest 3 (Link)

1. Anslut Quest 3 med en Link-kabel och kontrollera att headsetet hittas av *Meta Quest Link*-appen.
2. Aktivera **Quest Link** inifrån headsetet.
3. Tryck **Play** i Unity. XR Origin ska följa ditt huvud och dina händer.
4. Du kan också köra den färdigbyggda Windows-binären på [`Builds/Windows/WONDERFULWORLD.exe`](Builds/Windows/WONDERFULWORLD.exe).

> Slutleveransens byggmål är **Windows / x86_64 / IL2CPP / Linear färgrymd / Single Pass Instanced**.

---

## Kontroller — snabbreferens

### Globalt

| Handling | Indata |
| --- | --- |
| Teleport (standard) | Tryck **vänster spak** framåt → släpp |
| Mjuk förflyttning (alt.) | Tryck **vänster spak** |
| Snäpp-vridning (standard) | **Höger spak** vänster/höger (30° per steg) |
| Mjuk vridning (alt.) | **Höger spak** vänster/höger |
| Skala: Normal ↔ Liten | **Höger spak — dubbelklick på spaktryck** |
| Skala: Normal ↔ Stor | **Höger spak — håll spaktrycket inne i 0,45 s** |
| Centrera vyn | **Håll höger B** i 0,40 s |
| Kalla på hästen | Tryck **vänster X** |
| Paus / systemmeny | Reserverad för **vänster Menu** (höger Menu ägs av Oculus-skalet) |

### Interaktioner (höger avtryckare med kontrollerns stråle)

| Plats | Effekt |
| --- | --- |
| Lotusblad | Spelar en av sju toner; bladet vaggar och vattnet får ringar |
| Blomma (håll) | Laddar en partikelsfär; släpp för en procedurell blomning |
| Svampzon | Tryck för en svamp; håll och släpp för en ring av 5–8 |
| Befintlig svamp | Tryck för att kultivera (+0,35× storlek, upp till 2,4×) |
| Fyrverkerimörsare | Skickar ett eldband in i enheten och drar igång showen |
| Körsbärskula | Kollapsar kulan och spelar trädets tillväxt + kronbladsvirveln |
| Riddjur (endast Liten skala) | Höger A för avmontering; vänster spak rör sig, höger spak vrider |

Fullständig referens: [`Docs/InteractionBindings.md`](Docs/InteractionBindings.md).

---

## Projektstruktur

```text
Assets/
  _Project/              # Allt teamägt innehåll bor här
    Art/                 # Shaders, material, texturer, props
    Audio/               # Musik, ljudeffekter, ambient loops
    Characters/          # Resurser för specifika varelser
    Core/                # Delade runtime-system (XR-rigg, komfortprofil, recenter)
    Editor/              # Verktyg i editorn för produktion
    Features/            # Modulära spelsystem (en mapp per system)
      CherryGarden/      #   - runtime-kristallkula + trädtillväxt + kronbladsvirvel
      Fireworks/         #   - magisk mörsare + uppskjutningsplatta + show
      Growth/            #   - svampsåningszon + kultivering
      LotusPond/         #   - 7-tons musiksequenser
      Mounts/            #   - katt/hund/häst-ridkontroller + vägledningsfjärilar
      ParticleVitality/  #   - kronblads-/pollenmagi
      ScaleShift/        #   - skalning Normal/Liten/Stor
      Weather/           #   - väderförinställningar + regional respons
    UI/                  # World-space-UI, anslagstavlor, lokalisering, systemmeny
    World/               # Mästerscen, terräng, regioner, delad världsgrafik
      Persistent/        #   - World_WonderlandPark.unity (produktionsscenen)
      Regions/           #   - Innehåll per region (FlowerField, LotusPond, ...)
      Shared/            #   - Belysning/ljud/material som återanvänds över parken
Builds/Windows/          # Senast levererad Windows-build
Docs/                    # Produktionsdokumentation (engelska)
Packages/                # Unitys paketmanifest
ProjectSettings/         # Unity-projektinställningar (Linear, SPI, IL2CPP m.m.)
```

Tredjepartsinnehåll (Toon Fantasy Nature, NamuFX, ithappy, XR Interaction Toolkit-exempel) ligger kvar i sina leverantörsmappar och **refereras** — inte kopieras — från produktionsscenen.

---

## Prestandamål

Runtime-målet är Quest 3 över Link-kabel. **Bildtakt är viktigare än genomsnittlig FPS** — tappade bilder, tearing, svart flimmer eller skakighet behandlas som blockerande för release.

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

- [Project Overview](Docs/PROJECT_OVERVIEW.md) — produktinramning, målplattform, aktuell scen, funktionsinventering
- [Build & Run](Docs/BUILD_AND_RUN.md) — Unity-version, Quest 3 Link-arbetsflöde, smoke test-steg
- [System Structure](Docs/SYSTEM_STRUCTURE.md) — mappstruktur, scenhierarki, kärnprefabs, runtime-system
- [Interaction Bindings](Docs/InteractionBindings.md) — varje spelarvänd interaktion i produktionsscenen
- [Cleanup & Standardization](Docs/CLEANUP_AND_STANDARDIZATION.md) — regler för hierarki, resurser, namngivning och dokumentation
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

Projektets källkod och teamskapade resurser ägs av Wonderland-teamet. Tredjepartsresurser omfattas av sina respektive licenser — se varje leverantörsmapp under `Assets/` samt [Asset Reference Audit](Docs/Asset_Reference_Audit.md).

---

*Skapat med omsorg för headsetet.*
