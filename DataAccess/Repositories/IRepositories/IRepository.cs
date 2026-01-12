using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace DataAccess.Repositories.IRepositories
{
    public interface IRepository<T> where T : class
    {
        Task<bool> CreateAsync(T entity);

        Task<bool> EditAsync(T entity);

        Task<bool> DeleteAsync(T entity);
        Task<bool> DeleteAllAsync(IEnumerable<T> entities);
        Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>>? filter = null,
            Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null, bool tracked = true
            , int? skip = null, int? take = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null);

        Task<int> CountAsync(Expression<Func<T, bool>>? filter = null);
        Task<T?> GetOneAsync(Expression<Func<T, bool>>? filter = null,
            Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null, bool tracked = true);

        Task<bool> CommitAsync();
    }

}
