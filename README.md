# Mes Widgets

Des widgets pour le bureau Windows : horloge, post-it, météo, calendrier, musique, notes, jeux… 

Les widgets restent **collés au bureau**, derrière tes fenêtres, sans jamais se chevaucher. Ils se lancent au démarrage de Windows et se règlent avec leur bouton ✏ **Modifier**.

![Windows 10 / 11](https://img.shields.io/badge/Windows-10%20%2F%2011-0078D4)
![.NET 10](https://img.shields.io/badge/.NET-10-512BD4)
![Version](https://img.shields.io/badge/version-2.7.0-4F8CFF)

---

## ✨ Fonctionnalités

- **33 widgets** répartis en 5 catégories (liste ci-dessous)
- **Collés au bureau** : toujours derrière tes applications (jamais par-dessus), visibles avec Win+D, invisibles dans Alt+Tab
- **Pas de chevauchement** : un widget lâché sur un autre se décale vers la place libre la plus proche
- **Alignement automatique** : un widget lâché près d'un autre s'aimante sur ses bords (ou juste à côté, avec un espace régulier), et le bouton *Aligner tous les widgets* redresse toute la disposition
- **Personnalisable** : thème clair ou sombre, 8 couleurs d'accent, 7 polices, coins plus ou moins arrondis, transparence du fond
- **Taille libre** : tire le coin en bas à droite d'un widget, ou Ctrl + molette (de 50 % à 300 %)
- **Pour chaque widget** : opacité, verrouillage de la position et de la taille
- **Raccourci clavier global** :
    - `Win` + `Alt` + `M` : couper ou réactiver le micro
- **Sauvegarde** : exporter ou restaurer ta disposition dans un fichier `.json`
- **Plusieurs écrans** pris en charge, avec un bouton pour ramener les widgets sur l'écran principal
- **Mises à jour automatiques** depuis les *Releases* GitHub (voir [Publier une nouvelle version](#-publier-une-nouvelle-version))
- **Aucun compte ni clé API** : tous les services en ligne utilisés sont gratuits et publics

## 🧩 Les widgets

| Catégorie | Widgets |
|---|---|
| **Essentiels** | 🕒 Horloge · 📝 Post-it (6 couleurs) · 📅 Calendrier · ⛅ Météo · 🌅 Soleil & lune · 💬 Citation du jour · 🖼 Cadre photo |
| **Organisation** | ✅ Liste de tâches · 📓 Carnet de notes · 📋 Presse-papiers · ⏳ Compte à rebours · ⏱ Pomodoro · 🎂 Anniversaires · 🏖 Congés & jours fériés · 🌍 Horloges du monde · 📁 Raccourcis (applis, dossiers, sites) |
| **Mon PC** | 📊 Infos système · 🔋 Batterie · 🌡 Températures · 🎵 Musique (Spotify, navigateur…) · 🔊 Volume · 🎙 Micro · 📶 Réseau · 🗑 Corbeille |
| **Internet** | 🔍 Barre de recherche · 📰 Actualités (flux RSS) · 🍃 Qualité de l'air & pollens · 💱 Taux de change · 🗓 Agenda (Google / Outlook) |
| **Jeux & fun** | 🐱 Un chat qui se promène sur le bureau · 🔢 2048 · 🐍 Snake · 🎮 Derniers jeux Steam |

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
| Raccourci | Menu Démarrer › **Mes Widgets** |
| Lancement automatique | À l'ouverture de session (réglable dans le gestionnaire) |

Mes Widgets apparaît aussi dans **Paramètres › Applications › Applications installées**.

### Désinstallation

Va dans **Paramètres › Applications › Applications installées**, choisis **Mes Widgets**, puis **Désinstaller**.

Le programme te demande si tu veux garder tes widgets et leurs réglages, pour les retrouver si tu le réinstalles plus tard.

---

## 🖱️ Utilisation

| Action | Comment |
|---|---|
| Ouvrir le gestionnaire | Clic sur l'icône **Mes Widgets** près de l'horloge Windows (parfois cachée derrière la flèche **^**) |
| Ajouter un widget | Gestionnaire › clic sur une tuile, ou clic droit sur l'icône › *Ajouter un widget* |
| Déplacer un widget | Le glisser avec la souris |
| Agrandir / réduire un widget | Tirer le coin en bas à droite (visible au survol), ou Ctrl + molette |
| Régler un widget | Passer la souris dessus, puis clic sur le bouton ✏ en haut à droite (ou clic droit) |
| Supprimer un widget | ✏ › *Supprimer ce widget* |
| Voir les widgets cachés par tes fenêtres | `Win` + `D` (afficher le bureau) |

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
├── App.xaml(.cs)              Démarrage, icône près de l'horloge, gestion des widgets
├── GestionnaireWindow.xaml    Fenêtre de gestion (ajout, apparence, réglages…)
├── WidgetWindow.cs            Base commune de tous les widgets
├── Widgets/                   Un fichier par widget + Catalogue.cs (la liste)
├── Theme.cs                   Couleurs, polices, arrondis
├── Placement.cs               Règle « pas de chevauchement »
├── Installation.cs            Installation / désinstallation
├── MiseAJour.cs               Mises à jour via GitHub Releases
├── RaccourcisClavier.cs       Win+Alt+M (couper le micro)
├── Applications.cs            Liste des applis installées (widget Raccourcis)
├── Config.cs                  Sauvegarde dans %LOCALAPPDATA%\MesWidgets
├── installer.ps1              Compile l'installateur et installe
└── outils/creer-icone.ps1     Dessine l'icône app.ico
```

### Ajouter un nouveau widget

1. Crée une classe dans `Widgets/` qui hérite de `WidgetWindow` (inspire-toi de `HorlogeWidget.cs`, le plus simple).
2. Ajoute une ligne dans `Catalogue.Types` et une ligne dans `Catalogue.Creer`.

Le déplacement, le bouton ✏ Modifier et son menu, la taille, l'opacité, le thème, la sauvegarde et le placement sans chevauchement viennent automatiquement de la classe de base.

---

## 🚀 Publier une nouvelle version

1. Augmente `<Version>` dans `MesWidgets.csproj` (par exemple `2.3.0`).
2. Lance `.\installer.ps1` pour créer `dist\MesWidgets-Setup.exe`.
3. Sur GitHub, crée une **Release** avec le tag `v2.3.0` et joins-lui le fichier `MesWidgets-Setup.exe`.

Pour que les utilisateurs reçoivent la mise à jour automatiquement, ils saisissent le nom du dépôt (`<ton-pseudo>/MesWidgets`) dans **Gestionnaire › Réglages › Mises à jour depuis GitHub**. Mes Widgets vérifie ensuite toutes les 12 heures et propose d'installer la nouvelle version.

> 💡 Pour que ce soit automatique pour tout le monde, mets ton dépôt comme valeur par défaut de `Depot` dans `Config.cs`.

---

## 🔒 Confidentialité

Mes Widgets ne collecte aucune donnée. Seuls certains widgets contactent des services publics, sans compte :

| Service | Utilisé par | Ce qui est envoyé |
|---|---|---|
| [Open-Meteo](https://open-meteo.com) | Météo, Soleil & lune, Qualité de l'air | La ville choisie et ses coordonnées |
| [Frankfurter](https://frankfurter.dev) (taux de la BCE) | Taux de change | Les devises choisies |
| [data.education.gouv.fr](https://data.education.gouv.fr) | Congés & fériés | La zone scolaire |
| Flux RSS des journaux | Actualités | Rien (simple lecture du flux) |
| Ton agenda (adresse iCal) | Agenda | Rien (simple lecture de l'agenda) |
| API GitHub | Mises à jour | Rien (lecture de la dernière Release) |

Le widget **Presse-papiers** garde son historique uniquement en mémoire, jamais sur le disque. Il ignore aussi le contenu que les gestionnaires de mots de passe demandent de ne pas mémoriser.

---

## ⚠️ Limites connues

- **Températures** : seuls les capteurs de la carte mère (ACPI) sont lisibles sans droits administrateur. Le processeur cœur par cœur et la carte graphique ne sont pas disponibles.
- **Agenda** : les répétitions courantes sont gérées (tous les jours, semaines, mois, ans). Les règles complexes, comme « le 2e mardi du mois », ne le sont pas.
- **Déplacement** : pendant qu'on le déplace, un widget peut passer au-dessus des autres. Il se range seulement quand on le lâche.
