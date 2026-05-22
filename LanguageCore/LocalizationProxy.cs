using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace LanguageCore
{
    public sealed class LocalizationProxy : INotifyPropertyChanged
    {
        public static LocalizationProxy Instance { get; } = new();

        private LocalizationProxy()
        {
            WeakEventManager<LocalizationService, EventArgs>.AddHandler(
                LocalizationService.Instance,
                nameof(LocalizationService.LanguageChanged),
                OnLanguageChanged);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnLanguageChanged(object? sender, EventArgs e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }

        public string this[string token]
        {
            get
            {

                if (string.IsNullOrWhiteSpace(token)) return string.Empty;

                var parts = token.Split('|');

                if (parts.Length != 2 && parts.Length != 3) return $"!!BadToken Paramter!!";

                if (parts.Length == 2)
                {
                    var baseName = parts[0];
                    var key = parts[1];
                    return LocalizationService.Instance.GetString(baseName, key);
                }
                else
                {
                    var assemblyName = parts[0];
                    var baseName = parts[1];
                    var key = parts[2];
                    var asm = FindLoadedAssemblyByName(assemblyName);
                    if (asm == null) return $"!!NoAsm:{assemblyName}!!";
                    return LocalizationService.Instance.GetString(asm, baseName, key);
                }
            }
        }

        private static Assembly? FindLoadedAssemblyByName(string assemblyName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var name = asm.GetName().Name;
                if (string.Equals(name, assemblyName, StringComparison.OrdinalIgnoreCase))
                    return asm;
            }
            return null;
        }
    }
}
