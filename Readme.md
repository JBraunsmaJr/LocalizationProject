# Install

```
Install-Package LocalizationGenerator
```

# Setup

Two methods are supported. 

### Single File Approach

Add a JSON file in your project root called `localization.json`. Follow the format below.
```json
{
  "HELLO": {
    "en-US": "hello",
    "de-DE": "hallo"
  }
}
```

### Multi-File Approach
Similar to Microsoft's resx naming convention you need to use the following format `xx-XX.json` where `xx-XX` is the language code.

Example: `en-US.json`

Inside this language file, the following format is expected

```json
{
  "HELLO": "Hello"
}
```

----

## File Properties

This source generator relies on `AdditionalFiles`, use one of the 3 methods below to complete setup.

**Method 1**

If using Rider

- Right click file --> Properties
- Change build action to `AdditionalFiles`

**Method 2**

If using Visual Studio XXXX

- Click on file
- View the item properties panel and change build action to `AdditionalFiles`

**Method 3**

Manually add the following group to your csproj

(for single file approach)
```xml
<ItemGroup>
  <None Remove="localization.json" />
  <AdditionalFiles Include="localization.json"/>
</ItemGroup>
```

(for multi-file approach) -- I recommend grouping these files into a folder called `languages`, but you can name this folder whatever you'd like.
```xml
 <ItemGroup>
      <None Remove="languages\*.json" />
      <AdditionalFiles Include="languages\*.json" />
</ItemGroup>
```

# How to use
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

----

## Notes

- If you provide a `LocalizationResource` and `LocalizationLanguage`, it will default to the `CurrentCulture` for your application.

Given: `var translatedText = Localizer.Get(LocalizationResource.SAMPLE, LocalizationLanguage.de_DE);`

It will return `Something` because on my machine `en-US` is the current culture.
```json
{
  "HELLO": {
    "en-US": "Hello",
    "de-DE": "Hallo"
  },
  "SAMPLE":
  {
    "en-US": "Something"
  }
}
```

If a translation does not exist for your current culture then an empty string is returned.
