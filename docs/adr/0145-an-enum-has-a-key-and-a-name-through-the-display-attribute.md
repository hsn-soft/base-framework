# ADR-0145: An enum has a key and a name through the `Display` attribute (`Extensions`)

- **Status:** Accepted
- **Date:** 2026-11-04

## Context

A front end sends an enum to the back end as a **key**, not as a number or as the C# name: small letters, the words joined by `-` (`video-platform`). The same member also has a **name** for a person (`Video Platform`, with spaces). The old HHS shared library kept both on the member with `[Display(Name = ..., Prompt = ...)]` (`Name` the description, `Prompt` the key) and converted with an extension (`ToPrompt`, `ToEnumFromPrompt`); the rebuild on this framework dropped the attributes and the extension, and with them the contract with the front ends.

## Decision

`HsnSoft.Base.Extensions` has `EnumExtensions` (namespace `HsnSoft.Base.Text`): `ToPrompt()` (member to key), `ToDisplayName()` (member to name), `ToEnumFromPrompt<T>()` and `TryToEnumFromPrompt<T>()` (key to member, without regard to case). The key is the `Prompt` of the `Display` attribute of the member; a member with no prompt has a key made of its name (`VideoPlatform` is `video-platform`, `EInvoice` is `e-invoice`), so every member can be sent and read. What is read from the attributes is kept per enum type in a frozen dictionary, so no call uses reflection. A value that is not a member is written as its number. The package keeps its rule: no package of anyone else's (`System.ComponentModel.DataAnnotations` is the base library).

## Consequences

An enum that a client sees carries `[Display(Name, Prompt)]` on its members and is read with `ToEnumFromPrompt`; the prompts are part of the contract with the clients, so changing one is a change of the API. The cost: a member added without a prompt gets a key from its name, which becomes part of the contract too.

## Related

[ADR-0137](0137-the-helpers-of-the-old-core-come-back-only-when-a-consumer-used-them.md), `src/HsnSoft.Base.Extensions/HsnSoft/Base/Text/EnumExtensions.cs`.
