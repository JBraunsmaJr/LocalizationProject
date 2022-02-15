# How to Use

Add a JSON file in your project root called `localization.json`. Follow the format below.
```json
{
  "HELLO": {
    "en-US": "hello",
    "de-DE": "hallo"
  }
}
```

At build - a couple things are generated for you.

- Referencing the key values as strings can lead to typos and forces a developer to jump between files to know what's there. To combat this, keys are grouped under
the `LocalizationResource` enum! 
- Supported languages are inferred and can also be referenced as an enum, `LocalizationLanguage`.

Given the above JSON example we could do the following:


Pull text using current culture
```csharp
var translatedText = Localizer.Get(LocalizationResource.HELLO);
```

Pull text using specific culture
```csharp
var translatedText = Localizer.Get(LocalizationResource.HELLO, LocalizationLanguage.de_DE);
```

Pass in a culture info reference
```csharp
var translatedText = Localizer.Get(LocalizationResource.HELLO, CultureInfo.CurrentCulture);
```

Pass in a culture info as string
```csharp
var translatedText = Localizer.Get(LocalizationResource.HELLO, "en-US");
```
