using Sandbox.ModAPI;
using System.Collections.Generic;

namespace Jakaria
{
    public static class WaterLocalization
    {
        public static readonly string ModChatName = "WaterMod";

        public static readonly Dictionary<string, Language> Languages = new Dictionary<string, Language>()
        {
            {"english", new Language()},
            {"french", new Language()
            {
                EnglishName = "french",

                WaterModVersion = "Water Mod V{0} de Jakaria.",
                TranslationAuthor = "Jakaria",

                NoPlanetWater = "La planete n'a pas l'eau.",
                NoPlanet = "Il n'ya pas un planete voisin.",

                SetLanguage = "Fixe la langue a '{0}'.",
                SetLanguageNoParse = "Ne peux pas fixer la langue a  '{0}'. la langue se ne peux pas exister.",

                GetQuality = "La nature d'eau est '{0}'.",
                SetQuality = "Fixe la nature d'eau a '{0}'.",
                SetQualityNoParse = "Ne peux pas fixer la nature d'eau a '{0}'.",

                ToggleRenderCOB = "Bascule la visibility du centre de flottabilite.",

                GetBuoyancy = "La flottabilite d'eau est '{0}'.",
                SetBuoyancy = "Fixe la flottabilite d'eau a '{0}'.",
                SetBuoyancyNoParse = "Ne peux pas fixer la flottabilite d'eau a  '{0}'.",

                GetViscosity = "La viscosite d'eau est '{0}'.",
                SetViscosity = "Fixe la viscosite d'eau a '{0}'.",
                SetViscosityNoParse = "Ne peux pas fixer la viscosite d'eau a '{0}'.",

                GetRadius = "Le rayon d'eau est '{0}'.",
                SetRadius = "Fixe le rayon d'eau a '{0}'.",
                SetRadiusNoParse = "Ne peux pas fixer le rayon d'eau a '{0}'.",

                GetWaveHeight = "Le vague hauteur d'eau est '{0}m'.",
                SetWaveHeight = "Fixe le vague hauteur d'eau a '{0}m'.",
                SetWaveHeightNoParse = "Ne peux pas fixer le vague hauteur d'eau a '{0}'.",

                Reset = "Remet les parametres d'eau.",

                GetWaveSpeed = "Le vitesse d'eau est  '{0}'.",
                SetWaveSpeed = "Fixe le vitesse hauteur d'eau a '{0}'.",
                SetWaveSpeedNoParse = "Ne peux pas fixer le vitesse haueteur d'eau a '{0}'.",

                HasWater = "La planete deja as l'eau.",
                CreateWater = "Fait d'eau a la planete.",
                RemoveWater = "Enleve l'eau de la planete.",

                Depth = "Profondeur: {0}m",
            } },

            {"polish", new Language()
            {
                EnglishName = "polish",

                WaterModVersion = "Water Mod V{0} od Jakaria.",
                TranslationAuthor = "ŁukaszLutek",

                NoPlanetWater = "Brak wody na tej planecie.",
                NoPlanet = "Brak planety w pobliżu.",

                SetLanguage = "Ustawiono język na '{0}'. Przetłumaczone przez: {1}.",
                SetLanguageNoParse = "Nie znaleziono języka '{0}', ten język może nie być przetłumaczony.",

                GetQuality = "Obecna jakość wody to '{0}'.",
                SetQuality = "Ustawiono jakość wody na '{0}'.",
                SetQualityNoParse = "Podana jakość wody jest nieprawidłowa '{0}'.",

                ToggleRenderCOB = "Przełączono wyświetlanie środka wyporności.",

                GetBuoyancy = "Obecny mnożnik wyporności to '{0}'.",
                SetBuoyancy = "Ustawiono mnożnik wyporności na '{0}'.",
                SetBuoyancyNoParse = "Podany mnożnik wyporności jest nieprawidłowy '{0}'.",

                GetViscosity = "Obecna lepkość wody to '{0}'.",
                SetViscosity = "Ustawiono lepkość wody na '{0}'.",
                SetViscosityNoParse = "Podana lepkość wody jest nieprawidłowa '{0}'.",

                GetRadius = "Obecny promień wody to '{0}'.",
                SetRadius = "Ustawiono promeń wody na '{0}'.",
                SetRadiusNoParse = "Podany promień wody jest nieprawidłowy '{0}'.",

                GetWaveHeight = "Obecna wysokość fal to '{0}m'.",
                SetWaveHeight = "Ustawiono wysokość fal na '{0}m'.",
                SetWaveHeightNoParse = "Podana wysokość fal jest nieprawidłowa '{0}'.",

                Reset = "Zresetowano ustawienia wody na tej planecie.",

                GetWaveSpeed = "Obecna prędkość fal to '{0}'.",
                SetWaveSpeed = "Ustawiono prędkość fal na '{0}'.",
                SetWaveSpeedNoParse = "Podana prętkość fal jest nieprawidłowa '{0}'.",

                HasWater = "Na tej planecie jest już woda.",
                CreateWater = "Utworzono wodę na najbliższej planecie.",
                RemoveWater = "Usunięto wodę z najbliższej planety.",

                Depth = "głębokość : {0}m",
            } },

            {"german", new Language()
            {
                EnglishName = "german",

                WaterModVersion = "Water Mod V{0} by Jakaria.",
                TranslationAuthor = "Voss",

                NoPlanetWater = "Dieser Planet hat kein Wasser.",
                NoPlanet = "Kein Planet in Reichweite.",

                SetLanguage = "Sprache umstellen auf '{0}'. Übersetzt von {1}.",
                SetLanguageNoParse = "Kein Sprache namens '{0}' verfügbar, für diese Sprache gibt es möglicherweise noch keine Übersetzung.",

                GetQuality = "Die Wasserqualität liegt bei '{0}'.",
                SetQuality = "Wasserqualität geändert auf '{0}'.",
                SetQualityNoParse = "Ungültige Wasserqualität '{0}'.",

                ToggleRenderCOB = "Auftriebspunkt anzeigen Ein/Aus.",

                GetBuoyancy = "Der Auftriebsmultiplikator des Wassers liegt bei '{0}'.",
                SetBuoyancy = "Auftriebsmultiplikator des Wassers geändert auf '{0}'.",
                SetBuoyancyNoParse = "Ungültiger Auftriebsmultiplikator '{0}'.",

                GetViscosity = "Die Viskosität des Wassers liegt bei '{0}'.",
                SetViscosity = "Viskosität des Wassers geändert auf '{0}'.",
                SetViscosityNoParse = "Ungültige Viskosität '{0}'.",

                GetRadius = "Der Radius der Wasseroberfläche liegt bei '{0}'.",
                SetRadius = "Radius der Wasseroberfläche geändert auf '{0}'.",
                SetRadiusNoParse = "Ungültiger Radius '{0}'.",

                GetWaveHeight = "Die Wellenhöhe liegt  bei '{0}m'.",
                SetWaveHeight = "Wellenhöhe geändert auf '{0}m'.",
                SetWaveHeightNoParse = "Ungültige Wellenhöhe '{0}'.",

                Reset = "Wasser Einstellungen für diesen Planeten zurückgesetzt.",

                GetWaveSpeed = "Die Wellengeschwindigkeit liegt bei '{0}'.",
                SetWaveSpeed = "Geschwindigkeit der Wellen geändert auf '{0}'.",
                SetWaveSpeedNoParse = "Ungültige Wellengeschwindigkeit '{0}'.",

                HasWater = "Dieser Planet hat bereits Wasser.",
                CreateWater = "Wasser wurder auf dem nächsten Planeten eingerichtet.",
                RemoveWater = "Das Wasser wurder vom nächsten Planeten in Reichweite entfernt.",

                Depth = "Innigkeit: {0}m",
            } },

            {"pirate", new Language()
            {
                EnglishName = "pirate",

                WaterModVersion = "Water Mod V{0} plundered from Jakaria.",
                TranslationAuthor = "Mad Cap'n LL",

                NoPlanetWater = "Ain't no seas here.",
                NoPlanet = "Arrr. Where is the land?",

                SetLanguage = "Now we be speaking '{0}'. Order from {1}.",
                SetLanguageNoParse = "Never heard of '{0}', what lands come you from?.",

                GetQuality = "Sea looks like '{0} now'.",
                SetQuality = "Pray for '{0}' quiality of liquior.",
                SetQualityNoParse = "That ain't a thing '{0}'.",

                ToggleRenderCOB = "I see center of our ships float! Or maybe not...",

                GetBuoyancy = "Bucket of water feels like '{0}' buckets of water after fe bottels of rum.",
                SetBuoyancy = "We be floain '{0}'more or less captain.",
                SetBuoyancyNoParse = "Who needs numbers? '{0}'.",

                GetViscosity = "Water sticks like '{0}'.",
                SetViscosity = "Captn our enemies added glue to the sea! '{0}'.",
                SetViscosityNoParse = "I aint dumb... '{0}' is not right!.",

                GetRadius = "Earht round? Sea is '{0}m high.",
                SetRadius = "Yarr... Did just seas rise? '{0}'.",
                SetRadiusNoParse = "That ain't good '{0}'.",

                GetWaveHeight = "Waves are '{0}m'.",
                SetWaveHeight = "Seas are '{0}m' uncalm.",
                SetWaveHeightNoParse = "Waves ain't of this height '{0}'.",

                Reset = "Clang came... and wiped the world!",

                GetWaveSpeed = "Ocean's move beneth us '{0}'.",
                SetWaveSpeed = "Winds change, sea moves '{0}'.",
                SetWaveSpeedNoParse = "That's not a wind '{0}'.",

                HasWater = "I'm no blind... sea is there.",
                CreateWater = "World got flooded.",
                RemoveWater = "Cap'n oceans got dry how will we sail?!.",

                Depth = "Ye Depth: {0}m",
            } }
        };

        public static Language CurrentLanguage = Languages.GetValueOrDefault("english");
    }
}
