// See https://aka.ms/new-console-template for more information

using System.Globalization;
using ConsoleSample;

Console.WriteLine(Localizer.Get(LocalizationResource.HELLO));
Console.WriteLine(Localizer.Get(LocalizationResource.HELLO, LocalizationLanguage.de_DE));
Console.WriteLine(Localizer.Get(LocalizationResource.HELLO, CultureInfo.CurrentCulture));