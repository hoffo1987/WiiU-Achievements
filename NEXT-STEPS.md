# Prossime attività

**1 novembre 2026:** i 40 rilevatori mancanti sono implementati in entrambe le
versioni. Stato corrente: **110 attivi, 93 sperimentali, zero non implementati**.
Leggere `docs/DETECTOR-EXPANSION.md` prima delle prove: include tutti i nuovi
flag, i limiti di Maggie/scambi e la finestra finale di Ganondorf. Il proprietario
ha soltanto la partita corrente. I conteggi 70/53/40 più sotto sono storici.

Integrazione Cemu richiesta il 31 ottobre: completare la build Windows del fork
e le prove live elencate in `docs/CEMU-INTEGRATION.md`. Gli hook riusano motore,
interfaccia e progressi esistenti nel processo Cemu; non certificano nuovi offset.
La build Windows è riuscita; il fork locale utilizzabile è
`build-check/cemu-integrated/Cemu.exe`. Avvio, pannello nello stesso processo,
menu iniziale di Wind Waker, chiusura e riavvio sono stati provati sul PC.
Prossimo controllo: caricare il salvataggio e verificare uno sblocco reale,
poi le transizioni e le notifiche fullscreen. Tenere separata l'installazione
Cemu del Desktop fino alla validazione completa.

Usare l'EXE nella directory principale. L'interfaccia mantiene loghi centrati,
pergamena, vento, onde e cornici semplici senza deformazioni. Le immagini pubbliche
si rigenerano con `tools/render-docs.ps1`, usando soltanto dati sintetici.

1. Prove live di morte/continua, nuova partita/modalità eroe e due slot diversi.
2. Playtesting progressivo dei 93 rilevamenti sperimentali in entrambe le versioni.
3. Verificare prima i nuovi premi disponibili nella partita corrente; poi
   forzieri/switch dopo cambio scena, totale dei frammenti, Maggie/scambi,
   Galleria HD e transizione finale di Ganondorf nella versione esatta.
4. Controllo della notifica sopra le modalità fullscreen di Cemu usate dal giocatore.

La sincronizzazione cloud e i flussi automatici già verificati sono descritti in
`docs/CLOUD.md` e `docs/AUTOMATIC-LOADS.md`. Non cambiare ID o revisione dei progressi.

## Cronologia delle priorità precedenti

Le cartelle e i numeri nelle note seguenti sono riferimenti storici, non il
percorso della build attuale.

# Prossimo ciclo di sviluppo

**Interfaccia aggiornata, 30 ottobre 2026:** usare `WiiUAchievements.exe` nella
directory principale. Il kit completo e i loghi forniti dal proprietario sono
disponibili; la composizione rivista usa pochi decori, loghi centrati senza
deformazioni e stato separato dal banner. Corretto il ridisegno parziale dei
filtri. Non creare ZIP o cartelle
di build aggiuntive salvo richiesta o necessità tecnica. Le priorità di prova
su Cemu elencate sotto restano valide; nessun rilevamento è stato modificato.

La correzione dei progressi del 30 ottobre usa un archivio comune a tutte le
versioni, recupera gli ID dalle vecchie build senza alterarne le date e riconosce
le condizioni già soddisfatte nel salvataggio. La vecchia politica senza sblocchi
retroattivi è superata per il gioco reale. Vedi `docs/PROGRESS-RECOVERY.md`.


Le correzioni del 30 ottobre 2026 ripristinano build e pacchetto, proteggono le
credenziali e completano il client cloud opzionale. Vedi `docs/CLOUD.md`.
Il progetto Firebase esistente è ora utilizzabile: regole personali pubblicate,
34 test sull'emulatore e 9 verifiche del client reale superate. Gli account e i
documenti sintetici sono stati rimossi. I dati pubblici sono precompilati; il
pulsante Account cloud ora è anche disegnato. Build: `build-check/firebase-final/`.
Vedi `docs/CLOUD.md`. L’utente ha confermato **Progressi sincronizzati** nella
propria app. Lo stato connesso ora disabilita Accedi/Registrati; per cambiare
account si usa Esci.
Caricamenti e ricollegamenti automatici sono implementati e provati sui flussi
descritti in `docs/AUTOMATIC-LOADS.md`. Prossime prove live: morte/continua,
due slot diversi, nuova partita/eroe e progressioni sperimentali tardive.



La revisione del 2026-10-29 aggiunge rarità esplicite e correzioni di ordine/testo.
Usare `build-check/rarity/`; la memoria e le condizioni restano quelle della
build Playthrough TEST. Il futuro platino sarà un obiettivo separato di
completamento del set; non assegnarlo a una semplice soglia da 25 punti.

**Build attuale: Playthrough TEST.** Sono implementati 93 rilevamenti
sperimentali, in serie ordinate dalla partenza fino al finale e agli extra: 110
attivi in totale, zero non implementati. **Tappa / F** filtra il percorso e
**Categoria / C** il tipo. L'utente li verificherà durante la partita normale.
La guida operativa è `docs/PLAYTHROUGH.md`. Le note sotto descrivono la build
precedente e la progressione del lavoro.

Il catalogo ora comprende **110 obiettivi**: **17 attivi** (16 soglie più la tunica verde) e **93 obiettivi di storia ed extra da verificare**, con filtri per categoria e stato. ID originali e revisione `wwhd-alpha-1` sono conservati. I 93 traguardi preparati non assegnano punti o sblocchi finché non hanno una rilevazione verificata. La build `build-check/scroll-fix` corregge lo scorrimento e attiva la tunica dopo verifica live prima/dopo, caricamento e menu. L'utente ha confermato la notifica della tunica e lo sblocco è stato verificato su disco. Rimangono da provare riavvio dell'app per questo obiettivo e riavvio Cemu. Dettagli e fonti: `docs/ACHIEVEMENTS.md`.

0. **Alpha 0.3 sul PC con Cemu:** ricompilare con `build.ps1` ed eseguire i test. Poi controllare la demo e la notifica sopra Cemu, anche a schermo intero, il jingle, i tasti e la scala DPI. Controllare anche Giochi, pausa prima del caricamento, ripresa senza sblocchi retroattivi e una nuova soglia raggiunta dopo tre campioni sotto soglia. Le prove cloud non sostituiscono la sessione reale.
1. **Stato della partita:** identificare menu iniziale, caricamento, slot selezionato, modalità normale/eroe e sessione giocabile. Questa è una priorità prima di attribuire obiettivi di storia.
2. **Inventario iniziale:** acquisizione di cannocchiale, spada e scudo. Confrontare campioni prima e dopo ogni acquisizione, quindi verificare che i flag rimangano coerenti nei menu, nei cambi di area e dopo un riavvio.
3. **Progressione:** vela, Wind Waker, strumenti dei dungeon e boss, con condizioni che distinguano l'evento appena avvenuto dal caricamento di dati già completati.
4. **Attivazione del set curato:** implementare progressivamente i 93 traguardi ancora da verificare, mantenendo gli ID. Aggiungere condizioni su bit/eventi e regressioni solo dopo evidenze live; non tradurre maschere in soglie numeriche. Il catalogo copre storia, strumenti, mare, carte, melodie, collezioni e secondarie HD.
5. **Profili ulteriori:** altre regioni/versioni e riavvii di Cemu, con validazione esplicita degli indirizzi e delle firme.

Per le prossime prove bastano il gioco, l'app e i valori/eventi osservati dall'utente. Non servono password, chiavi della console o il trasferimento di ROM al progetto.

Per riprendere senza il contesto della chat, leggi `HANDOFF.md` e le evidenze in
`docs/VALIDATION.md`. Per contributi indipendenti usa `docs/COLLABORATION.md`.
