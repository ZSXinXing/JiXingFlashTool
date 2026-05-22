using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Markup;
using System.Xaml;

namespace LanguageCore
{
    [MarkupExtensionReturnType(typeof(string))]
    public sealed class LocExtension : MarkupExtension
    {
        public string Key { get; set; } = string.Empty;

        public string BaseName { get; set; } = string.Empty;

        public string? AssemblyName { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var asmName = AssemblyName ?? InferTargetAssemblyName(serviceProvider) ?? Assembly.GetExecutingAssembly().GetName().Name ?? "";
            var token = $"{asmName}|{BaseName}|{Key}";
            return new Binding($"[{token}]")
            {
                Source = LocalizationProxy.Instance,
                Mode = BindingMode.OneWay
            }.ProvideValue(serviceProvider);
        }

        private static string? InferTargetAssemblyName(IServiceProvider serviceProvider)
        {
            if (serviceProvider.GetService(typeof(IRootObjectProvider)) is IRootObjectProvider rop
                && rop.RootObject is not null)
            {
                return rop.RootObject.GetType().Assembly.GetName().Name;
            }
            if (serviceProvider.GetService(typeof(IProvideValueTarget)) is IProvideValueTarget pvt
                && pvt.TargetObject is not null)
            {
                var asm = pvt.TargetObject.GetType().Assembly.GetName().Name;
                if (!string.Equals(asm, "PresentationFramework", StringComparison.OrdinalIgnoreCase))
                    return asm;
            }
            return Assembly.GetEntryAssembly()?.GetName().Name
                   ?? Assembly.GetExecutingAssembly().GetName().Name;
        }
    }
}
