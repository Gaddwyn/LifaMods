[README.md](https://github.com/user-attachments/files/34079266/README.md)
# LifaMods : coop FF9 (mod Memoria), version de test Duel (Tetra Master, combat, échange, chasse au trésor clans et classement) : prototype v0

Ce que fait cette version :
- tous les joueurs sont **clients** d'un serveur dédié `CoopServer` (TCP, port 7777 par défaut) ;
- chacun voit les autres joueurs **sur le terrain** (field) quand ils sont dans la **même zone**, et **sur la carte du monde** quand ils y sont aussi : position, orientation, animation, pseudo au-dessus de la tête (le tien aussi au-dessus de ton personnage, `ShowOwnName = 0` pour le cacher). Pendant les scènes de l'histoire et les vidéos, les autres joueurs sont cachés ; quand tu parles simplement à un PNJ ou lis un panneau, ils restent visibles (`HideOthersInScenes = 0` pour ne jamais les cacher) ;
- le pseudo a un **cadre qui change tous les 10 niveaux** (niveau le plus haut de l'équipe) : bronze (10), argent (20), or (30), émeraude (40, avec une gemme de chaque côté), saphir (50), améthyste (60), rubis (70, avec une lueur qui pulse), mithril (80) et arc-en-ciel (90). Le niveau s'affiche à droite du cadre (« Lv.45 ») et dans la liste F8 ;
- quand un joueur change de zone, il disparaît de l'ancienne et apparaît dans la nouvelle chez ceux qui y sont ;
- **chacun sauvegarde sa propre partie** (hôte et clients), dans un fichier de sauvegarde **à son pseudo** (sous-dossier `Coop-pseudo` du dossier de sauvegardes du jeu) : deux jeux sur le même PC ne se mélangent plus leurs sauvegardes. La première fois, ce dossier part d'une copie des sauvegardes existantes. `SeparateSaves = 0` dans `Coop.ini` garde le fichier habituel du jeu. Pour réserver la sauvegarde à l'hôte, un client peut ajouter `AllowClientSave = 0` dans son `Coop.ini` ;
- trois **icônes en haut à droite** (terrain et carte du monde) : le **classement des duels**, le **clan** et le **menu principal** du jeu, avec leur touche écrite au-dessus (Pavé 9, F9, et la touche Menu de ta configuration, B chez certains). Un clic fait la même chose que la touche. `ShowHudButtons = 0` dans `Coop.ini` les cache ;
- les fenêtres du mod (clan, banque, échange, classement, raid, emotes) reprennent le **style des menus du jeu** : sa police, le cadre de ses fenêtres (gris ou bleu selon la couleur choisie dans les options du jeu), la barre de surbrillance et la main sous la souris. `GameStyle = 0` dans `Coop.ini` remet l'ancien style ;
- **F8** ouvre/ferme un panneau en haut à droite : qui est connecté, et où (nom de la zone, « en combat », « carte du monde »), avec « (avec toi) » pour ceux dans ta zone ;
- **T** ouvre un **tchat** en bas à gauche : on tape, **Entrée** envoie, **Échap** annule. Les messages s'effacent après quelques secondes. Le message apparaît aussi quelques secondes dans une bulle au-dessus du personnage de celui qui l'a écrit, chez les joueurs qui le voient. Pour **chuchoter** à un seul joueur : `/w pseudo message` (`/r message` répond au dernier qui t'a chuchoté). Il entend le « kupo » du Mog (`WhisperSound` dans `Coop.ini`), et le message s'affiche en rose chez vous deux seulement. **Emotes** : pendant que tu écris, **Tab** (ou le bouton Mog à droite de la ligne) ouvre une grille d'icônes du jeu (bulles « ! » et « ? », Mog, visages des personnages, icônes d'altérations...). Un clic ajoute son code (ex. `:mog:`, `:!:`, `:zidane:`, `:dodo:`) au message, et le code peut aussi se taper à la main. Les icônes s'affichent dans le tchat et dans la bulle ; une emote envoyée seule s'affiche en grand au-dessus de la tête. Les arrivées et départs de joueurs s'y affichent aussi ;
- rien n'est synchronisé en combat (les avatars sont masqués). Un joueur qui est dans une autre zone n'est pas affiché (c'est une autre scène), mais la liste F8 indique où il est.

Ce qu'elle ne fait pas encore (étapes suivantes de [CONCEPTION.md](../coop-design/CONCEPTION.md)) : l'envoi de la sauvegarde de l'hôte aux clients, le suivi automatique de la zone de l'hôte, le blocage des interactions des clients (PNJ, coffres, sorties). Pour l'instant **chaque joueur charge sa propre sauvegarde**, idéalement une copie de celle de l'hôte.

## Tetra Master entre joueurs (version de test)

1. Mettez-vous dans la même zone, chacun avec la main libre (pas de dialogue, pas de menu).
2. Dans une zone ou **sur la carte du monde** (à pied, pas à dos de chocobo ni en vaisseau), approche-toi du joueur à défier et appuie sur **F7** : son nom passe en jaune (« défier ? »). Appuie encore sur **F7** pour envoyer le défi. S'il y a plusieurs joueurs, c'est le plus proche de toi qui est choisi : colle-toi à celui que tu veux. La distance se règle avec `DuelRange` dans `Coop.ini`.
3. L'autre joueur voit ton nom en jaune (« te défie ! ») et a 30 s pour appuyer sur **F7** à son tour.
4. Les deux jeux ouvrent Tetra Master. Chacun choisit 5 cartes de **sa propre collection**, puis la partie se joue chacun son tour ; l'adversaire affiché est l'autre joueur.
5. À la fin, le gagnant choisit une carte du perdant (toutes en cas de victoire parfaite, comme dans le jeu). Pas de revanche : les deux reviennent dans la zone.

Règles et limites :
- les deux joueurs doivent avoir **la même version de LifaMods et de Memoria**, et les mêmes réglages `[TetraMaster]` dans `Memoria.ini` (`TripleTriad`, `ReduceRandom`) ; `EasyWin` doit être à 0 ;
- le hasard (blocs, pièce, combats de cartes) est tiré à l'identique des deux côtés. Les deux parties comparent le plateau à chaque tour : si elles divergent, la partie est annulée et personne ne perd de carte ;
- la carte gagnée ou perdue n'est gardée que si tu sauvegardes ensuite. Un client avec `AllowClientSave = 0` ne garde donc pas le résultat ;
- si l'autre joueur se déconnecte ou quitte, la partie est annulée sans perte.

La touche se change dans `Coop.ini` (`DuelKey = F7`).

## Duel en combat entre joueurs (version de test)

Ton équipe contre celle d'un autre joueur, dans un vrai combat de FF9. Celui qui perd revient simplement dans la zone : **pas de game over**.

1. Mettez-vous dans la même zone, chacun avec la main libre (pas de dialogue, pas de menu).
2. Dans une zone ou **sur la carte du monde** (à pied), approche-toi du joueur et appuie sur **F10** : son nom passe en jaune (« combat ? »). Appuie encore sur **F10** pour envoyer le défi.
3. L'autre joueur a 30 s pour appuyer sur **F10** à son tour.
4. Les deux jeux lancent un combat. En face de toi : les personnages de l'autre joueur (leur modèle, leur arme, leurs stats). Tu joues ton équipe normalement ; quand l'autre joueur agit, son personnage fait l'action chez toi aussi.
5. Le combat se termine comme d'habitude : le dernier camp debout gagne. Le perdant n'a pas de game over, et les deux reviennent dans la zone.

Règles et limites :
- il faut `SFXRework = 1` dans la section `[Battle]` de `Memoria.ini` (c'est la valeur par défaut), chez les deux joueurs, et la même version de LifaMods ;
- le duel commence **PV et PM au maximum**, sans altérations ; après le duel, chacun retrouve exactement ses PV, PM et altérations d'avant. Pas d'expérience, de gils, d'AP ni d'objets à gagner. Les objets utilisés pendant le duel sont consommés ;
- **un personnage K.O. est éliminé** : pas de Queue de Phénix, Vie, Auto-Vie ni Flamme Phénix pendant un duel ;
- pas de fuite possible, et les triches de Memoria sont coupées le temps du duel ;
- chaque jeu calcule les dégâts reçus par **ses propres** personnages, puis envoie leurs PV, PM et altérations à l'autre. Les deux jeux tirent le même hasard pour chaque action, donc les chiffres affichés sont en général identiques des deux côtés. Ils peuvent encore différer quand une capacité de soutien ou un équipement de la cible change le calcul (absorption d'un élément, par exemple) : les vrais PV sont toujours ceux calculés chez le propriétaire du personnage ;
- les personnages adverses n'ont pas leurs capacités de soutien (Tueur, Attaque PM, Contre...) quand ils agissent chez toi, ni le bonus de leur Transe. Freya ne saute pas chez l'adversaire : il voit directement l'attaque Lancer. Voler, Manger et Cuisiner n'ont pas d'effet sur un joueur. Gant Limite (Quina) échoue toujours contre un joueur, et les attaques qui dépendent de la partie (Lancer des pièces, Grenouille, Dragon Crest...) se calculent avec les gils et les compteurs de celui qui les reçoit ;
- l'arène est la scène de combat `BattleDuelScene` du `Coop.ini` de celui qui défie (par défaut `EF_R002`, une scène de la Forêt Maudite). Les combats de boss où le jeu empêche l'ennemi de mourir sont refusés (retour à `EF_R002`) ;
- les dégâts des duels sont les dégâts normaux du jeu ;
- la musique du duel est celle de la Fête de la chasse (`BattleDuelMusic = music082` dans `Coop.ini`, chacun entend celle de son propre réglage ; laisser vide pour la musique de combat habituelle) ;
- si l'autre joueur se déconnecte ou ne répond plus pendant 45 s, tu gagnes par abandon ;
- c'est une version de test : elle n'a pas encore été essayée en vrai, des personnages ou des capacités peuvent mal s'afficher. Le journal `Memoria.log` (lignes `[Coop]`) aide à corriger.

La touche se change dans `Coop.ini` (`BattleDuelKey = F10`). F10 n'est utilisée ni par le jeu ni par les triches de Memoria.

## Échange d'objets, de cartes et de gils (version de test)

1. Dans la même zone, approche-toi du joueur et appuie sur **F11** : son nom passe en jaune (« échanger ? »). Appuie encore sur **F11** pour lui proposer l'échange.
2. L'autre joueur a 30 s pour appuyer sur **F11** à son tour. Une fenêtre s'ouvre chez les deux (elle se manie à la souris).
3. À gauche, ton inventaire, avec deux onglets : **Objets** (**+1** / **+10** ajoutent un objet à ton offre) et **Cartes** (**+** ajoute une carte Tetra Master). Chaque carte est affichée avec son nom, ses stats (par exemple 0P23) et son nombre de flèches. Au milieu, ton offre (**-1** / **X** pour retirer) et les gils que tu donnes. À droite, l'offre de l'autre joueur, mise à jour en direct.
4. Clique sur **Valider**. L'échange se fait quand les deux ont validé. Si l'un des deux change son offre, les validations sont annulées : il faut revalider.
5. **Annuler** ou **Échap** ferme la fenêtre sans rien échanger.

Règles et limites :
- seuls les objets de l'inventaire s'échangent (pas les objets équipés, ni les objets rares / trésors) ;
- chaque carte Tetra Master est unique : l'autre joueur reçoit exactement la carte choisie (mêmes flèches, même type, mêmes stats). La limite de cartes du jeu (100 par défaut) est vérifiée avant l'échange ;
- avant d'échanger, chaque jeu vérifie que son joueur a toujours ce qu'il donne et a la place de recevoir (99 exemplaires par objet, 9 999 999 gils) ; sinon l'échange est refusé et la raison s'affiche ;
- pendant que la fenêtre est ouverte, ton personnage ne bouge pas et le tchat est fermé ;
- dès que l'échange est fait, le jeu de chacun fait une **sauvegarde automatique** (celle que charge « Reprendre ») : quitter sans sauvegarder ne peut plus annuler l'échange d'un seul côté. Un client avec `AllowClientSave = 0` ne sauvegarde pas et ne garde donc pas l'échange ;
- si l'autre joueur se déconnecte avant que l'échange soit fait, rien n'est échangé.

La touche se change dans `Coop.ini` (`TradeKey = F11`).

## Chasse au trésor à dos de Choco (version de test)

De temps en temps (toutes les 20 à 40 min par défaut), un trésor est enfoui n'importe où sur la **carte du monde**, le même pour tous les joueurs. Tout le monde le voit dans le tchat, avec le terrain où il se trouve :
- **sur la terre ferme** : n'importe quel Choco ;
- **sur des hauts-fonds ou des récifs** : il faut le Choco bleu clair ;
- **dans les montagnes** : il faut le Choco rouge ;
- **en pleine mer** : il faut le Choco bleu foncé.

Le Choco doré va partout où va le bleu foncé. Un trésor ne demande jamais un Choco que personne dans la partie n'a encore monté.

1. Monte sur ton Choco (à terre, pas en vol) : un indice s'affiche en haut à gauche, **froid**, **tiède**, **chaud**, **brûlant**, puis **ici !** quand tu es dessus.
2. Appuie sur **F9** pour que ton Choco creuse.
3. Le premier qui creuse au bon endroit gagne : des gils et un objet adaptés au niveau de son équipe (potions au début, Élixirs, gemmes ou Matière noire plus tard), et plus de gils quand le trésor demandait un Choco plus évolué. Tout le monde voit qui l'a trouvé, puis le trésor disparaît pour tous.
   - Bonus très rare : sur le CD 4, chaque trésor trouvé a 1 % de chances de contenir aussi **Excalibur II**, une seule fois par joueur (rien si tu l'as déjà, dans le sac ou équipée).
4. Si personne ne le trouve en 60 min, il disparaît et un autre sera enfoui plus tard.

Comment c'est décidé :
- c'est le jeu de l'hôte qui choisit quand (sur un serveur dédié, c'est le joueur connecté depuis le plus longtemps, et le rôle passe au suivant s'il part). Les réglages `TreasureMinMinutes`, `TreasureMaxMinutes` et `TreasureLifetimeMinutes` de son `Coop.ini` comptent ;
- l'emplacement est tiré au hasard sur toute la carte, en lisant le terrain du jeu. Pour lire le terrain, il faut que la carte du monde soit chargée : si l'hôte n'y est pas, c'est un joueur présent sur la carte du monde qui tire l'emplacement ;
- limite : le jeu ne dit pas quels endroits ne sont accessibles qu'en volant (Choco doré), donc un trésor « terre ferme » peut tomber sur un plateau isolé. Dans ce cas, il disparaîtra au bout de 60 min ;
- comme pour les échanges, un client ne garde le trésor que s'il peut sauvegarder (ce qui est le cas par défaut) ;
- `TreasureHunt = 0` dans `Coop.ini` coupe la chasse pour toi ; la touche se change avec `TreasureKey`.

## Clans (version de test)

- **F9** (sur le terrain ou la carte du monde, hors menu) ouvre la fenêtre du clan. À dos de Choco sur la carte du monde, F9 sert toujours à creuser pour le trésor (`ClanKey` dans `Coop.ini` pour choisir une autre touche).
- **Sans clan** : on choisit un nom (2 à 20 caractères), un tag (1 à 4 lettres, ex. MOG) et une couleur, puis **Créer le clan**. Un clan compte au plus **20 membres** (`ClanMaxMembers` dans le `Coop.ini` du chef, de 2 à 64). Les invitations reçues s'affichent à droite avec **Accepter** / **Refuser** (elles expirent après 2 minutes).
- **Dans un clan** : la liste des membres (★ = chef, connecté ou hors ligne, niveau). Le chef invite les joueurs connectés sans clan, nomme un autre **Chef**, **Exclut** un membre ou **Dissout** le clan (double clic pour confirmer). Un membre peut **Quitter** le clan ; le chef doit d'abord nommer un autre chef s'il reste des membres.
- Le tag `[TAG]` s'affiche dans la couleur du clan à gauche du cadre du pseudo, au-dessus de la tête des joueurs et dans la liste F8.
- `/c message` dans le tchat parle seulement aux membres du clan connectés (texte vert).
- **Niveau du clan** (1 à 10), affiché dans la fenêtre du clan avec une barre d'expérience : chaque combat **gagné** contre des monstres et chaque trésor trouvé par un membre donne 1 XP au clan (les fuites, les défaites et les duels entre joueurs ne comptent pas). Il faut 50 XP pour passer du niveau 1 au 2, puis 100, 200, 400... (le double à chaque niveau, 25 550 XP au total pour le niveau 10). Le passage de niveau s'annonce dans le tchat. L'XP est gardée avec la banque (serveur dédié 1.3 ou hôte).
- **Bienfaits** (bouton dans la fenêtre du clan, à partir du niveau 3 du clan) : le chef paie 10 000 gils avec la banque du clan pour 24 h de bonus sur les combats gagnés contre des monstres, au choix **+3 % d'EXP** (« Savoir partagé ») ou **+4 % de gils** (« Bourse du clan »). Un seul bienfait à la fois ; il profite à tous les membres du clan et à eux seuls. Le bienfait actif et son temps restant s'affichent dans la fenêtre du clan. Nécessite CoopServer 1.4 (ou un hôte à jour).
- **Banque du clan** (bouton dans la fenêtre du clan) : à gauche ton inventaire, à droite le coffre du clan. Un clic sur « 1 », « 10 » ou « Tout » dépose l'objet, et « 1 », « 10 » ou « Max » le retire (seulement ce qui tient dans ton inventaire, 99 au maximum). Les gils se déposent et se retirent avec le champ « Montant ». Tous les membres peuvent déposer et retirer ; les 10 derniers mouvements s'affichent en bas avec le nom du joueur. La banque est gardée par le **serveur dédié** (fichier `Coop-ClanBank.txt` à côté de `CoopServer`) ou, sans serveur, par le jeu de l'**hôte** (`Coop-ClanBank.txt` dans son dossier du jeu) : elle n'est donc accessible que connecté, et c'est la même pour tout le clan tant que vous jouez sur le même serveur ou avec le même hôte. Le serveur dédié doit être mis à jour (CoopServer 1.2). **Anti-duplication** : après chaque dépôt ou retrait, le jeu fait une **sauvegarde automatique** (celle que charge « Reprendre »), et c'est elle qui rend l'opération définitive. Elle se fait dès que tu marches librement (pas pendant un menu ou une scène) ; sur le terrain du clan, où rien n'est sauvegardé, elle se fait dès que tu le quittes. Si `DisableAutoSave = 1` dans `Memoria.ini`, le tchat te demande de sauvegarder toi-même. Si le joueur recharge une partie (Charger, Reprendre) ou relance le jeu sans avoir sauvegardé, l'opération est aussi annulée dans la banque, comme dans son inventaire. Un dépôt en attente n'est pas encore utilisable par les autres (la fenêtre affiche « Dépôts en attente »). Après un dépôt, un retrait ou un échange, reprends donc ta partie avec « Reprendre » (ou sauvegarde de nouveau) : charger une sauvegarde manuelle faite avant est une partie plus ancienne. **Sauvegardes plus anciennes** : à chaque sauvegarde et chaque chargement, le jeu envoie son temps de jeu au serveur (ou à l'hôte). Un joueur qui charge une partie plus ancienne que sa dernière sauvegarde (ou commence une nouvelle partie) est **bloqué pour la banque, les échanges et le raid** : un message le prévient dans le tchat, et il est débloqué dès qu'il recharge sa dernière sauvegarde, ou **tout seul après 1 h de jeu** sur la partie bloquée (1 h de temps de jeu et 1 h de temps réel, sans recharger entre-temps une partie plus ancienne ; le déblocage se fait à la sauvegarde suivante, et cette partie devient alors sa nouvelle référence). Le reste du jeu marche normalement. En cas de vraie nouvelle partie ou de sauvegarde perdue, l'admin le débloque : commande `unblock <pseudo>` dans la console du serveur dédié, ou `/debloquer <pseudo>` dans le tchat de l'hôte. Nécessite CoopServer 1.7 (1.16 pour le déblocage automatique).
- **Raid de boss** (bouton dans la fenêtre du clan) : le chef choisit un personnage de son équipe et ouvre les inscriptions ; les membres connectés sont prévenus dans le tchat et s'inscrivent depuis le même écran avec un personnage de leur équipe (chaque personnage une seule fois, 4 joueurs au plus, chef compris). **Lancer le raid** (double clic) vérifie que tout le monde est disponible sur une zone, puis prend **500 000 gils dans la banque du clan** et lance le combat chez tous. Chaque joueur contrôle son personnage avec sa propre barre ATB ; les personnages des autres sont des copies qui rejouent leurs actions. Le boss est tiré au hasard dans `RaidBosses` (par défaut les quatre Chaos : 932 Maliris, 933 Tiamat, 934 Kraken, 935 Liche) et agit avec son IA normale, calculée par le jeu du chef. Ses PV et ses dégâts s'adaptent au plus haut niveau du groupe (`RaidBossHpPercent` pour régler les PV). En cas de victoire, chacun reçoit des récompenses selon le niveau de son personnage : EXP, gils (2 000 × niveau), PA et objets (Potions X/Éthers, puis Élixirs, Plumes de phénix et Matière noire à haut niveau) ; comme toujours, un personnage K.O. ne gagne pas d'EXP. Pas de game over en cas de défaite, et les PV/PM d'avant le raid sont remis. Les gils sont pris au lancement et ne sont pas rendus si le combat est interrompu. Nécessite `SFXRework = 1` et CoopServer 1.5 ou plus (ou un hôte à jour). Non testé en jeu.
- Chaque joueur garde son clan dans `Coop-Clan.txt` (dossier du jeu, à côté de `Coop.ini`). Les changements (nouveau membre, exclusion, nouveau chef, dissolution) se transmettent dès que les membres se croisent en ligne, même s'ils étaient absents sur le moment. Les membres sont reconnus par leur pseudo : changer de `PlayerName` fait perdre sa place dans le clan.
- **Terrain de clan** (bouton « Terrain » dans la fenêtre du clan, ou `/terrain` dans le tchat) : le chef d'un clan **niveau 6 ou plus** achète une fois pour toutes un terrain pour le clan avec **1 000 000 de gils de la banque du clan**. Pour ce premier essai, c'est **Dali (le village)**. Les membres s'y téléportent depuis une zone ou la carte du monde. C'est une copie de Dali sans personnage ni événement : les portes mènent aux intérieurs (auberge, maison du maire, boutique, bar, moulin), eux aussi vides et privés ; les sorties du village ramènent au village. Les membres qui y sont se voient entre eux, mais pas les joueurs qui sont dans le vrai Dali. « Quitter le terrain » (ou `/terrain` à nouveau) ramène exactement à l'endroit d'où on est parti. Rien n'est sauvegardé tant qu'on est sur le terrain. Pour charger Dali sans cinématique, le jeu lui fait croire à une progression de fin de jeu (`ClanTerrainScenario`, 11100 par défaut), puis remet la vraie progression au retour. `ClanTerrainEntrance` choisit le point d'arrivée (-1 = celui du jeu). Nécessite CoopServer 1.12 (ou un hôte à jour).
- **Marchands du terrain** (fenêtre du clan, « Terrain ») : le chef engage une fois pour toutes trois marchands, **200 000 gils chacun pris dans la banque du clan** : un **marchand d'objets** à placer dans le **bar** de Dali, un marchand d'**armes et armures** et un **synthétiseur** à placer dans la **boutique** de Dali. Le chef se tient à l'endroit voulu (sur le terrain du clan) et clique « Engager ici » : le marchand apparaît là, tourné comme lui ; « Déplacer ici » le change de place gratuitement. Ensuite chaque membre, à côté d'un marchand, appuie sur **Confirmer** pour ouvrir la boutique du jeu et paie avec ses propres gils. Ce qui est vendu suit l'avancée de l'histoire de chacun : le marchand du bar vend seulement des objets (potions, éthers, tentes...), comme les épiceries du jeu, de plus en plus fournies ; les armes et armures et les synthèses suivent les boutiques de Lindblum au CD 1, puis Treno et Daguerreo, jusqu'à celles de la fin du jeu. Nécessite CoopServer 1.15 (ou un hôte à jour).
- **Langue** : le mod suit la langue du jeu (jeu en français = textes en français, toute autre langue = anglais). Les messages du serveur arrivent dans les deux langues et chaque jeu affiche la sienne. Nécessite CoopServer 1.21 pour les messages du serveur en anglais.
- **Couturier** (4e marchand du terrain, **500 000 gils** de la banque du clan, placé n'importe où sur le terrain du clan) : il vend les costumes, teintures et autres objets du mod **CostumePack** (objets 9000 à 9021), qui ne sont plus offerts automatiquement : costumes 300 000 gils, teintures 100 000, échangeurs d'animation, effets d'arme et Red Lockheart 150 000. Ils ne se revendent pas. Les teintures bleue, grise, violette, blanche et jaune ne sont pas vendues : aucune règle du CostumePack ne permet de les équiper. Les exemplaires déjà reçus gratuitement sont retirés une seule fois par sauvegarde. Chaque joueur doit avoir CostumePack installé (sinon le couturier n'a rien à vendre). Nécessite CoopServer 1.20.

## Apparence avec le mod Extra Equipment Menu (version de test)

Si le mod **Extra Equipment Menu** (EEM, de WarpedEdge) est installé, les autres joueurs voient ton apparence :
- **en zone et sur la carte du monde** : les couleurs et textures, les parties du modèle montrées ou cachées et, en zone, les costumes complets. C'est l'apparence du personnage que tu diriges ;
- **en raid de boss** : les personnages des autres joueurs ont leurs objets Extra (arme en transmog, costume, couleurs...), appliqués par EEM lui-même pendant le combat ; tes propres choix reviennent après ;
- **pas en duel** (l'adversaire y est un ennemi, qu'EEM ne sait pas habiller).

Chacun doit avoir les **mêmes mods de costumes** (et les mêmes fichiers de règles) : un costume qu'un joueur n'a pas installé ne s'affiche pas chez lui. LifaMods lit seulement ce qu'EEM documente (les objets choisis, enregistrés dans la sauvegarde, et les fichiers `ExtraEquipRules.tsv`), pas son code.

Le mod LifaMods **désactive tous les objets des emplacements Extra** d'EEM (No EXP, No AP, No Gil, « Weapon Scale » et « Model Scale », objets 8000 à 8012) : ils ne sont plus donnés, ne se mettent plus dans les emplacements Extra et n'ont plus d'effet, et ceux déjà dans l'inventaire en sont retirés. Les costumes et les transmogs d'arme continuent de marcher. Cela passe par ses propres fichiers `StreamingAssets/Data/Characters/Extra/ExtraEquipRules.tsv` et `ExtraAutoGrantItems.tsv` : LifaMods doit rester avant ExtraEquipmentMenu dans `FolderNames` de `Memoria.ini`.

## Duels visibles par les autres (version de test)

Pendant un duel en combat, une partie de Tetra Master entre joueurs ou un raid de boss, les joueurs concernés disparaissent de la zone. À leur place, les autres joueurs de la zone voient une icône qui pulse avec ce qui se passe et qui est dedans (« Duel : A contre B », « Tetra Master : A contre B », « Raid de boss : ... »). Elle s'affiche aussi pour un joueur qui arrive dans la zone après le départ, et disparaît dès que les joueurs reviennent. Tous les joueurs doivent avoir cette version du mod.

## Classement des duels (version de test)

- **Pavé numérique 9** (ou l'icône étoile « Rang » en haut à droite), sur le terrain ou la carte du monde : la fenêtre du classement, avec deux onglets, **Combats** (duels F10) et **Tetra Master** (parties F7). Les 50 meilleurs joueurs sont classés par nombre de victoires, puis moins de défaites ; ta place et ton bilan sont en haut. `RankingKey` dans `Coop.ini` pour changer la touche.
- Seuls les duels **entre joueurs** comptent. À la fin d'un duel, chaque jeu envoie son propre résultat ; il n'est compté que si les deux joueurs disent la même chose (une victoire contre une défaite, ou deux nuls). Un message « Classement ... enregistrée » le confirme dans le tchat. Un duel interrompu ou abandonné (déconnexion) ne compte pas.
- Pour éviter de gonfler son score avec un ami : au plus **10 résultats par jour** entre deux mêmes joueurs, pour chaque jeu.
- Le classement est gardé par le **serveur dédié** (fichier `Coop-Ranking.txt` à côté de `CoopServer`, commande `ranking reset` pour tout remettre à zéro) ou, sans serveur, par le jeu de l'**hôte** (`Coop-Ranking.txt` dans son dossier du jeu). Les joueurs sont reconnus par leur pseudo. Nécessite CoopServer 1.8.

## Fête de la chasse (version de test)

Un événement pour tout le serveur : **toutes les 6 h, pendant 1 h** (par défaut à 0 h, 6 h, 12 h et 18 h, heure du serveur), les joueurs comptent leurs points de chasse. Le tchat annonce la fête 5 min avant, puis à son ouverture.
- **Points** : chaque combat **gagné** contre des monstres rapporte le **niveau de chaque monstre** (un monstre niveau 1 = 1 point, niveau 10 = 10 points). Un **boss** compte double (combat qui n'est pas une rencontre aléatoire et dont on ne peut pas fuir). Le tchat affiche les points gagnés après chaque combat. Les duels entre joueurs et les raids de clan ne comptent pas, ni les combats d'un joueur bloqué (ancienne sauvegarde ou partie suspecte).
- **Classement à droite de l'écran** : pendant la fête, une petite fenêtre transparente montre le temps restant, les 10 meilleurs chasseurs avec leurs points, et ta place. La touche **²** la cache ou la remontre (choix gardé dans `Coop.ini`, `ShowHuntPanel`). Les points repartent de zéro à chaque fête.
- **Récompenses** : à la fin, le **1er gagne 100 000 gils**, le **2e 50 000** et le **3e 25 000**. Le tchat annonce le podium. Les gils arrivent dans la partie du gagnant dès qu'il marche librement (tout de suite, ou à sa prochaine connexion s'il était parti), suivis d'une sauvegarde automatique.
- **Classement des fêtes** : la fenêtre du classement (Pavé 9) a un troisième onglet, **Fête de la chasse**, avec les joueurs qui ont fini **le plus souvent 1er**, puis 2e, puis 3e.
- Le serveur garde tout dans `Coop-Hunt.txt`. Réglages du serveur : `--hunt-every 6` (heures entre deux fêtes), `--hunt-length 60` (minutes), `--hunt-offset 0` (heure de la première fête après minuit), ou les variables `HUNT_EVERY`, `HUNT_LENGTH`, `HUNT_OFFSET` (champs de l'egg Pterodactyl). **Programme** (CoopServer 1.27) : `--hunt-schedule "sam 20:00, dim 15:30"` ou la variable `HUNT_SCHEDULE` (champ « Fête de la chasse : programme » de l'egg) fixe les jours et heures des fêtes, à l'heure du serveur ; une heure seule (`18:00`) = tous les jours ; jours en français ou en anglais (`lun`…`dim`, `mon`…`sun`, ou en entier). Quand le programme est rempli, l'intervalle et le décalage sont ignorés, et la durée peut aller jusqu'à 24 h. La commande `hunt` affiche le programme et l'heure du serveur, pour vérifier son fuseau horaire. La commande `hunt` dans la console dit quand commence la prochaine fête, ou qui mène. **Commandes admin** (console du serveur, avec ou sans `/`) : `hunt start` lance une fête tout de suite (durée habituelle, ou `hunt start 30` pour 30 min), `hunt end` arrête la fête en cours : le podium est récompensé comme d'habitude. La fête suivante du programme a lieu normalement. Sans serveur dédié, l'hôte tape `/hunt start` et `/hunt end` dans le tchat. Le décalage compte depuis minuit quand l'intervalle divise 24 h (1, 2, 3, 4, 6, 8, 12, 24). Nécessite CoopServer 1.18.

## Ornements des N°1 (version de test)

Le **N°1 de chaque classement** a un ornement au-dessus du cadre de son pseudo, que tous les joueurs voient :
- **épées croisées** pour le N°1 des **duels en combat** ;
- **carte Tetra Master** pour le N°1 de **Tetra Master** ;
- **trophée doré** pour le N°1 de la **Fête de la chasse** (le plus de premières places).

Un joueur N°1 de plusieurs classements a plusieurs ornements côte à côte. L'ornement passe à un autre joueur dès que celui-ci fait **mieux** (une égalité ne suffit pas), et le tchat l'annonce à tous. La liste F8 indique aussi « (N°1 duels, Tetra, chasse) ». Le serveur dit qui est N°1 toutes les 10 s. Nécessite CoopServer 1.19.

## Familiers (version de test)

Les monstres amicaux de la carte du monde (Mu, Fantôme, Coccinelle, Yéti...) peuvent te suivre. Donne à chacun l'objet qu'il demande, sans l'attaquer : le mod le note dans ta sauvegarde (seulement à partir de l'installation du mod). Quand les 9 monstres amicaux (Eskuriax, Fantôme, Miskoxy, Yéti, Nymphe, Manta, Jabah, Garuda, Belhamel) sont satisfaits, le menu des familiers se débloque (icône « Familier » en haut à droite ou touche `FamiliarKey`, Pavé 8 par défaut) : **Invoquer** / **Renvoyer**. Le familier te suit sur les zones (pas sur la carte du monde), se cache pendant les scènes de l'histoire, et les autres joueurs le voient derrière toi. Réglages dans `Coop.ini` : `FamiliarScale = 0.8` (taille), `FamiliarTurn = 180` (degrés, si le monstre regarde du mauvais côté).

## Anti-triche des sauvegardes (version de test)

Chacun joue sa propre sauvegarde. Pour qu'une sauvegarde modifiée (éditeur, sauvegarde trouvée sur internet) ne puisse pas profiter aux autres, le jeu envoie au serveur (ou à l'hôte), à la connexion, à chaque sauvegarde et à chaque chargement : le temps de jeu, l'avancée dans l'histoire, les gils, le plus haut niveau de l'équipe et le nombre de sortes d'objets. Le serveur vérifie :
- que c'est **possible pour ce temps de jeu** : par exemple pas de niveau 60 ni de millions de gils en 3 h, pas de disque 4 en moins de 8 h, pas presque tous les objets en quelques heures ;
- pour un pseudo déjà connu, que c'est **possible depuis la dernière vérification** : pas de bond de gils, de niveaux ou d'objets en quelques minutes de jeu, et un temps de jeu qui n'avance pas plus vite que l'horloge réelle. Les gils et objets reçus normalement (échanges, banque, trésors, raid) sont comptés en plus.

Les seuils sont larges : un joueur normal n'en approche pas. Une partie suspecte reste jouable, mais la **banque, les échanges, le raid et le classement** sont bloqués. Le joueur voit la raison dans le tchat, la console du serveur l'affiche (lignes `[Anti-triche]`), et le blocage se lève tout seul si le joueur recharge une partie normale ou après 1 h de jeu sur cette partie (comme pour les sauvegardes plus anciennes), ou par l'admin : `unblock <pseudo>` (serveur) ou `/debloquer <pseudo>` (hôte), qui fait de la partie actuelle sa nouvelle référence. Réglage sur le serveur dédié : `--anticheat 100` (200 = deux fois plus large, 0 = désactivé). Sans serveur, chez l'hôte, il tourne toujours au réglage normal (pas d'option dans `Coop.ini`, pour que personne ne le coupe). Limite : tout se passe sur le PC du joueur, un tricheur qui modifie aussi la DLL peut tromper la vérification. Le serveur refuse aussi un pseudo déjà connecté. Nécessite CoopServer 1.9.

## Triches de Memoria coupées

Quand on joue chez un hôte, les triches de Memoria (section `[Cheats]` : vitesse F1, assistance F2, 9999 F3, plus de rencontres F4, compétences F5, niveau 99 F6, gils F7, combat auto) sont coupées chez tout le monde tant qu'on est connecté, et les boosters déjà actifs sont éteints. `Memoria.ini` n'est pas modifié : tout revient comme avant à la déconnexion. Limite : un joueur qui retire ou modifie le mod peut contourner le blocage.

## Serveur dédié

La partie passe par `CoopServer`, un petit programme qui ne joue pas et n'a pas besoin du jeu (un VPS, un NAS, un panel Pterodactyl ou un PC allumé suffisent). Tous les joueurs mettent `HostAddress = <IP du serveur>` dans leur `Coop.ini` (le mod est toujours client, il n'y a plus de réglage `Role`).

- Linux : `out/Serveur/linux-x64/CoopServer --port 7777 --nocheats 0 --maxplayers 16`. Windows : `out/Serveur/windows-x64/CoopServer.exe`, avec les mêmes options. Les variables d'environnement `SERVER_PORT`, `NO_CHEATS` et `MAX_PLAYERS` marchent aussi. **Nom du serveur** : `--name "Mon serveur"` ou la variable `SERVER_NAME` (40 caractères au plus) ; il s'affiche en haut de l'écran des joueurs connectés à la place de « LifaMods » (CoopServer 1.14). Sur Pterodactyl, c'est le champ « Nom du serveur » de l'egg (réimporter l'egg, ou ajouter la variable `SERVER_NAME` à la main). **Mods autorisés** (CoopServer 1.25) : `--mods "Moguri*, ExtraEquipmentMenu, CostumePack"` ou la variable `ALLOWED_MODS` (champ « Mods autorisés » de l'egg) : noms de dossiers de `FolderNames` (Memoria.ini) séparés par des virgules ; LifaMods est toujours autorisé, un sous-dossier est couvert par son dossier, `*` à la fin couvre un début de nom. Un joueur avec un autre mod est refusé et voit la liste des mods à retirer. Vide = tous autorisés. **Mods hors de Memoria** (CoopServer 1.26, activé par défaut) : un joueur avec un chargeur de mods (BepInEx, MelonLoader, Harmony, winhttp.dll/version.dll/doorstop) ou des fichiers ajoutés ou modifiés dans `StreamingAssets` / `FF9_Data` à la racine du jeu est refusé, avec la liste de ce qui a été trouvé ; `--strict-mods 0` ou `STRICT_MODS=0` pour l'accepter. Les fichiers de Memoria sont connus pour Memoria 2025-07-13 et 2026-10-14 (`src/Memoria.Scripts.Coop/Known/root-<version>.txt`, md5 d'une installation propre) ; une autre version n'est pas vérifiée sur ce point.
- Console : `list` (joueurs connectés), `say <texte>` (message « Serveur » dans le tchat), `kick <pseudo ou id>`, `unblock <pseudo>` (débloque un joueur qui joue une sauvegarde plus ancienne ou une partie jugée suspecte, voir l'anti-triche), `ranking reset` (remet le classement des duels à zéro), `hunt` (prochaine Fête de la chasse ou meneurs en cours), `hunt start [minutes]` et `hunt end` (lancer ou arrêter la fête), `give` (dons de l'admin, voir plus bas), `stop`.
- **Dons de l'admin** : `give <pseudo> gil <montant>` (jusqu'à 9 999 999), `give <pseudo> item <n° d'objet> [nombre]` (1 à 99), `give <pseudo> card <n° de carte> [nombre]` (cartes Tetra Master 0 à 99, 1 à 100), `give list` (dons pas encore reçus). Les mots français `gils`, `objet` et `carte` marchent aussi. Le don attend dans `Coop-Gifts.txt` et arrive dans le jeu du joueur dès qu'il est connecté et marche librement (tout de suite s'il est en ligne, sinon à sa prochaine connexion), avec un message dans le tchat et une sauvegarde automatique ; il compte comme un gain reçu pour l'anti-triche. Ce qui ne tient pas (99 exemplaires d'un objet, collection de cartes pleine, 9 999 999 gils) est perdu, et un n° d'objet qui n'existe pas dans le jeu du joueur est ignoré. Sans serveur dédié, l'hôte tape `/give ...` dans le tchat. Nécessite CoopServer 1.22.
- **Admins** : règle un mot de passe admin sur le serveur (`--admin-password <mot de passe>`, variable `ADMIN_PASSWORD`, champ « Mot de passe admin » de l'egg Pterodactyl). Un joueur tape `/admin <mot de passe>` dans le tchat pour devenir admin : le serveur garde son rôle (`Coop-Admins.txt`) et son jeu garde une clé secrète dans `Coop.ini` (`AdminToken`), donc il redevient admin tout seul à chaque connexion, et quelqu'un qui prend le même pseudo ne l'est pas. Ses messages ont une **étoile** et une couleur à part dans le tchat de tout le monde. `/a <texte>` parle sur le **canal des admins** (seuls les admins le voient). Un admin lance les commandes de la console depuis le tchat : `/give`, `/hunt start|end`, `/kick`, `/unblock` (ou `/debloquer`), `/say`, `/list`, `/ranking reset` (la réponse s'affiche dans son tchat ; `stop` reste réservé à la console). `/admin` seul affiche l'aide, `/admin off` rend le rôle. Dans la console : `admin` liste les admins, `admin remove <pseudo>` retire le rôle. Après 5 mauvais mots de passe, l'adresse est bloquée 10 min. Nécessite CoopServer 1.23.
- **Onglets du tchat** : quand le tchat est ouvert (T), un cadre transparent l'entoure avec des onglets en haut : **Général** (tout), **Clan [TAG]** (si tu es dans un clan) et **Admin** (si tu es admin). Clique un onglet : il n'affiche que ce canal, et ce que tu tapes y part directement, sans `/c` ni `/a`. Les commandes en `/` marchent dans tous les onglets. L'onglet reste choisi pour la prochaine fois.
- **Pause** : la touche Pause du jeu (Retour arrière par défaut) est désactivée pendant un duel de combat, une partie de Tetra Master entre joueurs et un raid, car elle ne figerait que ton jeu pendant que les autres continuent. Elle revient à la fin du combat.
- Comme personne n'est l'hôte, chaque joueur sauvegarde sa propre partie (pas besoin de `AllowClientSave`). `NoCheats` se règle sur le serveur avec `--nocheats 1`.
- Tetra Master, F8, tchat et carte du monde marchent comme avec un hôte joueur.
- Pterodactyl : importer `out/Serveur/pterodactyl/egg-ff9-coop-server.json` (Admin > Nests > Import Egg), créer le serveur avec un port TCP, puis envoyer le fichier Linux `CoopServer` à la racine via le gestionnaire de fichiers (ou remplir « Lien de téléchargement » avant l'installation). Le panel détecte le démarrage et le bouton Stop envoie `stop`.
- Le serveur et le mod doivent avoir le même protocole (8) : un mod plus ancien est refusé avec un message clair.

## Installer

1. Memoria doit déjà être installé dans le jeu, en version **v2025.07.04 ou plus récente** (le point d'entrée utilisé par le mod n'existe pas avant).
2. Copier le dossier `out/LifaMods` dans le dossier du jeu, à côté de `FF9_Launcher.exe` :
   `FINAL FANTASY IX/LifaMods/StreamingAssets/Scripts/Memoria.Scripts.LifaMods.dll` (Memoria ne charge que la DLL nommée `Memoria.Scripts.<nom du dossier>.dll`)
3. Activer le mod : dans `Memoria.ini` (racine du jeu), section `[Mod]`, ajouter `LifaMods` à `FolderNames`, par exemple `FolderNames = "LifaMods"` (ou `"LifaMods", "MonAutreMod"`). Si tu avais l'ancien dossier `Coop`, supprime-le et remplace `"Coop"` par `"LifaMods"` dans `FolderNames`. Le gestionnaire de mods du launcher Memoria peut aussi le faire.
4. Lancer le jeu. Sur l'écran titre, un panneau à gauche du menu permet de taper son **pseudo**, l'**adresse** et le **port** du serveur, puis « **Se connecter** » (tout est enregistré dans `Coop.ini`, créé au premier lancement). Sans pseudo, pas de connexion, et le serveur refuse un pseudo déjà connecté. « **Hors ligne** » permet de jouer seul, sans serveur. En jeu, la fenêtre **Échap** (sur le terrain ou la carte du monde) permet aussi de passer en ligne ou hors ligne, avec le pseudo et le serveur choisis au menu titre. Chaque pseudo a ses propres sauvegardes.
5. Les pseudos s'affichent avec la police du jeu. `NameFont` dans `Coop.ini` permet d'utiliser à la place une police installée dans Windows (pour tous les utilisateurs), et `NameFontSize` règle la taille.
6. En haut à gauche s'affiche `LifaMods | ...` avec l'état de la connexion. Le journal détaillé est dans `Memoria.log` (lignes `[Coop]`).

Jouer à distance : le serveur dédié doit avoir son port TCP (7777 par défaut) ouvert ; sur un panel Pterodactyl, c'est le port attribué au serveur.

## Tester seul sur un PC

Lancer deux fois le jeu n'est pas toujours possible avec Steam. Le plus simple est de tester à deux PC sur le même réseau local (`HostAddress` = IP locale de l'hôte, ex. `192.168.1.20`).

## Compiler soi-même

La DLL fournie dans `out/` a été compilée ici contre le code source de Memoria (branche `main` du 2026-10-20) et des DLL Unity de remplacement, car les DLL du jeu ne sont pas redistribuables. Elle devrait fonctionner, mais en cas de souci, recompile-la contre ton propre jeu :

1. Installer le [.NET SDK 8](https://dotnet.microsoft.com/download).
2. Lancer `build.bat "C:\chemin\vers\FINAL FANTASY IX"` (par défaut le chemin Steam standard).
3. La DLL est régénérée dans `out/LifaMods/StreamingAssets/Scripts/`.

## Comment ça marche

| Fichier | Rôle |
|---|---|
| `CoopBootstrap.cs` | Point d'entrée. Memoria n'a pas de hook « démarrage » pour les mods : on s'enregistre comme script `IOverloadVABattleScript`, que Memoria instancie une fois au chargement des scripts, et on s'abonne à `GameLoopManager.Update`. Conséquence : un mod de doublage qui définit aussi ce script et qui est placé **au-dessus** de LifaMods dans `FolderNames` empêcherait LifaMods de démarrer. |
| `CoopConfig.cs` | Lecture de `Coop.ini`. |
| `CoopNet.cs`, `Protocol.cs` | Réseau en étoile : l'hôte accepte les clients et relaie les positions. Un fil d'envoi par connexion, le jeu n'attend jamais le réseau. Testé ici avec un hôte et deux clients. |
| `CoopRuntime.cs`, `LocalPlayer.cs` | À chaque frame : lit le personnage contrôlé (`EventEngine.GetControlChar()`), envoie sa position 15 fois/s avec son heure, rejouée 0,15 s en retard chez les autres, applique les messages reçus. |
| `RemotePlayer.cs`, `RemoteAvatars.cs` | Crée le modèle de chaque autre joueur dans la zone courante (comme `FieldMap.AddFieldChar`) ou sur la carte du monde (comme `WMWorld.addGameObjectToWMActor`, en position absolue car la carte du monde « boucle »), sans collision ni script, interpole sa position (100 ms de retard) et joue son animation. |
| `SaveGuard.cs` | Chez un client, remplace le sérialiseur de sauvegarde du jeu par une version qui refuse `Save` et `Autosave`. |
| `CoopOverlay.cs` | Texte d'état, liste des joueurs (F8), tchat et pseudos à l'écran. |
| `ChatBox.cs`, `InputBlocker.cs` | Historique du tchat ; pendant la saisie, les touches clavier du jeu sont neutralisées (déplacement, confirmer, menu...) pour que taper un message ne fasse pas bouger le personnage. La manette continue de fonctionner. |

En cas d'erreur inattendue, le mod se désactive pour la session (le jeu continue) et écrit l'erreur dans `Memoria.log`.

## Changer les touches du mod

**Échap**, sur le terrain ou la carte du monde (hors menu, combat et scène), ouvre la fenêtre LifaMods. En haut, un bouton passe **en ligne** ou **hors ligne** (avec le pseudo et le serveur du menu titre). En dessous, les touches : clique sur une touche puis appuie sur la nouvelle (Échap annule). Le changement marche tout de suite et est écrit dans `Coop.ini`. La fenêtre signale une touche déjà prise par une autre action du mod, par le jeu (Confirmer, Menu...) ou par une triche de Memoria ; « Touches par défaut » remet tout comme au départ. Le clan et la fouille de trésor partagent F9 exprès (on ne creuse qu'à dos de chocobo). Ailleurs, Échap garde son rôle normal dans le jeu (annuler dans les menus et les combats).

On peut aussi toujours les changer à la main dans `Coop.ini` (`PlayerListKey = F9`, `ChatKey = Y`...). Attention : si l'option **SoftReset** de Memoria est activée, F8 pendant la pause relance le jeu ; dans ce cas, choisis une autre touche.

Hôte et clients doivent avoir la **même version** du mod (sinon la connexion est refusée avec un message).

## Points à vérifier au premier test

- Sur la carte du monde : l'autre joueur apparaît-il à la bonne taille et au bon endroit, y compris en traversant le bord de la carte ? À pied, en chocobo, en bateau ?
- L'avatar s'affiche-t-il au bon endroit et devant/derrière les décors comme les PNJ ?
- Le pseudo est-il au-dessus de la tête (le décalage est une estimation, `CoopOverlay.cs`) ?
- En tapant un message, le personnage reste-t-il immobile ? Les touches du jeu reviennent-elles après Entrée/Échap ?
- Chez le client, Sauvegarder chez un Mog affiche-t-il bien un échec ?

## Auteur et droits

LifaMods, © 2026 Gaddwyn. Tous droits réservés : voir `LICENSE.txt` (aussi copié dans le dossier du mod). Le nom et le copyright sont inscrits dans la DLL (clic droit > Propriétés > Détails sous Windows).
