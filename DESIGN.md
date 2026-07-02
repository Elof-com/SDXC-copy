# SDXC-copy — Designdokument

Program för att kopiera bilder från kamerors SDXC-kort till dator.

Status: **funktionsdesign beslutad** — app-typ, programspråk och implementation
väntar på bekräftelse.

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

Under kamerans grundkatalog skapas:

```
ÅÅÅÅ/MM/ÅÅÅÅ-MM-DD/
```

och där sparas filerna.

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
3. Efter importen visas en **kort avisering** med resultatet
   (t.ex. "127 filer kopierade, 43 hoppades över"). Ingen loggfil sparas.

## Felhantering

- **Grundkatalogen onåbar** (extern disk urkopplad, nätverksenhet nere):
  ingenting kopieras; en tydlig avisering förklarar varför. Eftersom kortet
  aldrig rörs kan det bara sättas i igen senare.

## Konsekvenser för app-typ (ännu ej beslutad)

Funktionsvalen innebär att programmet behöver:

- köras i bakgrunden (bevaka enhetsinsläpp),
- kunna visa aviseringar med knappar (bekräfta import, resultat),
- ha ett litet gränssnitt för guiden "ny kamera" och för att se/ändra
  kopplingen kamera → grundkatalog.

## Ej beslutat

- Typ av applikation.
- Programspråk/teknikstack.
- Var kopplingen kamera → grundkatalog lagras på datorn (detalj som följer
  av teknikvalet).
