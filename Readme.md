# Install

```
Install-Package LocalizationGenerator
```

# Setup

Add a JSON file in your project root called `localization.json`. Follow the format below.
```json
{
  "HELLO": {
    "en-US": "hello",
    "de-DE": "hallo"
  }
}
```

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

```xml
<ItemGroup>
  <None Remove="localization.json" />
  <AdditionalFiles Include="localization.json"/>
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
