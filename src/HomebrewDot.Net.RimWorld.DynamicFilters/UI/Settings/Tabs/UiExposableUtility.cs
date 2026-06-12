using System;
using System.IO;
using System.Linq;
using Verse;

namespace HomebrewDot.Net.Rimworld.UI.Settings.Tabs
{
    internal static class UiExposableUtility
    {
        public static IExposable Clone(IExposable source)
        {
            if (source == null)
            {
                return null;
            }

            var directory = Path.Combine(GenFilePaths.SaveDataFolderPath, "HomebrewedDynamicFilters", "UiTemp");
            var path = Path.Combine(directory, "template-settings-clone.xml");

            try
            {
                Directory.CreateDirectory(directory);

                var toSave = source;
                Scribe.saver.InitSaving(path, "DynamicFiltersUiClone");
                Scribe_Deep.Look(ref toSave, "settings");
                Scribe.saver.FinalizeSaving();

                IExposable clone = null;
                Scribe.loader.InitLoading(path);
                Scribe_Deep.Look(ref clone, "settings");
                Scribe.loader.FinalizeLoading();

                return clone ?? source;
            }
            catch
            {
                return source;
            }
            finally
            {
                try
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
                catch
                {
                }
            }
        }

        public static string[] Validate(HomebrewDot.Net.Rimworld.Configuration.IDynamicPolicyTemplate template, IExposable settings)
        {
            var errors = template?.ValidateSettings(settings);
            return errors?.ToArray() ?? Array.Empty<string>();
        }
    }
}
