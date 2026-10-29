using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace HsnSoft.Base.Domain.Sorting;

/// <summary>
/// The fields a list of an entity may be sorted by, the names a client uses and the expression behind each: <c>new SortFields&lt;Product&gt;().Add("name", p =&gt; p.NormalizedName).Add("price", p =&gt; p.Price)</c>.
/// A client sends <c>"name asc, price desc"</c> (<see cref="Parse"/>); a name that is not here is refused, so nobody sorts by a column that was not offered (a password hash) and the text is never part of a query.
/// </summary>
public sealed class SortFields<TEntity>
{
    private readonly Dictionary<string, SortClauseFactory> _fields = new(StringComparer.OrdinalIgnoreCase);

    private delegate SortClause<TEntity> SortClauseFactory(bool descending);

    public IReadOnlyCollection<string> Names => _fields.Keys;

    public SortFields<TEntity> Add<TKey>(string name, Expression<Func<TEntity, TKey>> key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(key);

        _fields[name.Trim()] = descending => new SortClause<TEntity, TKey>(key, descending);
        return this;
    }

    /// <summary>The sorting of <paramref name="sortingText"/> (<c>"name asc, price desc"</c>; a field alone is ascending), or of <paramref name="defaultSorting"/> when the text is blank.</summary>
    /// <exception cref="HsnSoft.Base.DomainException">A field is not one of the fields, or a direction is not <c>asc</c> or <c>desc</c> (the client is told: <c>Validation:UnknownSortField</c>, <c>Validation:UnknownSortDirection</c>).</exception>
    public SortSpec<TEntity> Parse(string? sortingText, string defaultSorting)
    {
        string text = string.IsNullOrWhiteSpace(sortingText) ? defaultSorting : sortingText;
        List<SortClause<TEntity>> clauses = [];
        foreach (string part in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string[] words = part.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (!_fields.TryGetValue(words[0], out SortClauseFactory? factory))
            {
                throw new HsnSoft.Base.DomainException($"'{words[0]}' is not a field to sort by ({string.Join(", ", _fields.Keys)}).").WithKey("Validation:UnknownSortField", words[0], string.Join(", ", _fields.Keys));
            }

            bool descending = words.Length switch
            {
                1 => false,
                2 when words[1].Equals("asc", StringComparison.OrdinalIgnoreCase) => false,
                2 when words[1].Equals("desc", StringComparison.OrdinalIgnoreCase) => true,
                _ => throw new HsnSoft.Base.DomainException($"'{string.Join(' ', words.Skip(1))}' is not a direction (asc or desc).").WithKey("Validation:UnknownSortDirection", string.Join(' ', words.Skip(1))),
            };
            clauses.Add(factory(descending));
        }

        return new SortSpec<TEntity>(clauses);
    }
}
