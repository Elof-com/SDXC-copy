# SDXC-copy — Designdokument

Program för att kopiera bilder från kamerors SDXC-kort till dator.

Status: **designen beslutad och implementerad** — källkoden finns under `src/SdxcCopy/`.

## Grundprinciper

1. **Minneskortet får aldrig förändras.** Programmet öppnar allt strikt
   läsande, skriver aldrig till kortet och raderar aldrig något där.
2. **Ingenting skrivs över och ingenting tappas bort** på datorsidan.
3. **Redan kopierade bilder kopieras inte igen.**

## Plattform

- Windows.

## Kameror och målkataloger

- Programmet hanterar **flera kameror** och kommer ihåg en egen
  **grundkatalog per kamera**.
- En kamera identifieras via **EXIF: kameramodell + serienummer** i bilderna
  på kortet.
- Kameran identifieras **en gång per kort**; alla filer på kortet (även de
  utan EXIF, t.ex. video) tillhör den kameran.
- **Ny/okänd kamera:** en guide visas med kamerans namn ur EXIF och användaren
  väljer/skapar en grundkatalog. Kopplingen sparas och kameran känns igen
  automatiskt framöver.

## Mappstruktur i målet

Under kamerans grundkatalog skapas en datumbaserad mappstruktur och där
sparas filerna.

- **Mappstrukturen är konfigurerbar per kamera** och sparas tillsammans med
  kamerans grundkatalog. Mönstret anges med platshållare:

  | Platshållare | Betydelse            |
  |--------------|----------------------|
  | `{ÅÅÅÅ}`     | år, fyra siffror     |
  | `{MM}`       | månad, två siffror   |
  | `{DD}`       | dag, två siffror     |

- **Standardmönster** (används om inget annat anges för kameran):

  ```
  {ÅÅÅÅ}/{MM}/{ÅÅÅÅ}-{MM}-{DD}
  ```

  vilket ger t.ex. `2026/07/2026-07-02/`.

- **Datumkälla:** fotograferingsdatum ur EXIF. Om EXIF saknas (t.ex. video)
  används filens ändringsdatum på kortet (eller videons metadata om den finns).

## Vad kopieras

- **Allt under `DCIM`** på kortet, oavsett filtyp — inget missas.

## Dubbletter och kollisioner

- **Dubblettkoll:** målmappen beräknas ur datumet; om filnamnet redan finns
  där **med samma storlek** räknas filen som redan kopierad och hoppas över.
  Ingen databas/journal används.
- **Kollision** (samma filnamn i målmappen men annan storlek = annan fil):
  filen från kortet kopieras med nytt namn, t.ex. `IMG_0001 (2).JPG`.
  Inget skrivs över.

## Kopiering

- **Ingen verifiering** efter kopiering — Windows filkopiering litas på.

## Arbetsflöde

1. Programmet finns igång i bakgrunden och **bevakar när ett SDXC-kort
   dyker upp** i Windows.
2. När ett kort upptäcks visas en avisering:
   *"Kort från [kamera] upptäckt — starta import?"* — användaren
   **bekräftar med ett klick** innan kopieringen börjar.
3. Under kopieringen visas ett **förloppsfönster** med förloppsindikator,
   antal filer och namnet på filen som just kopieras.
4. Efter importen visas en **kort avisering** med resultatet
   (t.ex. "127 filer kopierade, 43 hoppades över"). Ingen loggfil sparas.

## Felhantering

- **Grundkatalogen onåbar** (extern disk urkopplad, nätverksenhet nere):
  ingenting kopieras; en tydlig avisering förklarar varför. Eftersom kortet
  aldrig rörs kan det bara sättas i igen senare.

## Typ av applikation

**Systemfältsprogram (tray-applikation).** Motiverat av funktionskraven:

- körs i bakgrunden och bevakar enhetsinsläpp,
- visar aviseringar med knappar (bekräfta import, resultat),
- högerklick på ikonen i systemfältet öppnar inställningarna: guiden
  "ny kamera" samt lista över kameror med grundkatalog och mappmönster,
- kan startas automatiskt med Windows.

## Programspråk och verktyg

**C# på .NET** — enbart fria verktyg, ingen kompilator behöver köpas:

- **.NET SDK** (gratis, öppen källkod; kompilatorn ingår, `dotnet build`).
- **Windows Forms** för systemfältsikon och inställningsfönster
  (ingår i .NET, beprövat för tray-appar).
- **MetadataExtractor** (fritt bibliotek, Apache 2.0) för EXIF-läsning.
- **Windows aviseringar** med knappar via det fria paketet
  CommunityToolkit (Microsoft.Toolkit.Uwp.Notifications).
- Utvecklingsmiljö efter smak: VS Code eller Visual Studio Community
  (båda gratis).
- Resultatet blir en vanlig `.exe`.

## Lagring av inställningar

- Inställningarna (kamera → grundkatalog, mappmönster per kamera) sparas
  som en JSON-fil i användarens profil: `%APPDATA%\SDXC-copy\config.json`.
- Lagras alltid på datorn — aldrig på minneskortet.
