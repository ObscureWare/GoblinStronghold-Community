# Roadmapa transkit-tool i langpacków

## Przyjęty model

Źródłem jest wersjonowana paczka EN. Core, mod, mapa i tutorial mają własne rodziny paczek. UI gry i UI edytora to **oddzielne domeny**; nazwy zasobów i dane rozgrywki w Core oraz treści map/tutoriali są współdzielone. Synchronizacja podobnych napisów UI może podpowiadać tłumaczenie, ale nie łączy ich tożsamości ani wersji.

Identyfikatory:

- `familyId`: stały GUID rodziny źródła, również we wszystkich tłumaczeniach.
- `packageId`: GUID konkretnego wydania pliku. Każde nowe wydanie EN i każde nowe wydanie tłumaczenia dostaje nowy GUID. Ponowne spakowanie identycznego wydania zachowuje GUID.
- `sourcePackageId`: dokładny GUID wydania EN, powtórzony w tłumaczeniu.
- `translationId`: stały GUID projektu tłumaczenia autora/zespołu. Konkurencyjne tłumaczenia tego samego EN i języka mają różne GUID-y projektu.
- Hash SHA-256: kontrola dokładnej zawartości; GUID nie jest podpisem ani zabezpieczeniem przed celowym podszyciem.

Wersja EN: `major.minor.patch`. Wersja tłumaczenia: `wersja_EN.rewizja`, np. `1.4.0.3`. Nowe źródło resetuje licznik tłumaczenia do 1; szkielet zaczyna od 0. Aktualizacja zachowuje `translationId`, lecz nadaje nowy `packageId`.

Paczka zawiera autora, nazwę i opis projektu. W przyszłym selektorze gry gracz wybierze projekt tłumaczenia, a nie tylko język. Kompletność, przegląd językowy i jakość to osobne informacje; sam procent wpisów nie dowodzi jakości.

## T0 — niezależny kontrakt v1 (zaimplementowany prototyp)

- Archiwum `.langpack` z `langpack.json`; tekstowy JSON obsługiwany bez archiwizacji.
- Domeny `game-ui`, `editor-ui`, `core`, `mod`, `map`, `tutorial`.
- GUID-y i wersje źródła/tłumaczenia oraz hashe pochodzenia.
- Źródłowe metadane `renames`: wersja wprowadzenia, stary i nowy klucz.
- Jawne przenoszenie kluczy, bez zgadywania po podobieństwie tekstu. Historia zmian nazw pozostaje w kolejnych EN, aby obsłużyć przeskoki wersji.
- To nowy kontrakt narzędzia, jeszcze nie format ładowany przez aktualną grę.

## T1 — CLI i transkity (zaimplementowany prototyp)

- `pack`, `init`, `diff`, `apply`, `validate`, `status`, `export`.
- XLSX z edytowalnym tłumaczeniem i znacznikiem przeglądu; JSON do Gita.
- Dodane/zmienione/usunięte/przemianowane klucze, zachowanie istniejących tłumaczeń.
- Zmiana EN lub kontekstu wymaga przeglądu. Zmiana samego klucza może zachować zatwierdzenie.
- Konflikt GUID/hash bazy zatrzymuje `apply`; brak cichego nadpisywania lokalnych poprawek.
- Kontrola parametrów, podstawowego formatowania, kompletności, schematu i powtarzających się kluczy; zapis do nowego pliku.
- Testy migracji, konkurencyjnych tłumaczeń, obiegu XLSX/JSON, manipulacji kolumnami źródłowymi i wywołań CLI.

## T2 — eksport EN i zgodność kontraktów (następne)

Osobne zadania:

1. Dodać jawny eksport EN z gry do `game-ui` oraz osobny z edytora do `editor-ui`. Nie rozdzielać napisów heurystyką w transkit-tool.
2. Eksportować współdzielone Core/mod/map/tutorial z ich stabilnymi identyfikatorami i kontekstem.
3. Wprowadzić utrwalany rejestr rodzin/wydanych GUID-ów oraz nadawanie nowych GUID-ów EN przy zmianie źródła. Nie generować nowych identyfikatorów przy każdym buildzie tego samego wydania.
4. W procesie zmiany nazwy klucza wymagać wpisu `renames` i testu migracji; nie używać ponownie wycofanych kluczy.
5. Udostępnić neutralny, publiczny kontrakt i wspólne wektory testowe dla konsumentów, bez zależności CLI od silnika.
6. Dodać jawne schematy placeholderów, liczby mnogiej, rodzajów gramatycznych i formatowania. Obecna kontrola tokenów nie zastępuje pełnego parsera treści gry.
7. Zweryfikować XLSX po rzeczywistym zapisaniu w Excelu i LibreOffice. Obecne testy sprawdzają OpenXML i symulują edycję komórek.

## T3 — instalowanie i wybór tłumaczenia

- Odkrywanie wielu paczek tej samej rodziny/języka; identyfikacja po GUID-ach, nie nazwie pliku.
- Wybór `translationId` przez użytkownika, osobno dla UI gry/edytora i rodzin zawartości.
- Pokazanie autora, tytułu, wersji EN, rewizji, kompletności i statusu dystrybucji.
- Kontrola dokładnego `sourcePackageId` oraz hasha; jawny komunikat o niezgodności.
- Brakujące/niezatwierdzone wpisy paczek eksperymentalnych korzystają z EN. Niedopasowana paczka nie może po cichu przykrywać nowego źródła.
- Zachowanie preferowanego projektu przy aktualizacji; brak samoczynnego przełączania na konkurencyjnego autora.
- Sprawdzić konflikty wielokrotnie pobranych plików i prób użycia tego samego GUID-u z inną zawartością.

## T4 — praca społeczności i wydania narzędzia

- Źródła JSON i transkity w Community; duże paczki w Releases.
- Początkowo framework-dependent CLI (.NET 10); następnie podpisane archiwa samodzielne dla wspieranych systemów.
- CI walidujące transkity, zgodność źródeł i raport kompletności; żadne makra/formuły XLSX nie są wykonywane.
- Dodać raport konfliktów, świadome rebase i fork projektu tłumaczenia oraz pamięć tłumaczeń jako sugestie do akceptacji.
- Wprowadzić testy zgodności wstecznej przed zmianą numeru formatu.

## T5 — promocja do oficjalnej dystrybucji

Community może publikować różne projekty jako prerelease bez gwarancji. Oficjalny status nadaje proces akceptacji/release, a nie dowolne pole ustawione przez autora w paczce.

Kolejność: walidacja techniczna → przegląd językowy → test w grze/edytorze → akceptacja dokładnego GUID-u i hasha → hotfix lub zwykła aktualizacja. Zachować autorstwo i historię, nie przepakowywać zaakceptowanego wydania ze zmienionymi tekstami pod tym samym GUID-em.
