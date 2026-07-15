# SDXC-copy

Ett Windows-program som ligger i systemfältet, upptäcker när ett SDXC-kort
från en kamera sätts i och kopierar bilderna till rätt plats på datorn.

Designen beskrivs i [DESIGN.md](DESIGN.md). Kortversionen:

- **Kortet förändras aldrig** — allt läses strikt skrivskyddat.
- Kameran känns igen via EXIF (modell + serienummer); varje kamera har en
  egen grundkatalog och ett eget mappmönster, t.ex.
  `{ÅÅÅÅ}/{MM}/{ÅÅÅÅ}-{MM}-{DD}` → `2026/07/2026-07-02/`.
- Redan kopierade filer (samma namn och storlek i målmappen) hoppas över;
  namnkollisioner får ett suffix som `IMG_0001 (2).JPG` — inget skrivs över.
- Import startar först efter en bekräftelse i aviseringen, och en
  avisering visar resultatet.

## Ladda hem

Färdigbyggda versioner finns under
[Releases](https://github.com/Elof-com/SDXC-copy/releases) — ladda ner
zip-filen, packa upp `SDXC-copy.exe` och starta. Ingen installation och
ingen .NET-miljö behövs (Windows 10 version 1809 eller senare, 64-bitars).

Eftersom filen inte är kodsignerad visar Windows SmartScreen en varning
första gången — välj **Mer information → Kör ändå**.

Nya releaser byggs automatiskt av GitHub Actions när en versionstagg
pushas (`git tag v0.1.0 && git push origin v0.1.0`).

## Bygga

Kräver bara den fria [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
(kompilatorn ingår — inget behöver köpas).

```
dotnet build SdxcCopy.sln -c Release
```

Körbar fil som enda `.exe` (kräver ingen separat .NET-installation på måldatorn):

```
dotnet publish src/SdxcCopy -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

Resultatet hamnar under `src/SdxcCopy/bin/Release/net8.0-windows10.0.17763.0/win-x64/publish/`.

## Använda

1. Starta `SDXC-copy.exe` — en ikon läggs i systemfältet vid klockan.
2. Sätt i ett SDXC-kort. Första gången en kamera dyker upp öppnas en guide
   där du väljer grundkatalog (och mappmönster om du vill ändra standard).
3. Därefter räcker det att sätta i kortet och klicka **Starta import** i
   aviseringen. När kopieringen är klar visas resultatet.
4. Högerklicka på ikonen för **Inställningar** (kameror, kataloger,
   mappmönster, autostart med Windows) eller **Sök efter kort nu**.

Inställningarna sparas i `%APPDATA%\SDXC-copy\config.json`.

## Versioner

Alla versioner finns att hämta under
[Releases](https://github.com/Elof-com/SDXC-copy/releases).

| Version | Datum | Ändringar |
|---------|------------|-----------|
| 1.0.2 | 2026-07-13 | Programfilen heter nu `SDXC-copy.exe` (tidigare `SdxcCopy.exe`); autostartposten i registret uppdaterar sig själv om programmet byter namn eller plats. |
| 1.0.1 | 2026-07-03 | Programikon: på exe-filen, i systemfältet och på alla fönster. |
| 1.0.0 | 2026-07-02 | Första utgåvan. Bevakning av kortinsläpp, kameraigenkänning via EXIF (modell + serienummer), egen grundkatalog och konfigurerbart mappmönster per kamera, dubblettskydd, kollisionssäker kopiering (kortet lämnas alltid orört), förloppsfönster, aviseringar och autostart med Windows. |

## Licens

Copyright Rikard Elofsson (elof@elof.com).

SDXC-copy är licensierat under
[PolyForm Noncommercial License 1.0.0](LICENSE.md) — fritt att använda,
ändra och sprida vidare för icke-kommersiella ändamål. Alla kopior och
vidareutvecklingar måste behålla licensvillkoren och raden

> Required Notice: Copyright Rikard Elofsson (elof@elof.com)

Kommersiell användning kräver ett separat avtal med upphovsrättsinnehavaren.
