using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoKoViewer
{
    class SearchSettings
    {
        public bool RatingSafe = true;
        public bool RatingQuestionable = false;
        public bool RatingExplicit = false;
        public bool RatingUnknown = false;

        public HashSet<string> Spiders = new HashSet<string>();

        public SearchSettings()
        {

        }

        public void Save()
        {
            var to = Windows.Storage.ApplicationData.Current.LocalSettings;
            to.Values["SearchSettings_RatingSafe"] = RatingSafe;
            to.Values["SearchSettings_RatingQuestionable"] = RatingQuestionable;
            to.Values["SearchSettings_RatingExplicit"] = RatingExplicit;
            to.Values["SearchSettings_RatingUnknown"] = RatingUnknown;

            var sb = new StringBuilder();
            foreach (var spider in Spiders)
                sb.AppendLine(spider.Trim());
            to.Values["SearchSettings_Spiders"] = sb.ToString().Trim();
        }

        public static SearchSettings Load()
        {
            var settings = new SearchSettings();

            try
            {
                var from = Windows.Storage.ApplicationData.Current.LocalSettings;
                settings.RatingSafe = (bool)from.Values["SearchSettings_RatingSafe"];
                settings.RatingQuestionable = (bool)from.Values["SearchSettings_RatingQuestionable"];
                settings.RatingExplicit= (bool)from.Values["SearchSettings_RatingExplicit"];
                settings.RatingUnknown = (bool)from.Values["SearchSettings_RatingUnknown"];

                var spidersStr = (string)from.Values["SearchSettings_Spiders"];
                var spiders = spidersStr.Split("\n");

                foreach (var i in spiders)
                    if(!String.IsNullOrWhiteSpace(i.Trim()))
                        settings.Spiders.Add(i.Trim());
            }
            catch(Exception)
            {

            }

            return settings;
        }
    }
}
