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

## Checking a value in the domain: `Guard`

`Guard` (Core) checks and fails with a `DomainException` that names a text, so the domain never asks for one ([ADR-0039](adr/0039-guard-and-translating-a-response-body.md)):

```csharp
Name = Guard.NotNullOrWhiteSpace(name, nameof(Name), maxLength: 128);    // trimmed; Validation:Required / MaxLength / MinLength
Description = Guard.Length(description, nameof(Description), 1024);
Price = Guard.NotNegative(price, nameof(Price));                          // Validation:NotNegative
Quantity = Guard.Range(quantity, nameof(Quantity), 1, int.MaxValue);      // Validation:Range
ProductId = Guard.NotDefault<Guid>(productId, nameof(ProductId));         // Validation:Required for null or Guid.Empty
Lines = Guard.NotNullOrEmpty(lines, nameof(Lines));                       // Validation:IsNotEmpty
```

The first argument of each text is the name of the parameter. The framework has the texts in `en`, `tr`, `ru`.

## A translated text in a response body

An application service has `L` (the standard `IStringLocalizer`, in the `ApplicationServiceBase` of the service, [ADR-0041](adr/0041-application-service-base-in-the-framework.md)) and so has a controller (in its base controller); code with neither asks for `IMessageLocalizer`. All read the one set of texts in the language of the request:

```csharp
// application service (ApplicationServiceBase: L, Logger, CurrentUser, CurrentTenant)
StatusText = L[$"OrderStatus:{order.Status}"].Value,         // the key comes back when no language has the text

// code with no L: IMessageLocalizer.Get gives the key too, Find gives null

// controller (base controller: L, CurrentUser, CurrentTenant, Logger)
return Ok(new { label = L["OrderStatus:Pending"].Value });
```

Keep the stable code in the body too (`Status`): the client reacts to the code and shows the text.

## In code

`IMessageLocalizer.Find(key, args)` gives the text in the current language (`null` when there is none); the standard `IStringLocalizer` and `IStringLocalizer<T>` read the same texts (one set; the type does not matter).

## The texts of a validation

The framework JSON has `Validation:*` (`Required`, `MaxLength`, `MinLength`, `Range`, `Email`, `Phone`, `Url`, `Pattern`, `Compare`, `Length`, `InvalidValue`, `InvalidJson`, `Failed`) in en, tr and ru; a test keeps the three in step. The label of a field is `Field:<Dto>:<Property>`, then `Field:<Property>`, then the
property name, and it is the first argument of the text. See [ADR-0044](adr/0044-validation-of-requests.md) and [WebApi.md](WebApi.md#validation-of-a-request).
