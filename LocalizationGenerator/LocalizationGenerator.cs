using System;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace LocalizationGenerator
{
    [Generator]
    public class LocalizationGenerator : ISourceGenerator
    {
        private const string ResourceEnum = "LocalizationResource";
        private const string LanguageEnum = "LocalizationLanguage";
        private Regex _fileRegex = new Regex("([a-z]{2}-[A-Z]{2})%*.json");

        /// <summary>
        /// Key: Resource Name -->
        ///     Key: Language Code -->
        ///         Value: Translated Text
        /// </summary>
        private ConcurrentDictionary<string, Dictionary<string, string>> _output =
            new ConcurrentDictionary<string, Dictionary<string, string>>();

        /// <summary>
        /// All languages that have been discovered 
        /// </summary>
        private ConcurrentBag<string> _languages = new ConcurrentBag<string>();
        
        /// <summary>
        /// Tracks a resource key to the languages it can translate to
        /// </summary>
        ConcurrentDictionary<string, HashSet<string>> _tracking = new ConcurrentDictionary<string, HashSet<string>>();
        
        private GeneratorExecutionContext _context;
        
        public void Initialize(GeneratorInitializationContext context)
        {
            
        }

        private void ReportError(string message, DiagnosticSeverity severity = DiagnosticSeverity.Error)
        {
            _context.ReportDiagnostic(
                Diagnostic.Create(new DiagnosticDescriptor("SI001", 
                        message, 
                        message, 
                "Localizer", 
                        severity, 
                        true),
                    null));
        }

        public void Execute(GeneratorExecutionContext context)
        {
            _context = context;
            string assemblyName = context.Compilation.AssemblyName ?? "Localizer";

            var localizationFile = context.AdditionalFiles.FirstOrDefault(x =>
                x.Path.EndsWith("localization.json", StringComparison.OrdinalIgnoreCase));
            
            if (localizationFile is null || !File.Exists(localizationFile.Path))
            {
                Parallel.ForEach(_context.AdditionalFiles, file =>
                {
                    var match = _fileRegex.Match(file.Path);

                    if (!match.Success)
                        return;

                    Dictionary<string, string> contents;

                    try
                    {
                        contents = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(file.Path));
                    }
                    catch (Exception ex)
                    {
                        ReportError(ex.Message);
                        return;
                    }

                    var languageCode = match.Groups[0].Value.Replace(".json","");
                    
                    if(!_languages.Contains(languageCode))
                        _languages.Add(languageCode);

                    foreach (var pair in contents)
                    {
                        var translations = _output.GetOrAdd(pair.Key, new Dictionary<string, string>());

                        _output.AddOrUpdate(pair.Key, new Dictionary<string, string>
                        {
                            [languageCode] = pair.Value
                        },
                        (dictKey, oldValue) => oldValue);

                        var hash = _tracking.GetOrAdd(pair.Key, new HashSet<string>());

                        if (!hash.Contains(languageCode))
                            hash.Add(languageCode);
                        
                        if (!translations.ContainsKey(languageCode))
                            translations.Add(languageCode, pair.Value);
                        else
                            ReportError($"{pair.Key} already contains a translation for {languageCode}", DiagnosticSeverity.Warning);
                    }
                });
                
                GenerateOutput();
                return;
            }

            // single file processing
            Dictionary<string, Dictionary<string, string>> singleFileContents =
                JsonSerializer.Deserialize<Dictionary<string, Dictionary<String, string>>>(
                    File.ReadAllText(localizationFile.Path));

            foreach (var translation in singleFileContents)
            {
                var entry = _tracking.GetOrAdd(translation.Key, new HashSet<string>());
                
                foreach (var language in translation.Value.Keys)
                {
                    if (!_languages.Contains(language))
                        _languages.Add(language);

                    if (!entry.Contains(language))
                        entry.Add(language);
                }

                if (!_output.ContainsKey(translation.Key))
                    _output.TryAdd(translation.Key, translation.Value);
                else
                    ReportError($"{translation.Key} has already been defined", DiagnosticSeverity.Warning);
            }
            
            GenerateOutput();
        }

        string EntryFor(KeyValuePair<string, Dictionary<string, string>> entry)
        {
            using StringWriter stringWriter = new StringWriter();
            using IndentedTextWriter writer = new IndentedTextWriter(stringWriter);
            
            writer.Indent = 3;
            
            writer.WriteLine($"[{ResourceEnum}.{entry.Key}] = new Dictionary<string,string>()");
            writer.WriteLine("{");
            writer.Indent++;
            var keys = entry.Value.Keys.ToList();

            for (int i = 0; i < keys.Count; i++)
            {
                string comma = i == keys.Count - 1 ? "" : ",";
                writer.WriteLine($"[\"{keys[i]}\"] = \"{entry.Value[keys[i]]}\"{comma}");
            }
            
            writer.Indent--;
            writer.WriteLine("}");
            
            return stringWriter.ToString();
        }
        
        private void GenerateOutput()
        {
            // Find the keys that are missing certain translations
            foreach (var entry in _tracking)
            {
                var differences = _languages.Except(entry.Value).ToArray();
                
                if(differences.Any())
                    ReportError($"{entry.Key} is missing translations for {string.Join(", ", differences)}", DiagnosticSeverity.Warning);
            }
            
            List<string> entries = new List<string>();
            foreach (var entry in _output)
                entries.Add(EntryFor(entry));
            
            string template = $@"
namespace {_context.Compilation.AssemblyName ?? "Localizer"};
using System.Collections.Generic;
using System.Linq;

///<summary>
/// Strongly typed approach to referencing key values in language files
///</summary>
public enum {ResourceEnum}
{{
    {string.Join(",\n\t", _output.Keys)}
}}

///<summary>
/// Languages that were discovered during compilation
///</summary>
public enum {LanguageEnum}
{{
    {string.Join(",\n\t", _languages.Select(x=>x.Replace("-","_")))}
}}

public static class Localizer
{{
    private static Dictionary<{LanguageEnum}, string> _supportedLanguages = new Dictionary<{LanguageEnum}, string>()
    {{
        {string.Join(",\n\t\t", _languages.Select(x=>$"[{LanguageEnum}.{x.Replace("-","_")}] = \"{x}\""))}
    }};

    private static Dictionary<{ResourceEnum}, Dictionary<string,string>> _resources =
        new Dictionary<{ResourceEnum}, Dictionary<string,string>>()
        {{
            {string.Join(",\n\t\t\t", entries)}
        }};

    ///<summary>
    /// List of languages supported / found during compilation
    ///</summary>
    public static List<string> SupportedLanguages => _supportedLanguages.Values.ToList();

    ///<summary>
    /// Translate <paramref name=""resource""/> into <paramref name=""language""/>
    ///</summary>
    public static string Get({ResourceEnum} resource, string language)
    {{
        if(!_resources.ContainsKey(resource) || !_resources[resource].ContainsKey(language))
            return string.Empty;
        
        return _resources[resource][language];
    }}

    ///<summary>
    /// Translate <paramref name=""resource""/> into <paramref name=""language""/>
    ///</summary>
    public static string Get({ResourceEnum} resource, {LanguageEnum} language)
    {{
        if(!_resources.ContainsKey(resource) || !_resources[resource].ContainsKey(_supportedLanguages[language]))
            return Get(resource);
        
        return _resources[resource][_supportedLanguages[language]];
    }}

    ///<summary>
    /// Get translation based on current culture
    ///</summary>
    public static string Get({ResourceEnum} resource)
    {{
        string lang = System.Globalization.CultureInfo.CurrentCulture.Name;
        return Get(resource,lang);
    }}

    ///<summary>
    /// Translate <paramref name=""resource""/> using provided culture <paramref name=""info""/>
    ///</summary>
    public static string Get({ResourceEnum} resource, System.Globalization.CultureInfo info)
    {{
        return Get(resource, info.Name);
    }}
}}";
            try
            {
                _context.AddSource("Localizer_generated.cs", template);
            }
            catch (Exception ex)
            {
                ReportError($"{ex.Message},\n\n{template}");
            }
        }
    }
}