# Localization

The texts the client reads (the status messages, the error messages) come from JSON files embedded in the assemblies, in the language of the request ([ADR-0038](adr/0038-localization.md)). The logs are not translated.

## The files

One file for each language, in a `Localization` folder of the project, embedded:

```xml
<ItemGroup>
    <EmbeddedResource Include="Localization\*.json" />
</ItemGroup>
```

```json
{
  "culture": "tr",
  "texts": {
    "product_not_found": "{0} numaralı ürün bulunamadı.",
    "product_sku_duplicate": "'{0}' stok kodu kullanılıyor."
  }
}
```

`{0}`, `{1}` are the places for the arguments. The languages are `en`, `tr`, `ru` (`Localization:SupportedLanguages`), the default `en` (`Localization:DefaultLanguage`). A file with a key twice, with a wrong `culture` or that is not JSON stops the application at start;
what a translation is missing (a key, a `{n}`) is logged as a Warning at start.

## The layers: from the innermost out

All the texts are merged into one dictionary per language, **outermost layer first, innermost last; the inner one wins** for a key they both have:

| Layer | Where | Registered by |
|---|---|---|
| framework | `HsnSoft.Base.Localization/Localization` (`Status:*`, `Base:ErrorCode`, `Validation:Failed`) | `AddBaseLocalization` |
| shared | `Abc.Shared.Hosting/Localization` (what several services say: `field_required`, `invalid_credentials`) | `AddAbcHosting` |
| service | `Abc.XService.Domain/Localization` (its errors: `product_not_found`) | `AddAbcMicroservice(config, title, typeof(Product).Assembly)` |

```csharp
services.AddBaseLocalization(configuration, options => options.AddEmbedded(typeof(SomeTypeOfTheAssembly).Assembly));   // each call adds its layer after the earlier ones
```

A key that is not in the current language is looked up in the default language; a key that no language has is logged once and the client gets the message of the exception.

## Which language

1. the **`locale` claim** of the token (`Localization:LanguageClaim`), when it is a supported language (`tr-TR` is `tr`);
2. the **`Accept-Language` header**: the supported language with the highest quality;
3. the **default language**.

`app.UseBaseLocalization()` (after `UseAuthentication`; the Abc hosts do it in `UseAbcHosting`) sets it for the rest of the pipeline and answers the `Content-Language` header. The exception handler and the status pages work out the language themselves.
The language is a short code (`ILanguageAccessor.Current`), not a `CultureInfo`, so it works with `InvariantGlobalization`.

## Naming a text in an error

The exception keeps its message (English, what the logs say) and names the text and its arguments; only the client gets the translation:

```csharp
throw new NotFoundException($"Product {id} was not found.").WithKey("product_not_found", id);
throw new DomainException($"{name} is required and at most {max} characters.").WithKey("field_required", name, max);
throw new BusinessException("product_sku_duplicate", $"The sku '{sku}' is taken.").WithKey("product_sku_duplicate", sku);   // the error code is the key unless WithKey is given
```

`NotFoundException`, `BusinessRuleException`, `DomainException`, `BaseHttpException` and `BusinessException` have `WithKey`; a `BusinessException` without it uses its error code as the key (with no arguments). The failures of the service (5xx) are not translated.

## In code

`IMessageLocalizer.Find(key, args)` gives the text in the current language (`null` when there is none); the standard `IStringLocalizer` and `IStringLocalizer<T>` read the same texts (one set; the type does not matter).
