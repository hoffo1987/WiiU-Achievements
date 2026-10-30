# Progressi persistenti e riconoscimento dal salvataggio

Correzione del 30 ottobre 2026. Le vecchie build salvavano in `data/` accanto
all’eseguibile: cambiando cartella, cambiava anche l’archivio letto. Questo aveva
nascosto tunica e cannocchiale già registrati nelle build precedenti. Il controllo
«già presente all’avvio» era inoltre una scelta del vecchio motore: non era un
limite che impedisse di riconoscere le condizioni ancora presenti nel gioco.

Ora tutte le versioni normali dell’app usano
`%LOCALAPPDATA%\WiiUAchievements`. Lo stesso archivio contiene `settings.xml`,
`cloud.xml` (token protetto con DPAPI) e `progress/`. Il mutex usa questa cartella,
così due nuove versioni non scrivono contemporaneamente nello stesso archivio.
I test e le anteprime usano cartelle esplicite isolate; `--data-dir=<percorso>` è
un’opzione per prove tecniche, non necessaria per l’uso normale.

Alla prima apertura si importano impostazioni/account solo se assenti, preferendo
la cartella `data/` della copia avviata. In un checkout di sviluppo si cercano
anche `data/` nella radice e nelle build dirette di `build-check/` dotate di EXE.
Per i progressi si usa esclusivamente la stessa identità completa
profilo/titolo/versione/RPX/revisione. Si uniscono soltanto gli ID eseguibili
mancanti: nessuna data già presente viene sovrascritta, nessun sorgente viene
cancellato e nessun backup di credenziali viene importato. Una sorgente illeggibile
o con identità discordante segnala un errore, senza inventare sblocchi.
Le vecchie cartelle rimangono private: non condividerne `data/`.

Gli sblocchi registrati sono visibili anche prima di collegare Cemu. Collegandosi,
il client recupera i dati cloud e monitora il salvataggio con
`IncludeExistingSave`: menu/titolo/caricamenti rimangono esclusi, occorrono 750 ms
di contesto giocabile coerente e poi tre conferme consecutive della condizione.
Le condizioni già soddisfatte vengono registrate, senza richiedere di riottenere
un oggetto o di spendere e riguadagnare rupie. Gli ID già archiviati non notificano
di nuovo e mantengono la propria data. La demo e i test della precedente modalità
`NewEventsOnly` conservano la verifica del passaggio sotto/sopra soglia.

Per uno sblocco trovato in un vecchio archivio si conserva la data originale.
Per una condizione appena riconosciuta nel salvataggio si registra l’ora della
verifica: la notifica dice **Riconosciuto dal salvataggio** e la scheda
**Registrato il**. Non si può ricostruire la data originale di un evento mai
registrato. Se una condizione non lascia più traccia (per esempio rupie già
spese), non si deduce un passato che il gioco non espone.

ID, punti, chiave dei file e revisione `wwhd-alpha-1` sono invariati. Le descrizioni
delle soglie sono aggiornate alla nuova politica. I 53 rilevamenti sperimentali
restano sperimentali; riconoscere il valore caricato non certifica nuovi offset.
Le regole Firebase già pubblicate sono compatibili: gli sblocchi restano documenti
immutabili con il solo timestamp. Non sono necessarie modifiche al backend.
