# Mes Widgets

Des widgets pour le bureau Windows : horloge, post-it, météo, calendrier, musique, notes, GitHub, Valorant, jeux…

Les widgets restent **collés au bureau**, toujours derrière tes applications, bien alignés et sans jamais se chevaucher. Ils se lancent au démarrage de Windows et se règlent avec leur bouton ✏ **Modifier**.

![Windows 10 / 11](https://img.shields.io/badge/Windows-10%20%2F%2011-0078D4)
![.NET 10](https://img.shields.io/badge/.NET-10-512BD4)
![Version](https://img.shields.io/badge/version-2.13.0-4F8CFF)

---

## ✨ Fonctionnalités

- **36 widgets** répartis en 6 catégories (liste ci-dessous)
- **Collés au bureau** : toujours derrière tes applications (jamais par-dessus), visibles avec `Win` + `D`, invisibles dans `Alt` + `Tab`
- **Alignement automatique** : un widget lâché près d'un autre s'aimante sur ses bords, ou se place juste à côté avec un espace régulier. Le bouton *Aligner tous les widgets* redresse toute la disposition d'un coup.
- **Pas de chevauchement** : un widget lâché ou agrandi contre un autre **rétrécit** juste assez pour tenir (jusqu'à 50 %). S'il n'y a vraiment pas la place, il se décale vers l'espace libre le plus proche. Tu peux choisir de toujours le décaler plutôt que le rétrécir.
- **Toujours dans l'écran** : un widget ne peut pas sortir de l'écran ni passer sous la barre des tâches. Il bute contre les bords quand tu le déplaces, et il est ramené à l'intérieur s'il grandit trop près d'un bord ou après un changement d'écran.
- **Taille libre** : tire le coin en bas à droite d'un widget, ou `Ctrl` + molette (de 50 % à 300 %)
- **Bouton ✏ Modifier** sur chaque widget (visible au survol) : réglages, taille, opacité, verrouillage, suppression
- **Personnalisable** : thème clair ou sombre, 8 couleurs d'accent, 7 polices, coins plus ou moins arrondis, transparence du fond. Les menus et les fenêtres de configuration suivent le même style.
- **Raccourci clavier global** : `Win` + `Alt` + `M` pour couper ou réactiver le micro
- **Sauvegarde** : exporter ou restaurer ta disposition dans un fichier `.json`
- **Plusieurs écrans** pris en charge, avec un bouton pour ramener les widgets sur l'écran principal
- **Mises à jour automatiques** depuis les *Releases* GitHub (voir [Publier une nouvelle version](#-publier-une-nouvelle-version))
- **Sans compte pour l'essentiel** : la plupart des widgets utilisent des services gratuits et publics. Seuls GitHub, GitLab et Valorant demandent un jeton ou une clé (voir [Connecter tes comptes](#-connecter-tes-comptes)).

## 🧩 Les widgets

| Catégorie | Widgets |
|---|---|
| **Essentiels** | 🕒 Horloge · 📝 Post-it (6 couleurs) · 📅 Calendrier · ⛅ Météo · 🌅 Soleil & lune · 💬 Citation du jour · 🖼 Cadre photo |
| **Organisation** | ✅ Liste de tâches · 📓 Carnet de notes · 📋 Presse-papiers · ⏳ Compte à rebours · ⏱ Pomodoro · 🎂 Anniversaires · 🏖 Congés & jours fériés · 🌍 Horloges du monde · 📁 Raccourcis (applis installées, dossiers, sites) |
| **Mon PC** | 📊 Infos système · 🔋 Batterie · 🌡 Températures · 🎵 Musique (Spotify, navigateur…) · 🔊 Volume · 🎙 Micro · 📶 Réseau · 🗑 Corbeille |
| **Internet** | 🔍 Barre de recherche · 📰 Actualités (flux RSS) · 🍃 Qualité de l'air & pollens · 💱 Taux de change · 🗓 Agenda (Google / Outlook) |
| **Développement** | 🐙 GitHub · 🦊 GitLab (gitlab.com ou ton propre serveur) : relectures demandées, tâches assignées, notifications, graphique de contributions |
| **Jeux & fun** | 🐱 Un chat qui se promène sur le bureau · 🔢 2048 · 🐍 Snake · 🎮 Derniers jeux Steam · 🎯 Valorant (rang, RR, dernières parties classées) |

---

## 📥 Installation

### Configuration requise

- Windows 10 (version 2004 ou plus récente) ou Windows 11, 64 bits
- **Rien d'autre à installer** : le programme contient tout ce dont il a besoin
- Pas besoin d'être administrateur

### Étapes

1. Va dans l'onglet [**Releases**](../../releases) du dépôt.
2. Télécharge **`MesWidgets-Setup.exe`** depuis la dernière version.
3. Double-clique sur le fichier, puis réponds **Oui** à la question « Installer Mes Widgets sur ce PC ? ».

C'est tout. Mes Widgets s'ouvre avec une horloge et un post-it, et la fenêtre de gestion apparaît pour ajouter d'autres widgets.

> ⚠️ **Avertissement Windows SmartScreen**
> Le programme n'est pas signé numériquement, donc Windows peut afficher « Windows a protégé votre ordinateur ».
> Clique sur **Informations complémentaires**, puis sur **Exécuter quand même**.

### Ce que fait l'installation

| Élément | Emplacement |
|---|---|
| Programme | `%LOCALAPPDATA%\Programs\MesWidgets\MesWidgets.exe` |
| Tes widgets et réglages | `%LOCALAPPDATA%\MesWidgets\config.json` |
| Jetons et clés (GitHub, GitLab, Valorant) | Gestionnaire d'identification de Windows, chiffrés |
| Raccourci | Menu Démarrer › **Mes Widgets** |
| Lancement automatique | À l'ouverture de session (réglable dans le gestionnaire) |

Mes Widgets apparaît aussi dans **Paramètres › Applications › Applications installées**.

### Mise à jour

Pour passer à une nouvelle version, lance le nouveau `MesWidgets-Setup.exe` : il remplace l'ancienne version et **garde tes widgets**. Si les mises à jour automatiques sont activées (voir plus bas), Mes Widgets te le propose tout seul.

### Désinstallation

Va dans **Paramètres › Applications › Applications installées**, choisis **Mes Widgets**, puis **Désinstaller**.

Le programme te demande si tu veux garder tes widgets et leurs réglages, pour les retrouver si tu le réinstalles plus tard.

---

## 🖱️ Utilisation

| Action | Comment |
|---|---|
| Ouvrir le gestionnaire | Clic sur l'icône **Mes Widgets** près de l'horloge Windows (parfois cachée derrière la flèche **^**) |
| Ajouter un widget | Gestionnaire › clic sur une tuile, ou clic droit sur l'icône › *Ajouter un widget* |
| Déplacer un widget | Attraper la **poignée** en haut au centre (visible au survol), ou glisser le widget n'importe où : il s'aimante aux widgets voisins et aux bords de l'écran |
| Aligner toute la disposition | Gestionnaire › *Disposition et sauvegarde* › **Aligner tous les widgets** |
| Agrandir / réduire un widget | Tirer le coin en bas à droite (visible au survol), ou `Ctrl` + molette |
| Régler un widget | Passer la souris dessus, puis clic sur le bouton ✏ en haut à droite (ou clic droit) |
| Supprimer un widget | ✏ › *Supprimer ce widget* |
| Voir les widgets cachés par tes fenêtres | `Win` + `D` (afficher le bureau) |

L'aimantation et la règle « pas de chevauchement » se désactivent dans **Gestionnaire › Réglages**.

---

## 🔑 Connecter tes comptes

Ces trois widgets ont besoin d'un accès à ton compte. Clique sur **Se connecter** ou **Configurer** dans le widget, puis suis les étapes.

| Widget | Ce qu'il faut | Où l'obtenir |
|---|---|---|
| 🐙 **GitHub** | Un jeton d'accès personnel (*classic*) avec les droits `repo`, `notifications` et `read:user` | Le bouton du widget ouvre la page de création avec les bons droits déjà cochés |
| 🦊 **GitLab** | L'adresse de ton serveur (gitlab.com par défaut) et un jeton avec les droits `read_api` et `read_user` | Idem : le bouton ouvre la bonne page sur ton serveur |
| 🎯 **Valorant** | Ton Riot ID (`Pseudo#TAG`), ta région et une clé de l'API communautaire HenrikDev (gratuite) | Riot ne donne pas accès aux données de Valorant aux projets personnels : la clé s'obtient via la [documentation HenrikDev](https://docs.henrikdev.xyz) (Discord) |

Les jetons et la clé sont rangés **chiffrés** dans le Gestionnaire d'identification de Windows, jamais dans les réglages ni dans les sauvegardes. Pense à choisir une date d'expiration pour tes jetons. Quand ils expirent, le widget te le signale.

---

## 🛠️ Compiler depuis le code source

### Prérequis

- [SDK .NET 10](https://dotnet.microsoft.com/download/dotnet/10.0), par exemple avec :
  ```powershell
  winget install Microsoft.DotNet.SDK.10
  ```
- Un éditeur au choix : Visual Studio 2022 ou plus récent, VS Code (avec l'extension C#) ou JetBrains Rider

### Compiler et tester

```powershell
git clone https://github.com/<ton-pseudo>/MesWidgets.git
cd MesWidgets
dotnet build -c Release
```

L'exécutable compilé se trouve dans `bin\Release\net10.0-windows10.0.19041.0\win-x64\`. Lancé depuis un dossier `bin`, il fonctionne directement, sans passer par l'installation.

### Créer l'installateur et l'installer sur ton PC

```powershell
.\installer.ps1
```

Ce script :
1. compile le programme en **un seul fichier autonome** : `dist\MesWidgets-Setup.exe` (environ 80 Mo) ;
2. l'installe sur ton PC, en remplaçant la version précédente et en gardant tes widgets.

### Structure du projet

```
MesWidgets/
├── App.xaml(.cs)              Démarrage, icône près de l'horloge, style des menus, gestion des widgets
├── GestionnaireWindow.xaml    Fenêtre de gestion (ajout, apparence, disposition, réglages…)
├── WidgetWindow.cs            Base commune de tous les widgets (bouton ✏, taille, déplacement…)
├── Widgets/                   Un fichier par widget + Catalogue.cs (la liste)
│   └── ForgeWidget.cs         Base commune des widgets GitHub et GitLab
├── Dialogue.cs                Modèle des fenêtres de configuration
├── Dialogues.xaml             Style des champs, boutons, listes de ces fenêtres
├── Saisie.cs                  Petite fenêtre « saisir un texte »
├── ConnexionForge.cs          Connexion GitHub / GitLab
├── ChoixApplications.cs       Choix des applis installées (widget Raccourcis)
├── Applications.cs            Liste et icônes des applis installées
├── Theme.cs                   Couleurs, polices, arrondis
├── Placement.cs               Alignement automatique et règle « pas de chevauchement »
├── Windows.cs                 Fenêtres collées au bureau et gardées dans l'écran, lancement au démarrage
├── Coffre.cs                  Jetons et clés dans le Gestionnaire d'identification
├── Lieux.cs                   Recherche de villes (météo, soleil, qualité de l'air)
├── Installation.cs            Installation / désinstallation
├── MiseAJour.cs               Mises à jour via GitHub Releases
├── RaccourcisClavier.cs       Win+Alt+M (couper le micro)
├── Config.cs                  Sauvegarde dans %LOCALAPPDATA%\MesWidgets
├── installer.ps1              Compile l'installateur et installe
└── outils/creer-icone.ps1     Dessine l'icône app.ico
```

### Ajouter un nouveau widget

1. Crée une classe dans `Widgets/` qui hérite de `WidgetWindow` (inspire-toi de `HorlogeWidget.cs`, le plus simple).
2. Ajoute une ligne dans `Catalogue.Types` et une ligne dans `Catalogue.Creer`.

Tout le reste vient automatiquement de la classe de base : déplacement, bouton ✏ Modifier et son menu, taille, opacité, thème, sauvegarde, alignement et placement sans chevauchement. Pour une fenêtre de configuration, hérite de `Dialogue` : elle aura automatiquement le bon style.

---

## 🚀 Publier une nouvelle version

1. Augmente `<Version>` dans `MesWidgets.csproj` (par exemple `2.11.0`).
2. Lance `.\installer.ps1` pour créer `dist\MesWidgets-Setup.exe`.
3. Sur GitHub, crée une **Release** avec le tag `v2.11.0` et joins-lui le fichier `MesWidgets-Setup.exe`.

Pour recevoir la mise à jour automatiquement, les utilisateurs saisissent le nom du dépôt (`<ton-pseudo>/MesWidgets`) dans **Gestionnaire › Réglages › Mises à jour depuis GitHub**. Mes Widgets vérifie ensuite toutes les 12 heures et propose d'installer la nouvelle version.

> 💡 Pour que ce soit automatique pour tout le monde, mets ton dépôt comme valeur par défaut de `Depot` dans `Config.cs`.

---

## 🔒 Confidentialité

Mes Widgets ne collecte aucune donnée. Certains widgets contactent des services en ligne :

| Service | Utilisé par | Ce qui est envoyé |
|---|---|---|
| [Open-Meteo](https://open-meteo.com) | Météo, Soleil & lune, Qualité de l'air | La ville choisie et ses coordonnées |
| [Frankfurter](https://frankfurter.dev) (taux de la BCE) | Taux de change | Les devises choisies |
| [data.education.gouv.fr](https://data.education.gouv.fr) | Congés & fériés | La zone scolaire |
| [valorant-api.com](https://valorant-api.com) | Valorant | Rien (icônes des rangs et des agents) |
| Flux RSS des journaux | Actualités | Rien (simple lecture du flux) |
| Ton agenda (adresse iCal) | Agenda | Rien (simple lecture de l'agenda) |
| API GitHub | Mises à jour, widget GitHub | Rien pour les mises à jour · ton jeton d'accès pour le widget |
| API GitLab (ton serveur) | Widget GitLab | Ton jeton d'accès |
| [HenrikDev API](https://docs.henrikdev.xyz) (non officielle) | Widget Valorant | Ton Riot ID, ta région et ta clé API |

Les jetons GitHub et GitLab et la clé Valorant sont rangés dans le **Gestionnaire d'identification de Windows** (chiffrés, liés à ton compte Windows), jamais dans `config.json` ni dans les sauvegardes exportées. Ils sont effacés quand tu supprimes le widget ou cliques sur *Se déconnecter*.

Le widget **Presse-papiers** garde son historique uniquement en mémoire, jamais sur le disque. Il ignore aussi le contenu que les gestionnaires de mots de passe demandent de ne pas mémoriser.

---

## ⚠️ Limites connues

- **Températures** : seuls les capteurs de la carte mère (ACPI) sont lisibles sans droits administrateur. Le processeur cœur par cœur et la carte graphique ne sont pas disponibles.
- **Agenda** : les répétitions courantes sont gérées (tous les jours, semaines, mois, ans). Les règles complexes, comme « le 2e mardi du mois », ne le sont pas.
- **Valorant** : il dépend d'une API communautaire non officielle. Si elle change ou tombe en panne, le widget ne peut plus se mettre à jour. Une clé gratuite limite aussi le nombre de demandes.
- **GitLab** : le graphique de contributions ne s'affiche que si ton profil est public. Le reste du widget fonctionne quand même.
- **Déplacement** : pendant qu'on le déplace, un widget peut passer au-dessus des autres. Il s'aligne et se range quand on le lâche.
- **Nouveau widget** : comme les widgets restent toujours derrière les fenêtres, un widget ajouté depuis le gestionnaire apparaît derrière celui-ci. Utilise `Win` + `D` pour le voir.

---

## 📝 Historique des versions

| Version | Nouveautés |
|---|---|
| **2.13** | Poignée de déplacement en haut de chaque widget |
| **2.12** | Un widget qui manque de place rétrécit au lieu d'être déplacé |
| **2.11** | Les widgets ne peuvent plus sortir de l'écran ni passer sous la barre des tâches |
| **2.10** | Fenêtres de configuration redessinées (thème sombre / clair) et plus jamais au premier plan |
| **2.9** | Widget Valorant |
| **2.8** | Widgets GitHub et GitLab, jetons chiffrés dans Windows |
| **2.7** | Alignement automatique (aimantation) et bouton *Aligner tous les widgets* |
| **2.6** | Les widgets ne passent plus jamais devant une application |
| **2.5** | Nouveau style des menus |
| **2.4** | Bouton ✏ Modifier sur chaque widget |
| **2.3** | Taille libre (poignée et `Ctrl` + molette) |
| **2.2** | Ajout d'applis installées dans Raccourcis, depuis une liste |
| **2.1** | Pas de chevauchement entre widgets |
| **2.0** | 13 nouveaux widgets, personnalisation, installateur autonome, mises à jour, sauvegarde |
| **1.x** | Premières versions : 20 widgets, thème clair / sombre, gestionnaire |
