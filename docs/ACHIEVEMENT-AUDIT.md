# Revisione completa degli achievement — 31 ottobre 2026

Controllati tutti i **110 ID**, confrontando testo effettivamente caricato, requisito, stato del rilevamento, ordine, categoria, soggetto e cornice dell'icona. Corrette 60 intestazioni e 110 descrizioni; tutte le 110 voci hanno ora testi editoriali espliciti. Questo è un controllo editoriale e statico: non certifica nuovi indirizzi di memoria o acquisizioni nel gioco reale.

## Risultato e criteri

- **53 bronzo, 47 argento, 10 oro**. Nessuna rarità è stata cambiata: le assegnazioni risultano coerenti con i criteri del progetto, non con percentuali globali di sblocco misurate.
- Tutte le **110 cornici corrispondono** alla rarità. Le due sospette nell'anteprima piccola erano già corrette: 20 cuori è oro e saldo 5.000 rupie è argento. Il colore dell'oggetto centrale non determina il rango.
- **70 eseguibili**, inclusi **53 sperimentali**, e **40 non implementati**. La dicitura standard non certifica ogni soglia o ogni scenario di gioco.
- ID, revisione, categorie interne, punti, soglie, condizioni e progressi restano identici. I tre XML delle regole sono protetti da hash nel manifesto dell'artwork.
- Targhette delle schede da 62×15 a **80×22 px logici**, caratteri da 9,3 a **13,64 px**, testo scuro e piena opacità anche sotto le medaglie bloccate. Contrasti nominali: bronzo **8,78:1**, argento **10,35:1**, oro **8,94:1**, platino **9,71:1**. Il controllo riguarda queste coppie di colori, non una certificazione di accessibilità dell'intera applicazione.
- Sei icone migliorate: Canzone del Vento (Zeolo, senza testo incorporato errato), tre soglie della Carta Nautica (Scorfano parlante, senza confonderle con mappe speciali), quattro ampolle tutte visibili e premio corretto per la prima prova dei barili. **52** immagini originarie sono ancora identiche; la sostituzione della Canzone del Vento è registrata con il vecchio hash.
- Le raccolte seguono ora soglie crescenti. I minigiochi iniziali restano nella loro tappa; l'ordine del percorso è indicativo e non impone prerequisiti al motore.
- La ricerca conserva i nomi inglesi come alias dei nomi italiani. Sono stati eliminati collegamenti troppo generici: un oggetto trovato in un dungeon non diventa automaticamente un risultato per boss. Le parole scritte nelle descrizioni restano ricercabili.

## Correzioni di significato

Le rupie indicano il saldo simultaneo, non i guadagni cumulativi. I cuori indicano la salute massima, anche se la barra non è piena. Le capacità di bombe/frecce e portafoglio non richiedono di riempire le scorte. Le ampolle contano indipendentemente dal loro contenuto. I settori della Carta Nautica comprendono quelli già segnati inizialmente.

Per gli oggetti si descrive il possesso rilevato, senza promettere una verifica distinta di acquisto, asta, addestramento o filmato. Gohma e Kardemos richiedono anche la relativa perla; Jay-Harla e Gayla richiedono il relativo stato della Spada Suprema. La zuppa deve essere ancora presente al rilevamento. I bit dei boss possono aggiornarsi all'uscita o al portale: non si promette una notifica sul fotogramma finale.

Le missioni non attive distinguono raccolta, consegna, esito della singola prova e ricompensa. Per Miss Mary servono 20 Portafelicità per la villa e altri 20 per il premio successivo (oltre al dono introduttivo); HD assegna la Mappa 45. Il Dedalo dei Mostri usa i piani numerati 30 e 50, escludendo il piano d'ingresso zero. La Galleria Minitendo comprende Mingle in HD; alcuni soggetti sono fotografabili solo in determinati momenti. Non viene inventato un contatore certificato della Galleria.

## Fonti consultate

- [Manuale italiano Nintendo di Wind Waker HD](https://www.nintendo.com/eu/media/downloads/games_8/emanuals/wii_u_6/thelegendofzelda_windwaker_hd/ElectronicManual_WiiU_TLOZWWHD_IT.pdf) e [pagina italiana Nintendo](https://www.nintendo.com/it-it/Giochi/Giochi-per-Wii-U/The-Legend-of-Zelda-The-Wind-Waker-HD-765386.html): terminologia ufficiale degli strumenti.
- [Traduzioni di Wind Waker](https://wiki.zeldalegends.net/index.php?title=TWW_Translations), confrontate con le citazioni in gioco nelle voci [Canzone del Vento](https://zeldawiki.wiki/wiki/Wind%27s_Requiem), [Canto del Burattino](https://zeldawiki.wiki/wiki/Command_Melody), [Sonata in Dì Notturno](https://zeldawiki.wiki/wiki/Song_of_Passing) e [Melodia del Tifone](https://zeldawiki.wiki/wiki/Ballad_of_Gales). I nomi inglesi restano ricercabili.
- [Miss Mary](https://zeldawiki.wiki/wiki/Mrs._Marie): il premio dopo la villa richiede altri 20, non altri 40. Alcune guide secondarie riportano erroneamente 40 aggiuntivi.
- [Dedalo dei Mostri](https://zeldawiki.wiki/wiki/Savage_Labyrinth), [Galleria](https://zeldawiki.wiki/wiki/Nintendo_Gallery), [Mingle](https://zeldawiki.wiki/wiki/Knuckle), [differenze HD](https://zeldawiki.wiki/wiki/The_Legend_of_Zelda:_The_Wind_Waker_HD): ricompense e requisiti della versione corretta.
- [Tiro ai barili](https://www.zeldadungeon.net/wiki/Barrel_Shoot): prima ricompensa Frammento di Cuore, seconda Mappa 17.
- [Carta Nautica](https://zeldawiki.wiki/wiki/Sea_Chart), [Piattaforma di Volo](https://zeldawiki.wiki/wiki/Flight_Control_Platform), [Statua del Gran Mercante](https://zeldawiki.wiki/wiki/Shop_Guru_Statue): soggetto e definizione dei bersagli.
- [Mappa della memoria](MEMORY-MAP.md), [validazione](VALIDATION.md), `src/WindWakerProgression.cs` e XML delle regole: ciò che il programma può realmente osservare.
- [Provenienza dei PNG e composizioni riproducibili](ARTWORK-SOURCES.md).

## Registro di tutte le 110 voci

La colonna del controllo descrive il dato o il bersaglio. Le voci non attive hanno solo un bersaglio editoriale e nessuna condizione eseguibile. Le immagini sono rappresentative: un esempio di miniatura o una rupia colorata non aggiungono requisiti.

| ID | Titolo revisionato | Rarità | Rilevamento | Controllo / precisazione |
| --- | --- | --- | --- | --- |
| `story-heros-clothes` | Il colore dell'avventura | Bronzo | Standard | Ottieni dalla nonna la tunica dell'eroe. |
| `story-telescope` | Un regalo all'orizzonte | Bronzo | Sperimentale | Ottieni il cannocchiale di Aril. |
| `story-hero-sword` | La prima lama | Bronzo | Sperimentale | Ottieni la Spada dell'Eroe. |
| `story-hero-shield` | Un ricordo di casa | Bronzo | Sperimentale | Ottieni lo Scudo dell'Eroe. |
| `story-spoils-bag` | Bottino da pirata | Bronzo | Sperimentale | Ottieni il Borsello sulla nave dei pirati. |
| `story-fortress-escape` | Un incontro in mare | Bronzo | Sperimentale | Il dato è l'incontro con Re Drakar, non una verifica autonoma dell'intera fuga. |
| `story-sail` | Vento in poppa | Bronzo | Sperimentale | Ottieni la vela normale per navigare con Re Drakar. |
| `gear-picto-box` | Il primo Immaginografo | Bronzo | Sperimentale | Ottieni l'Immaginografo per scattare fotografie. |
| `gear-deluxe-picto` | Ricordi a colori | Argento | Sperimentale | Ottieni l'Immaginografo SP, il potenziamento per le foto a colori. |
| `gear-bait-bag` | Esche a bordo | Bronzo | Sperimentale | Ottieni il Fagotto per conservare le esche. |
| `side-killer-bees` | Trovati tutti! | Bronzo | Non attivo | Trova i 4 Fanatici Quattro a nascondino sull'Isola Taura e ritira il premio. |
| `side-sinking-ships` | Centro e affondato | Bronzo | Non attivo | Vinci la prima partita alla battaglia navale di Arraffaele e ritira il premio. |
| `side-sinking-ships-20` | Una mira da venti colpi | Argento | Non attivo | 20 colpi è incluso: il requisito è 20 o meno. Premio Mappa del Tesoro 23. |
| `story-wind-waker` | La Bacchetta del Vento | Bronzo | Sperimentale | Ottieni la Bacchetta del Vento da Re Drakar. |
| `collect-winds-requiem` | Canzone del Vento | Bronzo | Sperimentale | Nome corretto Canzone del Vento; sostituita l'icona con testo errato con Zeolo. |
| `gear-delivery-bag` | Posta in viaggio | Bronzo | Sperimentale | Ottieni la Cartella per trasportare lettere e oggetti da consegnare. |
| `gear-grappling-hook` | Appigli da pirata | Bronzo | Sperimentale | Ottieni il Rampino nella Caverna del Drago. |
| `story-din-pearl` | Il drago può riposare | Argento | Sperimentale | Sconfiggi Gohma e ottieni la Perla di Din. |
| `side-mail-25` | Postino provetto | Argento | Non attivo | Smista almeno 25 lettere in una singola prova all'ufficio dell'Isola del Drago. |
| `gear-deku-leaf` | Il primo volo | Bronzo | Sperimentale | Ottieni la Foglia Deku dall'Albero Deku. |
| `gear-boomerang` | Andata e ritorno | Bronzo | Sperimentale | Ottieni il Boomerang nel Boschi Proibiti. |
| `story-farore-pearl` | Il bosco ritrova la voce | Argento | Sperimentale | Sconfiggi Kardemos e ottieni la Perla di Farore. |
| `gear-bombs` | Un carico esplosivo | Bronzo | Sperimentale | Ottieni le Bombe e sblocca il loro utilizzo. |
| `story-nayru-pearl` | Una luce nella tempesta | Argento | Sperimentale | Ottieni la Perla di Nayru da Jabun. |
| `side-grandma` | Una zuppa che sa di casa | Bronzo | Sperimentale | Il decoder richiede nonna guarita e una porzione di zuppa ancora nell'ampolla. |
| `story-tower-rises` | Tre perle, una torre | Argento | Non attivo | Colloca le Perle di Din, Farore e Nayru e fai emergere la Torre degli Spiriti. |
| `collect-command-melody` | Canto del Burattino | Bronzo | Sperimentale | Impara il Canto del Burattino nella Torre degli Spiriti. |
| `gear-bow` | Mira da eroe | Bronzo | Sperimentale | Ottieni l'Arco dell'Eroe nella Torre degli Spiriti. |
| `story-gohdan` | Il giudizio di Kuznar | Argento | Sperimentale | Sconfiggi Kuznar, il boss della Torre degli Spiriti. |
| `story-master-sword` | Una lama dal passato | Argento | Sperimentale | Ottieni per la prima volta la Spada Suprema. |
| `collect-ballad-gales` | Melodia del Tifone | Bronzo | Sperimentale | Impara la Melodia del Tifone, che permette di viaggiare con i cicloni. |
| `gear-skull-hammer` | Un colpo deciso | Bronzo | Sperimentale | Ottieni il Martello nella Fortezza dei Demoni. |
| `story-helmaroc` | Mai più in gabbia | Argento | Sperimentale | Sconfiggi Re Elmaroc nella Fortezza dei Demoni. |
| `story-zelda` | L'eredità del regno | Argento | Sperimentale | Scopri la vera identità di Dazel nel castello di Hyrule. |
| `gear-elemental-arrows` | Fuoco e ghiaccio | Bronzo | Sperimentale | Ottieni il potenziamento delle frecce di fuoco e di ghiaccio. |
| `gear-power-bracelets` | La forza sotto la lava | Bronzo | Sperimentale | Ottieni i Superbracciali nella Montagna di Fuoco. |
| `gear-iron-boots` | Piedi ben piantati | Bronzo | Sperimentale | Ottieni gli Calzari di Piombo sull'Isola Gelata. |
| `collect-earth-lyric` | Inno della Terra | Bronzo | Sperimentale | Impara l'Inno della Terra da Laruto. |
| `story-medli-sage` | La saggia della terra | Argento | Non attivo | Risveglia Famirè come saggia del Santuario della Terra. |
| `gear-mirror-shield` | Riflessi d'eroe | Bronzo | Sperimentale | Ottieni lo Scudo Specchio nel Santuario della Terra. |
| `story-jalhalla` | Luce nelle tenebre | Argento | Sperimentale | Richiede sia boss sia primo potenziamento della Spada Suprema. |
| `collect-wind-aria` | Inno del Vento | Bronzo | Sperimentale | Impara l'Inno del Vento da Fado. |
| `story-makar-sage` | Il saggio del vento | Argento | Non attivo | Risveglia Macorè come saggio del Santuario del Vento. |
| `gear-hookshot` | Nessuna distanza | Bronzo | Sperimentale | Ottieni l'Arpione nel Santuario del Vento. |
| `story-molgera` | Il vento torna a cantare | Oro | Sperimentale | Richiede sia boss sia Spada Suprema al pieno potere; oro come tappa conclusiva dei saggi. |
| `story-triforce-first` | Un frammento di coraggio | Bronzo | Sperimentale | Ottieni almeno un frammento della Triforza del Coraggio. |
| `story-triforce-complete` | Otto frammenti di coraggio | Oro | Sperimentale | Ottieni tutti gli 8 frammenti della Triforza del Coraggio. |
| `story-hyrule-barrier` | Oltre l'ultima barriera | Bronzo | Sperimentale | Abbatti la barriera di Hyrule che blocca la strada alla Torre di Ganon. |
| `story-light-arrows` | Una freccia di luce | Bronzo | Sperimentale | Ottieni le Frecce di Luce nella Torre di Ganon. |
| `story-puppet-ganon` | I fili di Triganon | Argento | Non attivo | Sconfiggi tutte e tre le forme di Triganon nella Torre di Ganon. |
| `story-ganondorf` | Un nuovo orizzonte | Oro | Non attivo | Oro finale; il platino resta riservato al futuro completamento del set. |
| `gear-swift-sail` | Il vento mi segue | Argento | Sperimentale | Ottieni la Vela Rapida, il potenziamento esclusivo della versione HD. |
| `gear-magic-armor` | Una protezione preziosa | Argento | Sperimentale | Ottieni l'Corazza Magica. |
| `gear-magic-double` | Magia raddoppiata | Argento | Sperimentale | Raddoppia la capacità massima della barra magica. |
| `gear-bombs-99` | Artificiere del mare | Argento | Sperimentale | Potenzia la borsa fino a una capacità massima di 99 bombe. |
| `gear-arrows-99` | Una faretra senza fondo | Argento | Sperimentale | Potenzia la faretra fino a una capacità massima di 99 frecce. |
| `gear-wallet-max` | Spazio per ogni tesoro | Argento | Sperimentale | Capacità del portafoglio; distinto dal saldo di 5.000 rupie. |
| `explore-big-octo` | Occhi sull'abisso | Bronzo | Non attivo | Sconfiggi almeno un Octoboss nel Grande Mare. |
| `explore-all-octos` | Un mare senza Octoboss | Argento | Non attivo | Sconfiggi tutti i 6 Octoboss del Grande Mare. |
| `explore-chart-10` | Dieci settori di mare | Bronzo | Sperimentale | Settori segnati ≥ 10 su 49, compresi quelli iniziali; icona Scorfano corretta. |
| `explore-chart-25` | Metà del Grande Mare | Argento | Sperimentale | Settori segnati ≥ 25 su 49, compresi quelli iniziali; icona Scorfano corretta. |
| `explore-chart-49` | Il mare non ha segreti | Oro | Sperimentale | Settori segnati ≥ 49 su 49, compresi quelli iniziali; icona Scorfano corretta. |
| `explore-salvage-first` | Il primo tesoro sommerso | Bronzo | Non attivo | Recupera un forziere indicato da una Mappa del Tesoro normale. |
| `explore-salvage-10` | Dieci tesori sommersi | Argento | Non attivo | Recupera 10 forzieri diversi indicati dalle Mappe del Tesoro normali. |
| `explore-salvage-46` | Tutti i tesori sommersi | Oro | Non attivo | Recupera i forzieri di tutte le 46 Mappe del Tesoro della versione HD. |
| `explore-ghost-ship` | A bordo del mistero | Argento | Non attivo | Sconfiggi i nemici della Nave Fantasma e apri il forziere del suo tesoro. |
| `explore-savage-30` | Il tesoro del piano 30 | Argento | Non attivo | In HD il forziere contiene un frammento della Triforza, non una mappa. |
| `explore-savage-50` | Il fondo del labirinto | Oro | Non attivo | In HD il forziere finale contiene l'Amuleto dell'Eroe, non il Frammento di Cuore GameCube. |
| `collect-heart-piece-first` | Un quarto di speranza | Bronzo | Non attivo | Ottieni almeno un Frammento di Cuore; i Portacuori interi non contano. |
| `collect-heart-pieces-12` | Dodici frammenti di cuore | Argento | Non attivo | Ottieni almeno 12 dei 44 Frammenti di Cuore, inclusi quelli già convertiti. |
| `collect-heart-pieces-44` | Ogni frammento al suo posto | Oro | Non attivo | Ottieni tutti i 44 Frammenti di Cuore, senza contare i Portacuori dei boss. |
| `collect-treasure-charts-10` | Dieci Mappe del Tesoro | Bronzo | Non attivo | Ottieni 10 Mappe del Tesoro diverse, escluse le carte speciali e della Triforza. |
| `collect-treasure-charts-25` | Venticinque mappe | Argento | Non attivo | Ottieni 25 delle 46 Mappe del Tesoro normali della versione HD. |
| `collect-treasure-charts-46` | Tutte le Mappe del Tesoro | Oro | Non attivo | Ottieni tutte le 46 Mappe del Tesoro normali presenti nella versione HD. |
| `collect-triforce-charts` | Tre mappe da decifrare | Argento | Non attivo | Tre mappe decifrate in HD, distinte dalle otto parti della Triforza. |
| `collect-tingle-statues-5` | Le cinque statue di Tingle | Argento | Non attivo | Apri i 5 forzieri delle statue di Tingle nascosti nei dungeon. |
| `collect-gallery-first` | La prima miniatura | Bronzo | Non attivo | La miniatura nell'icona è un esempio, non un soggetto obbligatorio. |
| `collect-gallery-complete` | Un mare in miniatura | Oro | Non attivo | In HD include Mingle; non richiede Miiverse. Occorre ancora definire/verificare il rilevatore del completamento. |
| `collect-bottles-4` | Quattro ampolle | Argento | Sperimentale | Contano 4 ampolle ottenute, anche piene; icona ridisposta per mostrarle tutte. |
| `collect-song-passing` | Dal giorno alla notte | Bronzo | Sperimentale | Impara da Gill la Sonata in Dì Notturno. |
| `collect-songs-6` | Le sei melodie | Argento | Sperimentale | Impara tutte le 6 melodie della Bacchetta del Vento. |
| `side-trading` | L'ultimo scambio | Argento | Non attivo | Scambia la Statua del Gran Mercante all'Isola del Pesce e ritira il Frammento di Cuore. |
| `side-spectacle` | Artiglieria di precisione | Bronzo | Non attivo | Prima vittoria ai barili: Frammento di Cuore; la Mappa 17 è il secondo premio. Icona corretta. |
| `side-flight-control` | Oltre il traguardo | Argento | Non attivo | Supera il traguardo della Piattaforma di Volo con la Foglia Deku e ritira il premio. |
| `side-lighthouse` | Un faro nella notte | Bronzo | Non attivo | Riaccendi il faro di Taura e ritira il Frammento di Cuore dal custode Galbino. |
| `side-maggie` | Una lettera per Maggy | Bronzo | Non attivo | Consegna a Maggy la risposta del suo innamorato e ritira il premio. |
| `side-joy-pendants-40` | La carta della maestra | Argento | Non attivo | 20 per la villa e altri 20 per il premio successivo; il dono introduttivo è separato. |
| `side-forest-water` | Otto alberelli in fiore | Argento | Non attivo | Rianima tutti gli 8 alberelli con acqua della foresta prima che scada e ritira il premio. |
| `side-cabana` | Una villa tutta mia | Argento | Non attivo | Dopo il primo dono, consegna 20 Portafelicità a Miss Mary e ottieni l'Invito alla Villa. |
| `side-hurricane-spin` | La tecnica dell'uragano | Argento | Non attivo | Consegna 10 Emblemi del Guerriero a Sensei Ken e impara l'Attacco Uragano. |
| `side-orca-500` | Cinquecento colpi | Argento | Non attivo | Totalizza almeno 500 colpi nella sfida di Sensei Ken e ritira il Frammento di Cuore. |
| `side-town-decoration` | Una città in festa | Argento | Non attivo | Contano i 14 piedistalli esterni, non quelli nei negozi e negli edifici. |
| `side-mila` | Una seconda possibilità | Bronzo | Non attivo | Concludi la vicenda notturna di Mina e ricevi la sua ampolla. |
| `side-golden-feathers` | Piume per una promessa | Bronzo | Non attivo | Consegna 20 Piume Dorate a Fagalore e ritira la ricompensa spedita per posta. |
| `hearts-4` | 4 cuori di coraggio | Bronzo | Standard | Salute massima ≥ 4 cuori; non salute corrente. |
| `hearts-5` | 5 cuori di coraggio | Bronzo | Standard | Salute massima ≥ 5 cuori; non salute corrente. |
| `hearts-6` | 6 cuori di coraggio | Bronzo | Standard | Salute massima ≥ 6 cuori; non salute corrente. |
| `hearts-8` | 8 cuori di coraggio | Bronzo | Standard | Salute massima ≥ 8 cuori; non salute corrente. |
| `hearts-10` | 10 cuori di coraggio | Argento | Standard | Salute massima ≥ 10 cuori; non salute corrente. |
| `hearts-12` | 12 cuori di coraggio | Argento | Standard | Salute massima ≥ 12 cuori; non salute corrente. |
| `hearts-15` | 15 cuori di coraggio | Argento | Standard | Salute massima ≥ 15 cuori; non salute corrente. |
| `hearts-18` | 18 cuori di coraggio | Argento | Standard | Salute massima ≥ 18 cuori; non salute corrente. |
| `hearts-20` | 20 cuori di coraggio | Oro | Standard | Cornice correttamente oro; numero 20 riferito alla salute massima. |
| `wallet-10` | Le prime 10 rupie | Bronzo | Standard | Saldo simultaneo ≥ 10; non guadagni totali. |
| `wallet-100` | 100 rupie in tasca | Bronzo | Standard | Saldo simultaneo ≥ 100; non guadagni totali. |
| `wallet-200` | 200 rupie in tasca | Bronzo | Standard | Saldo simultaneo ≥ 200; non guadagni totali. |
| `wallet-500` | 500 rupie in tasca | Bronzo | Standard | Saldo simultaneo ≥ 500; non guadagni totali. |
| `wallet-1000` | 1.000 rupie in tasca | Argento | Standard | Saldo simultaneo ≥ 1000; non guadagni totali. |
| `wallet-2500` | 2.500 rupie in tasca | Argento | Standard | Saldo simultaneo ≥ 2500; non guadagni totali. |
| `wallet-5000` | 5.000 rupie in tasca | Argento | Standard | Il soggetto è una rupia dorata; la cornice è correttamente argento. Il colore della rupia non è la rarità. |

## Ripetere il controllo

```powershell
python -I tools/audit-achievement-catalog.py
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\run-ui.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\run.ps1
```

Il controllo statico confronta la cornice su 15.367 pixel opachi dell'anello esterno, escludendo soggetto, cielo, barca e contatori; non determina semanticamente quale personaggio sia raffigurato. Il soggetto è stato esaminato visivamente. Il JSON tecnico è in `test-results/catalog-audit.json`, escluso da Git.

Esiti di questa revisione: **147 controlli UI** e **322 core/Win32** superati con dati sintetici; nessuna nuova certificazione Cemu.
