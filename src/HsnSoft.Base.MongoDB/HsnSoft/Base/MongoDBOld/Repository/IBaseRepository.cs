using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using HsnSoft.Base.MongoDB.Options;
using HsnSoft.Base.MongoDBOld.Base;
using MongoDB.Driver;

namespace HsnSoft.Base.MongoDBOld.Repository;

public interface IBaseRepository<TDocument> where TDocument : IBaseDocument
{
    #region Queryable

    /// <summary>
    /// Provides functionality to evaluate queries against a specific data source wherein the type of the data is not specified.
    /// Uses secondaryPreferred: https://docs.mongodb.com/manual/core/read-preference/#mongodb-read-mode-secondaryPreferred
    /// </summary>
    /// <returns>IQueryable</returns>
    IQueryable<TDocument> AsQueryable();

    #endregion

    #region Find

    /// <summary>
    /// Returns FirstOrDefault for given filter expression
    /// </summary>
    /// <param name="filterExpression"></param>
    /// <param name="readOption"></param>
    /// <returns>found document</returns>
    TDocument FindOne(Expression<Func<TDocument, bool>> filterExpression,
        ReadOption readOption);

    TDocument FindOne(Expression<Func<TDocument, bool>> filterExpression);

    /// <summary>
    /// Returns FirstOrDefault for given filter expression and read option
    /// </summary>
    /// <param name="filterExpression"></param>
    /// <param name="readOption"></param>
    /// <returns>found document</returns>
    Task<TDocument> FindOneAsync(Expression<Func<TDocument, bool>> filterExpression,
        ReadOption readOption);

    Task<TDocument> FindOneAsync(Expression<Func<TDocument, bool>> filterExpression);

    /// <summary>
    /// Returns SingleOrDefault for given id and read option
    /// </summary>
    /// <param name="id">string representation of object id</param>
    /// <param name="readOption"></param>
    /// <returns>found document</returns>
    TDocument FindById(string id, ReadOption readOption);

    TDocument FindById(string id);

    /// <summary>
    /// Returns SingleOrDefault for given id and read option
    /// </summary>
    /// <param name="id">string representation of object id</param>
    /// <param name="readOption"></param>
    /// <returns>found document</returns>
    Task<TDocument> FindByIdAsync(string id, ReadOption readOption);

    Task<TDocument> FindByIdAsync(string id);

    #endregion

    #region Insert

    /// <summary>
    /// Inserts the provided document
    /// </summary>
    /// <param name="document"></param>
    void InsertOne(TDocument document);

    /// <summary>
    /// Inserts the provided document
    /// </summary>
    /// <param name="document"></param>
    /// <returns></returns>
    Task InsertOneAsync(TDocument document);

    /// <summary>
    /// Inserts the provided documents
    /// </summary>
    /// <param name="documents"></param>
    void InsertMany(ICollection<TDocument> documents);

    /// <summary>
    /// Inserts the provided documents
    /// </summary>
    /// <param name="documents"></param>
    /// <returns></returns>
    Task InsertManyAsync(ICollection<TDocument> documents);

    /// <summary>
    /// Inserts given key and update lists to existing document, encapsulates nested updates behind the scene
    /// </summary>
    /// <param name="filterExpression"></param>
    /// <param name="key"></param>
    /// <param name="updateList"></param>
    /// <param name="filterList"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task InsertNestedObjectAsync<T>(Expression<Func<TDocument, bool>> filterExpression, string key,
        List<T> updateList, List<string> filterList);

    #endregion

    #region Replace

    /// <summary>
    /// Replaces given document by id
    /// </summary>
    /// <param name="document"></param>
    void ReplaceOne(TDocument document);

    /// <summary>
    /// Replaces given document by id
    /// </summary>
    /// <param name="document"></param>
    /// <returns></returns>
    Task ReplaceOneAsync(TDocument document);

    /// <summary>
    /// Replaces given documents by id
    /// </summary>
    /// <param name="documents"></param>
    /// <param name="isOrdered">if true, then when a write fails, return without performing the remaining writes.</param>
    public void ReplaceMany(List<TDocument> documents, bool isOrdered = false);

    /// <summary>
    /// Replaces given documents by id
    /// </summary>
    /// <param name="documents"></param>
    /// <param name="isOrdered">if true, then when a write fails, return without performing the remaining writes.</param>
    /// <returns></returns>
    Task ReplaceManyAsync(List<TDocument> documents, bool isOrdered = false);

    #endregion

    #region Delete

    /// <summary>
    /// Deletes one document using given filter expression
    /// </summary>
    /// <param name="filterExpression"></param>
    void DeleteOne(Expression<Func<TDocument, bool>> filterExpression);

    /// <summary>
    /// Deletes one document using given filter expression
    /// </summary>
    /// <param name="filterExpression"></param>
    /// <returns></returns>
    Task DeleteOneAsync(Expression<Func<TDocument, bool>> filterExpression);

    /// <summary>
    /// Deletes one document using given string representation of object id
    /// </summary>
    /// <param name="id">string representation of object id</param>
    void DeleteById(string id);

    /// <summary>
    /// Deletes one document using given string representation of object id
    /// </summary>
    /// <param name="id">string representation of object id</param>
    /// <returns></returns>
    Task DeleteByIdAsync(string id);

    /// <summary>
    /// Deletes matching documents using given filter expression
    /// </summary>
    /// <param name="filterExpression"></param>
    void DeleteMany(Expression<Func<TDocument, bool>> filterExpression);

    /// <summary>
    /// Deletes matching documents using given filter expression
    /// </summary>
    /// <param name="filterExpression"></param>
    /// <returns></returns>
    Task DeleteManyAsync(Expression<Func<TDocument, bool>> filterExpression);

    /// <summary>
    /// Deletes given keys using pull operator, encapsulates nested updates behind the scene
    /// </summary>
    /// <param name="filterExpression"></param>
    /// <param name="key"></param>
    /// <param name="filterObject"></param>
    /// <param name="filterList"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task DeleteNestedObjectAsync<T>(Expression<Func<TDocument, bool>> filterExpression, string key,
        Expression<Func<T, bool>> filterObject, List<string> filterList);

    #endregion

    #region Count

    /// <summary>
    /// Counts documents for given filter expression
    /// </summary>
    /// <param name="filterExpression"></param>
    /// <param name="readOption"></param>
    /// <returns>number of documents matches with filter expression</returns>
    public long Count(Expression<Func<TDocument, bool>> filterExpression,
        ReadOption readOption);

    public long Count(Expression<Func<TDocument, bool>> filterExpression);

    /// <summary>
    /// Gives the estimated document count for entire collection
    /// </summary>
    /// <returns></returns>
    public long EstimatedDocumentCount();

    /// <summary>
    /// Counts documents for given filter expression
    /// </summary>
    /// <param name="filterExpression"></param>
    /// <param name="readOption"></param>
    /// <returns>number of documents matches with filter expression</returns>
    Task<long> CountAsync(Expression<Func<TDocument, bool>> filterExpression,
        ReadOption readOption);

    Task<long> CountAsync(Expression<Func<TDocument, bool>> filterExpression);

    /// <summary>
    /// Gives the estimated document count for entire collection
    /// </summary>
    /// <returns></returns>
    Task<long> EstimatedDocumentCountAsync();

    #endregion

    #region Update

    /// <summary>
    /// Updates single document for given expression and update dictionary
    /// </summary>
    /// <param name="filterExpression"></param>
    /// <param name="updateValues">key,dynamic dictionary</param>
    /// <returns>update operation acknowledge</returns>
    public bool Update(Expression<Func<TDocument, bool>> filterExpression,
        Dictionary<string, dynamic> updateValues);

    /// <summary>
    /// Updates single document for given expression and update dictionary
    /// </summary>
    /// <param name="filterExpression"></param>
    /// <param name="updateValues">key,dynamic dictionary</param>
    /// <returns></returns>
    Task UpdateAsync(Expression<Func<TDocument, bool>> filterExpression, Dictionary<string, dynamic> updateValues);

    /// <summary>
    /// Updates documents for given expression and update dictionary
    /// </summary>
    /// <param name="filterExpression"></param>
    /// <param name="updateValues">key,dynamic dictionary</param>
    /// <returns>update operation acknowledge</returns>
    public bool UpdateMany(Expression<Func<TDocument, bool>> filterExpression,
        Dictionary<string, dynamic> updateValues);

    /// <summary>
    /// Updates documents for given expression and update dictionary
    /// </summary>
    /// <param name="filterExpression"></param>
    /// <param name="updateValues">key,dynamic dictionary</param>
    /// <returns></returns>
    Task UpdateManyAsync(Expression<Func<TDocument, bool>> filterExpression,
        Dictionary<string, dynamic> updateValues);

    /// <summary>
    /// Updates single document for given filter expression.
    /// Combines multiple update values on a single update definition
    /// </summary>
    /// <param name="filterExpression"></param>
    /// <param name="filterField"></param>
    /// <param name="filterObject"></param>
    /// <param name="updateValues"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task UpdateNestedAsync<T>(Expression<Func<TDocument, bool>> filterExpression,
        Expression<Func<TDocument, IEnumerable<T>>> filterField, Expression<Func<T, bool>> filterObject,
        Dictionary<Expression<Func<TDocument, object>>, object> updateValues);

    /// <summary>
    /// Updates single document for given filter expression via encapsulating mongodb array filters
    /// </summary>
    /// <param name="filterExpression"></param>
    /// <param name="updateList"></param>
    /// <param name="filterList"></param>
    /// <returns></returns>
    Task UpdateNestedFields(Expression<Func<TDocument, bool>> filterExpression,
        Dictionary<string, dynamic> updateList, List<string> filterList);

    /// <summary>
    /// Updates single document for given filter expression via encapsulating mongodb array filters
    /// </summary>
    /// <param name="filterExpression"></param>
    /// <param name="updateValues"></param>
    /// <param name="filterList"></param>
    /// <returns></returns>
    Task UpdateNestedFields(Expression<Func<TDocument, bool>> filterExpression,
        Dictionary<Expression<Func<TDocument, object>>, object> updateValues, List<string> filterList);

    /// <summary>
    /// Updates single document for given expression and update dictionary
    /// </summary>
    /// <param name="filterExpression"></param>
    /// <param name="updateValues"></param>
    /// <returns></returns>
    bool Update(Expression<Func<TDocument, bool>> filterExpression,
        Dictionary<Expression<Func<TDocument, object>>, object> updateValues);

    /// <summary>
    /// Updates single document for given expression and update dictionary
    /// </summary>
    /// <param name="filterExpression"></param>
    /// <param name="updateValues"></param>
    /// <returns></returns>
    Task UpdateAsync(Expression<Func<TDocument, bool>> filterExpression,
        Dictionary<Expression<Func<TDocument, object>>, object> updateValues);

    /// <summary>
    /// Updates documents for given expression and update dictionary
    /// </summary>
    /// <param name="filterExpression"></param>
    /// <param name="updateValues"></param>
    /// <returns></returns>
    bool UpdateMany(Expression<Func<TDocument, bool>> filterExpression,
        Dictionary<Expression<Func<TDocument, object>>, object> updateValues);

    /// <summary>
    /// Updates documents for given expression and update dictionary
    /// </summary>
    /// <param name="filterExpression"></param>
    /// <param name="updateValues"></param>
    /// <returns></returns>
    Task UpdateManyAsync(Expression<Func<TDocument, bool>> filterExpression,
        Dictionary<Expression<Func<TDocument, object>>, object> updateValues);

    #endregion

    #region FindOneAndUpdate

    TDocument FindOneAndUpdate(Expression<Func<TDocument, bool>> filterExpression,
        UpdateDefinition<TDocument> updateDefinition, FindOneAndUpdateOptions<TDocument> updateOptions);

    Task<TDocument> FindOneAndUpdateAsync(Expression<Func<TDocument, bool>> filterExpression,
        UpdateDefinition<TDocument> updateDefinition, FindOneAndUpdateOptions<TDocument> updateOptions);

    #endregion

    #region Filter

    /// <summary>
    /// Filters documents for given expression and projects documents using projection expression
    /// </summary>
    /// <param name="filterExpression"></param>
    /// <param name="projectionExpression"></param>
    /// <param name="filterOptions"></param>
    /// <typeparam name="TProjected"></typeparam>
    /// <returns></returns>
    Task<IEnumerable<TProjected>> FilterByAsync<TProjected>(
        Expression<Func<TDocument, bool>> filterExpression, FilterOptions filterOptions,
        Expression<Func<TDocument, TProjected>> projectionExpression);

    Task<IEnumerable<TProjected>> FilterByAsync<TProjected>(
        Expression<Func<TDocument, bool>> filterExpression,
        Expression<Func<TDocument, TProjected>> projectionExpression);

    /// <summary>
    /// Filters documents for given expression, uses filterOptions to paginate and readPreference
    /// </summary>
    /// <param name="filterExpression"></param>
    /// <returns></returns>
    Task<IEnumerable<TDocument>> FilterByAsync(
        Expression<Func<TDocument, bool>> filterExpression);

    Task<IEnumerable<TDocument>> FilterByAsync(
        Expression<Func<TDocument, bool>> filterExpression, FilterOptions filterOptions);

    /// <summary>
    /// Filters documents for given expression and projects documents using projection expression, include and exclude list,
    /// uses filterOptions to paginate and readPreference
    /// </summary>
    /// <param name="filterExpression"></param>
    /// <param name="include"></param>
    /// <param name="exclude"></param>
    /// <typeparam name="TProjected"></typeparam>
    /// <returns></returns>
    Task<IEnumerable<TProjected>> FilterByAsync<TProjected>(
        Expression<Func<TDocument, bool>> filterExpression,
        List<string> include = null, List<string> exclude = null);

    Task<IEnumerable<TProjected>> FilterByAsync<TProjected>(
        Expression<Func<TDocument, bool>> filterExpression, FilterOptions filterOptions,
        List<string> include = null, List<string> exclude = null);

    /// <summary>
    /// Filters documents for given expression and projects documents using projection expression, include and exclude list,
    /// uses filterOptions to paginate and readPreference
    /// </summary>
    /// <param name="filterExpression"></param>
    /// <param name="include"></param>
    /// <param name="exclude"></param>
    /// <typeparam name="TProjected"></typeparam>
    /// <returns></returns>
    IEnumerable<TProjected> FilterBy<TProjected>(
        Expression<Func<TDocument, bool>> filterExpression,
        List<string> include = null, List<string> exclude = null);

    IEnumerable<TProjected> FilterBy<TProjected>(
        Expression<Func<TDocument, bool>> filterExpression, FilterOptions filterOptions,
        List<string> include = null, List<string> exclude = null);

    /// <summary>
    /// Filters documents for given expression and projects documents using projection expression
    /// </summary>
    /// <param name="filterExpression"></param>
    /// <param name="projectionExpression"></param>
    /// <typeparam name="TProjected"></typeparam>
    /// <returns></returns>
    IEnumerable<TProjected> FilterBy<TProjected>(
        Expression<Func<TDocument, bool>> filterExpression,
        Expression<Func<TDocument, TProjected>> projectionExpression);

    IEnumerable<TProjected> FilterBy<TProjected>(
        Expression<Func<TDocument, bool>> filterExpression,
        FilterOptions filterOptions,
        Expression<Func<TDocument, TProjected>> projectionExpression);

    /// <summary>
    /// Filters documents for given expression, uses filterOptions to paginate and readPreference
    /// </summary>
    /// <param name="filterExpression"></param>
    /// <param name="filterOptions"></param>
    /// <returns></returns>
    IEnumerable<TDocument> FilterBy(Expression<Func<TDocument, bool>> filterExpression,
        FilterOptions filterOptions);

    IEnumerable<TDocument> FilterBy(Expression<Func<TDocument, bool>> filterExpression);

    Task<IEnumerable<TDocument>> FilterByAsync(FilterDefinition<TDocument> filterDefinition);
    IEnumerable<TDocument> FilterBy(FilterDefinition<TDocument> filterDefinition);

    IEnumerable<TDocument> FilterByText(string searchTerm, string defaultTextIndexLanguage = "en");
    Task<IEnumerable<TDocument>> FilterByTextAsync(string searchTerm, string defaultTextIndexLanguage = "en");
    IEnumerable<TDocument> FilterByText(List<string> searchTerms, string defaultTextIndexLanguage = "en");
    Task<IEnumerable<TDocument>> FilterByTextAsync(List<string> searchTerms, string defaultTextIndexLanguage = "en");

    Task<string> CreateTextIndex(IndexKeysDefinition<TDocument> textIndexKeys, bool ifExistReCreate, string defaultTextIndexLanguage = "en");

    #endregion
}