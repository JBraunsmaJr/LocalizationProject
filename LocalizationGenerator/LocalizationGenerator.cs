using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using System.Text.Json;

namespace LocalizationGenerator
{
    [Generator]
    public class LocalizationGenerator : ISourceGenerator
    {
        private GeneratorExecutionContext _context;

        private const string LANGUAGE_ENUMS = "%LANGS%";
        private const string ENUMS = "%ENUMS%";
        private const string SUPPORTED_LANGUAGES = "%SUPPORTED_LANGUAGES%";
        private const string NAMESPACE = "%NAMESPACE%";
        private const string ENTRIES = "%ENTRIES%";
        
        private const string Template = 
@"
namespace %NAMESPACE%;
using System.Collections.Generic;
using System.Linq;

public enum LocalizationResource
{
    %ENUMS%
}

public enum LocalizationLanguage
{
    %LANGS%
}

public static class Localizer
{
    private static Dictionary<LocalizationLanguage, string> _supportedLanguages = new Dictionary<LocalizationLanguage, string>()
    {
%SUPPORTED_LANGAUGES%
    };

    private static Dictionary<LocalizationResource, Dictionary<string, string>> _resources =
        new Dictionary<LocalizationResource, Dictionary<string, string>>()
        {
%ENTRIES%
        };
    
    public static List<string> SupportedLanguages => _supportedLanguages.Values.ToList();

    public static string Get(LocalizationResource resource, string language)
    {
        if(!_resources.ContainsKey(resource) || !_resources[resource].ContainsKey(language))
            return string.Empty;
        
        return _resources[resource][language];
    }

    public static string Get(LocalizationResource resource, LocalizationLanguage language)
    {
        if(!_resources.ContainsKey(resource) || _supportedLanguages.ContainsKey(language))
            return string.Empty;

        return _resources[resource][_supportedLanguages[language]];
    }

    public static string Get(LocalizationResource resource, System.Globalization.CultureInfo info)
    {
        return Get(resource, info.Name);
    }
}";

        private readonly string _enumStart = new string('\t', 3);
        readonly string _entryStartTab = new string('\t', 4);
        readonly string _entryDictTab = new string('\t', 5);
        readonly string _entryDictItemTab = new string('\t', 6);
        private readonly HashSet<string> _languages = new HashSet<string>();

        public void Initialize(GeneratorInitializationContext context)
        {
            
        }
        
        Dictionary<string, Dictionary<string, string>> ParseFile(string jsonContents)
        {
            if (jsonContents is null)
                throw new Exception("Unable to do anything");

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(jsonContents);
            }
            catch (Exception ex)
            {
                ReportError(ex.Message);
                throw;
            }
        }

        string EntryFor(KeyValuePair<string, Dictionary<string, string>> entry)
        {
            StringBuilder results = new StringBuilder();
            results.AppendLine($"{_entryStartTab}{{LocalizationResource.{entry.Key}, new Dictionary<string,string>()\n{_entryDictTab}{{");
            
            List<string> lines = new List<string>();

            foreach (var pair in entry.Value)
            {
                if (!_languages.Contains(pair.Key))
                    _languages.Add(pair.Key);
                
                lines.Add($"{_entryDictItemTab}[\"{pair.Key}\"] = \"{pair.Value}\"");
            }
            
            results.AppendLine(string.Join(",\n", lines));
            
            results.AppendLine($"{_entryDictTab}}}\n{_entryStartTab}}}");
            return results.ToString();
        }

        void ReportError(string message)
        {
            _context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("SI001", message, 
                message, 
                "Localizer", 
                DiagnosticSeverity.Error, true), null));
        }
        
        public void Execute(GeneratorExecutionContext context)
        {
            _context = context;
            
            string assemblyName = context.Compilation.AssemblyName ?? "Localizer";
            
            var localizationFile = context.AdditionalFiles.FirstOrDefault(x =>
                x.Path.EndsWith("localization.json", StringComparison.InvariantCultureIgnoreCase));
            
            if (localizationFile is null || !File.Exists(localizationFile.Path))
            {
                ReportError("Unable to find localization.json");
                return;
            }
                        
            string contents = File.ReadAllText(localizationFile.Path);
            
            var data = ParseFile(contents);
            
            List<string> entries = new List<string>();
            foreach (var entry in data)
                entries.Add(EntryFor(entry));
            
            // Take the languages and convert them into enums!
            List<string> languageDictionary = new List<string>();
            List<string> languageEnums = new List<string>();

            foreach (var lang in _languages)
            {
                // Sanitize text into something that conforms to C# syntax
                var enumText = lang.Replace("-", "_");
                languageDictionary.Add($"{_entryStartTab}[LocalizationLanguage.{enumText}] = \"{lang}\"");
                languageEnums.Add(enumText);
            }

            string templateContents = Template
                .Replace(NAMESPACE, assemblyName)
                .Replace(ENUMS, string.Join($",\n{_enumStart}", data.Keys))
                .Replace(ENTRIES, string.Join($"{_entryStartTab},\n", entries))
                .Replace(SUPPORTED_LANGUAGES, string.Join($"{_entryStartTab},\n", languageDictionary))
                .Replace(LANGUAGE_ENUMS, string.Join($",\n{_enumStart}", languageEnums));
            
            context.AddSource($"Localizer_generated.cs", templateContents);
        }
    }
}