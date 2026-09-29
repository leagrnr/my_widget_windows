using System.Linq;

namespace MesWidgets;

public record TypeWidget(string Type, string Nom, string Icone, string Description, string Categorie);

public static class Catalogue
{
    public static readonly string[] Categories = { "Essentiels", "Organisation", "Mon PC", "Internet", "Jeux & fun" };

    public static readonly TypeWidget[] Types =
    {
        new("horloge",      "Horloge",            "🕒", "L'heure et la date",                    "Essentiels"),
        new("postit",       "Post-it",            "📝", "Une note à coller sur le bureau",       "Essentiels"),
        new("calendrier",   "Calendrier",         "📅", "Le mois en cours",                      "Essentiels"),
        new("meteo",        "Météo",              "⛅", "Temps actuel et prévisions",            "Essentiels"),
        new("soleil",       "Soleil & lune",      "🌅", "Lever, coucher, phase de la lune",      "Essentiels"),
        new("citation",     "Citation du jour",   "💬", "Une phrase inspirante chaque jour",     "Essentiels"),
        new("cadrephoto",   "Cadre photo",        "🖼", "Un diaporama de tes images",            "Essentiels"),

        new("taches",       "Liste de tâches",    "✅", "Des choses à faire, à cocher",          "Organisation"),
        new("notes",        "Carnet de notes",    "📓", "Plusieurs notes, avec recherche",       "Organisation"),
        new("pressepapiers","Presse-papiers",     "📋", "Les 10 derniers textes copiés",         "Organisation"),
        new("rebours",      "Compte à rebours",   "⏳", "Les jours avant une date",              "Organisation"),
        new("pomodoro",     "Pomodoro",           "⏱", "Minuteur travail / pause",              "Organisation"),
        new("anniversaires","Anniversaires",      "🎂", "Les prochains anniversaires",           "Organisation"),
        new("conges",       "Congés & fériés",    "🏖", "Vacances scolaires et jours fériés",    "Organisation"),
        new("mondes",       "Horloges du monde",  "🌍", "L'heure dans d'autres villes",          "Organisation"),
        new("raccourcis",   "Raccourcis",         "📁", "Tes applis, dossiers et sites",         "Organisation"),

        new("systeme",      "Infos système",      "📊", "Processeur, mémoire, disque, batterie", "Mon PC"),
        new("batterie",     "Batterie",           "🔋", "Temps restant, alertes pleine / faible","Mon PC"),
        new("temperatures", "Températures",       "🌡", "Température de la carte mère",          "Mon PC"),
        new("musique",      "Musique",            "🎵", "Ce qui joue (Spotify…) + contrôles",    "Mon PC"),
        new("volume",       "Volume",             "🔊", "Régler le son, couper le son",          "Mon PC"),
        new("micro",        "Micro",              "🎙", "Couper le micro d'un clic",             "Mon PC"),
        new("reseau",       "Réseau",             "📶", "Vitesse de téléchargement en direct",   "Mon PC"),
        new("corbeille",    "Corbeille",          "🗑", "Taille de la corbeille, la vider",      "Mon PC"),

        new("recherche",    "Barre de recherche", "🔍", "Google, YouTube, Wikipédia…",           "Internet"),
        new("actualites",   "Actualités",         "📰", "Les derniers titres de l'info",         "Internet"),
        new("air",          "Qualité de l'air",   "🍃", "Pollution et pollens",                  "Internet"),
        new("devises",      "Taux de change",     "💱", "Euro, dollar, livre…",                  "Internet"),
        new("agenda",       "Agenda",             "🗓", "Tes prochains rendez-vous",             "Internet"),

        new("animal",       "Chat",               "🐱", "Un chat qui se promène sur le bureau",  "Jeux & fun"),
        new("jeu2048",      "2048",               "🔢", "Fais glisser, fusionne, atteins 2048",  "Jeux & fun"),
        new("snake",        "Snake",              "🐍", "Le jeu du serpent",                     "Jeux & fun"),
        new("steam",        "Jeux Steam",         "🎮", "Tes derniers jeux, en un clic",         "Jeux & fun"),
    };

    public static TypeWidget Info(string type) => Types.FirstOrDefault(t => t.Type == type);

    public static WidgetWindow Creer(WidgetConfig c) => c.Type switch
    {
        "horloge"       => new HorlogeWidget(c),
        "postit"        => new PostItWidget(c),
        "calendrier"    => new CalendrierWidget(c),
        "meteo"         => new MeteoWidget(c),
        "soleil"        => new SoleilLuneWidget(c),
        "citation"      => new CitationWidget(c),
        "cadrephoto"    => new CadrePhotoWidget(c),
        "taches"        => new TachesWidget(c),
        "notes"         => new NotesWidget(c),
        "pressepapiers" => new PressePapiersWidget(c),
        "rebours"       => new CompteAReboursWidget(c),
        "pomodoro"      => new PomodoroWidget(c),
        "anniversaires" => new AnniversairesWidget(c),
        "conges"        => new CongesWidget(c),
        "mondes"        => new HorlogesMondeWidget(c),
        "raccourcis"    => new RaccourcisWidget(c),
        "systeme"       => new SystemeWidget(c),
        "batterie"      => new BatterieWidget(c),
        "temperatures"  => new TemperaturesWidget(c),
        "musique"       => new MusiqueWidget(c),
        "volume"        => new VolumeWidget(c),
        "micro"         => new MicroWidget(c),
        "reseau"        => new ReseauWidget(c),
        "corbeille"     => new CorbeilleWidget(c),
        "recherche"     => new RechercheWidget(c),
        "actualites"    => new ActualitesWidget(c),
        "air"           => new AirWidget(c),
        "devises"       => new DevisesWidget(c),
        "agenda"        => new AgendaWidget(c),
        "animal"        => new AnimalWidget(c),
        "jeu2048"       => new Jeu2048Widget(c),
        "snake"         => new SnakeWidget(c),
        "steam"         => new SteamWidget(c),
        _ => null,
    };
}
