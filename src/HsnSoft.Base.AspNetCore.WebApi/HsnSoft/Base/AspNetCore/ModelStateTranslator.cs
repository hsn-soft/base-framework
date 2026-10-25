using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using HsnSoft.Base.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.AspNetCore;

/// <summary>
/// Tells every field of a request that is wrong, each in the language of the request: what an attribute (<c>[Required]</c>, <c>[StringLength]</c>, <c>[Range]</c>, <c>[EmailAddress]</c>...) found, a value that could not be read at all (<c>"abc"</c> for a
/// guid, a malformed JSON), and what a rule of the DTO named with <see cref="ValidationRule"/>. The texts are <c>Validation:*</c>, the label of a field is <c>Field:&lt;Dto&gt;:&lt;Property&gt;</c> or <c>Field:&lt;Property&gt;</c> when somebody translated it, else the name of the property.
/// What it does not know (a message another rule wrote) is told as it was written.
/// </summary>
internal static class ModelStateTranslator
{
    public static List<string> Translate(ActionContext context)
    {
        IServiceProvider services = context.HttpContext.RequestServices;
        IMessageLocalizer? localizer = services.GetService<IMessageLocalizer>();
        IModelMetadataProvider provider = services.GetRequiredService<IModelMetadataProvider>();
        ModelMetadata[] roots = [.. context.ActionDescriptor.Parameters.Where(parameter => parameter.ParameterType != typeof(string)).Select(parameter => provider.GetMetadataForType(parameter.ParameterType))];

        List<string> errors = [];
        foreach ((string key, ModelStateEntry? entry) in context.ModelState.Where(pair => pair.Value?.Errors.Count > 0))
        {
            ModelMetadata? metadata = Find(roots, key);
            string label = Label(localizer, metadata, key);
            foreach (ModelError error in entry!.Errors)
            {
                errors.Add(Say(localizer, metadata, label, key, error));
            }
        }

        return [localizer?.Find("Validation:Failed") ?? "Validation failed.", .. errors.Distinct()];
    }

    private static string Say(IMessageLocalizer? localizer, ModelMetadata? metadata, string label, string key, ModelError error)
    {
        // a rule of the DTO named its text (ValidationRule): the field's name in its arguments is the label
        if (ValidationRule.TryDecode(error.ErrorMessage, out string ruleKey, out string[] ruleArgs))
        {
            string property = metadata?.Name ?? Last(key);
            object?[] args = [.. ruleArgs.Select(argument => string.Equals(argument, property, StringComparison.OrdinalIgnoreCase) ? label : argument)];
            return localizer?.Find(ruleKey, args) ?? error.ErrorMessage;
        }

        // a value that could not be read: "abc" for a guid, a number that is not one, a JSON that is broken
        // (the JSON input formatter keeps the text of its exception, with the JSON path in it, instead of the exception)
        if (error.Exception is not null || error.ErrorMessage.Contains("Path: $", StringComparison.Ordinal))
        {
            return key is "" or "$" && (error.Exception is null or JsonException)
                ? localizer?.Find("Validation:InvalidJson") ?? "The request is not valid JSON."
                : localizer?.Find("Validation:InvalidValue", label) ?? $"{label} has an invalid value.";
        }

        // what an attribute found
        foreach (ValidationAttribute attribute in metadata?.ValidatorMetadata.OfType<ValidationAttribute>() ?? [])
        {
            if (string.Equals(attribute.FormatErrorMessage(metadata!.GetDisplayName()), error.ErrorMessage, StringComparison.Ordinal) && Of(attribute, label) is var (textKey, textArgs))
            {
                return localizer?.Find(textKey, textArgs) ?? error.ErrorMessage;
            }
        }

        return string.IsNullOrWhiteSpace(error.ErrorMessage) ? localizer?.Find("Validation:InvalidValue", label) ?? $"{label} has an invalid value." : error.ErrorMessage;
    }

    private static (string Key, object?[] Args)? Of(ValidationAttribute attribute, string label) => attribute switch
    {
        RequiredAttribute => ("Validation:Required", [label]),
        NotDefaultAttribute => ("Validation:InvalidValue", [label]),
        StringLengthAttribute { MinimumLength: 0 } length => ("Validation:MaxLength", [label, length.MaximumLength]),
        StringLengthAttribute length => ("Validation:Length", [label, length.MinimumLength, length.MaximumLength]),
        MaxLengthAttribute max => ("Validation:MaxLength", [label, max.Length]),
        MinLengthAttribute min => ("Validation:MinLength", [label, min.Length]),
        RangeAttribute range => ("Validation:Range", [label, range.Minimum, range.Maximum]),
        EmailAddressAttribute => ("Validation:Email", [label]),
        PhoneAttribute => ("Validation:Phone", [label]),
        UrlAttribute => ("Validation:Url", [label]),
        RegularExpressionAttribute => ("Validation:Pattern", [label]),
        CompareAttribute compare => ("Validation:Compare", [label, compare.OtherProperty]),
        _ => null,
    };

    private static string Label(IMessageLocalizer? localizer, ModelMetadata? metadata, string key)
    {
        string property = metadata?.Name ?? Last(key);
        string? owner = metadata?.ContainerType?.Name;
        return (owner is null ? null : localizer?.FindIfPresent($"Field:{owner}:{property}")) ?? localizer?.FindIfPresent($"Field:{property}") ?? property;
    }

    private static string Last(string key)
    {
        string last = key.Split('.')[^1];
        int index = last.IndexOf('[', StringComparison.Ordinal);
        return index >= 0 ? last[..index] : last;
    }

    // the metadata of the property a ModelState key names, walked from the DTOs of the action: "Name", "$.lines[0].productId", "input.Name"
    private static ModelMetadata? Find(ModelMetadata[] roots, string key)
    {
        string[] path = key.StartsWith("$.", StringComparison.Ordinal) ? key[2..].Split('.') : key.Split('.');
        foreach (string[] candidate in new[] { path, path.Length > 1 ? path[1..] : path })
        {
            foreach (ModelMetadata root in roots)
            {
                ModelMetadata? found = Walk(root, candidate);
                if (found is not null)
                {
                    return found;
                }
            }
        }

        return null;
    }

    private static ModelMetadata? Walk(ModelMetadata root, string[] path)
    {
        ModelMetadata? current = root;
        foreach (string part in path)
        {
            int index = part.IndexOf('[', StringComparison.Ordinal);
            string name = index >= 0 ? part[..index] : part;
            current = current.Properties.FirstOrDefault(property => string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase));
            if (current is not null && index >= 0)
            {
                current = current.ElementMetadata;
            }

            if (current is null)
            {
                return null;
            }
        }

        return current;
    }
}
