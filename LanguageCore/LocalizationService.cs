using LanguageCore.Exceptions;
using LanguageCore.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LanguageCore
{
    public sealed class LocalizationService
    {
        /// <summary>
        /// 支持语言列表
        /// </summary>
        private List<LangInfo> supportLangList = new List<LangInfo>();
        /// <summary>
        /// 当前语言
        /// </summary>
        public LangInfo CurrentLang { get; private set; }
        /// <summary>
        /// 语言基本的集合
        /// </summary>
        public Assembly LangAssembly { get; set; }
        /// <summary>
        /// BaseName前缀
        /// </summary>
        public string BaseNamePrefix { get; set; } = "";
        /// <summary>
        /// 语言文件名字
        /// </summary>
        public string LangFileName { get; set; } = "Lang";
        public static LocalizationService Instance { get; } = new();

        private LocalizationService()
        {
        }

        public event EventHandler? LanguageChanged;

        public CultureInfo CurrentCulture { get; private set; } = CultureInfo.DefaultThreadCurrentUICulture ?? CultureInfo.CurrentUICulture;
        private readonly ConcurrentDictionary<(Assembly asm, string baseName), ResourceManager> _rmCache = new();


        public void Init(Assembly langAssembly, string baseNamePrefix, string langFileName, List<LangInfo> infos) {
            LangAssembly = langAssembly;
            BaseNamePrefix = baseNamePrefix;
            LangFileName = langFileName;
            AddSupportLang(infos);
            LoadLanguageConfig();
        }

        public void LoadLanguageConfig()
        {
            var saved = LanguageCore.Language.Default.AppCulture;
            string cultureName;
            if (!string.IsNullOrWhiteSpace(saved))
            {
                cultureName = saved;
                CurrentLang = supportLangList.Find(it => it.Key.Equals(cultureName));
                if (CurrentLang == null) {
                    LangInfo defaultInfo = supportLangList.Find(it => it.IsDefault);
                    cultureName = defaultInfo.Key;
                    CurrentLang = defaultInfo;
                }
            }
            else
            {
                if (supportLangList == null || supportLangList.Count == 0) throw new NolanguageFileException();
                LangInfo defaultInfo = supportLangList.Find(it => it.IsDefault);

                if (defaultInfo == null) throw new NoDefaultLangException();

                cultureName = defaultInfo.Key;
                CurrentLang = defaultInfo;
            }

            ChangeCulture(cultureName);
        }

        public void AddSupportLang(LangInfo info) {
            supportLangList.Add(info);
        }

        public void AddSupportLang(List<LangInfo> infos)
        {
            foreach (LangInfo info in infos) AddSupportLang(info);
        }

        public void ChangeCulture(string cultureName)
        {
            var culture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;


            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            CurrentCulture = culture;

            LanguageChanged?.Invoke(this, EventArgs.Empty);

            LanguageCore.Language.Default.AppCulture = cultureName;
            LanguageCore.Language.Default.Save();
        }

        public void ChangeCulture(LangInfo lang)
        {
            ChangeCulture(lang.Key);
            CurrentLang = lang;
        }



        public string GetString(Assembly assembly, string baseName, string key)
        {
            if (assembly is null) throw new ArgumentNullException(nameof(assembly));
            if (string.IsNullOrWhiteSpace(key)) return string.Empty;

            var normalizedBaseName = string.IsNullOrWhiteSpace(baseName)
                ? $"{BaseNamePrefix}.{LangFileName}"
                : $"{BaseNamePrefix}.{baseName}.{LangFileName}";

            var rm = _rmCache.GetOrAdd((assembly, normalizedBaseName), k => new ResourceManager(k.baseName, k.asm));
            return rm.GetString(key, CurrentCulture) ?? $"!!{key}!!";
        }

        public string GetString(string baseName, string key) => GetString(LangAssembly,baseName,key);

        public string this[string module, string key] => Instance.GetString(module, key);
    }
}
